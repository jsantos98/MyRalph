using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectManagement.Application.Services;
using ProjectManagement.Core.Interfaces;
using ProjectManagement.Infrastructure.Claude;
using ProjectManagement.Infrastructure.Data.DbContext;
using ProjectManagement.Infrastructure.Data.Repositories;
using ProjectManagement.Infrastructure.Git;
using ProjectManagement.CLI.Commands;
using Spectre.Console.Cli;

namespace ProjectManagement.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                      .AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true)
                      .AddEnvironmentVariables()
                      .AddUserSecrets<Program>(optional: true);
            })
            .ConfigureServices((context, services) =>
            {
                // Configuration
                services.Configure<ClaudeApiSettings>(
                    context.Configuration.GetSection(ClaudeApiSettings.SectionName));
                services.Configure<GitSettings>(
                    context.Configuration.GetSection(GitSettings.SectionName));

                // Database
                services.AddDbContext<ProjectManagementDbContext>(options =>
                {
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                        ?? "Data Source=projectmanagement.db";
                    options.UseSqlite(connectionString);
                });

                // Repositories
                services.AddScoped<IWorkItemRepository, WorkItemRepository>();
                services.AddScoped<IDeveloperStoryRepository, DeveloperStoryRepository>();
                services.AddScoped<IDeveloperStoryDependencyRepository, DeveloperStoryDependencyRepository>();
                services.AddScoped<IExecutionLogRepository, ExecutionLogRepository>();
                services.AddScoped<IUnitOfWork, UnitOfWork>();

                // Infrastructure Services
                services.AddScoped<IClaudeApiService, ClaudeApiService>();
                services.AddScoped<IClaudeCodeIntegration, ClaudeCodeIntegration>();
                services.AddScoped<IGitService, LibGit2SharpService>();

                // Application Services
                services.AddScoped<IStateManager, StateManager>();
                services.AddScoped<IWorkItemService, WorkItemService>();
                services.AddScoped<IRefinementService, RefinementService>();
                services.AddScoped<IDependencyResolutionService, DependencyResolutionService>();
                services.AddScoped<IImplementationService, ImplementationService>();

                // Logging
                services.AddLogging(configure => configure.AddConsole());
            })
            .Build();

        var registrar = new TypeRegistrar(host.Services);
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("pm");
            config.AddCommand<CreateItemCommand>("create");
            config.AddCommand<RefineItemCommand>("refine");
            config.AddCommand<SelectNextCommand>("next");
            config.AddCommand<ImplementCommand>("implement");
            config.AddCommand<ListItemsCommand>("list");
        });

        return await app.RunAsync(args);
    }

    private class TypeRegistrar : ITypeRegistrar, IDisposable
    {
        private readonly IServiceScope _scope;
        private readonly Dictionary<Type, Type> _typeRegistrations;
        private readonly Dictionary<Type, object> _instanceRegistrations;
        private readonly Dictionary<Type, Func<object>> _lazyRegistrations;
        private bool _disposed = false;

        public TypeRegistrar(IServiceProvider services)
        {
            // Create a single scope for the entire application lifetime
            // CLI apps run one command and exit, so a single scope is appropriate
            _scope = services.CreateScope();
            // Track registrations from Spectre.Console.Cli
            _typeRegistrations = new Dictionary<Type, Type>();
            _instanceRegistrations = new Dictionary<Type, object>();
            _lazyRegistrations = new Dictionary<Type, Func<object>>();
        }

        public ITypeResolver Build()
        {
            return new TypeResolver(
                _typeRegistrations,
                _instanceRegistrations,
                _lazyRegistrations,
                _scope.ServiceProvider);
        }

        public void Register(Type service, Type implementation)
        {
            _typeRegistrations[service] = implementation;
        }

        public void RegisterInstance(Type service, object implementation)
        {
            _instanceRegistrations[service] = implementation;
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            _lazyRegistrations[service] = factory;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _scope.Dispose();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }
    }

    private class TypeResolver : ITypeResolver
    {
        private readonly Dictionary<Type, Type> _typeRegistrations;
        private readonly Dictionary<Type, object> _instanceRegistrations;
        private readonly Dictionary<Type, Func<object>> _lazyRegistrations;
        private readonly IServiceProvider _scopedProvider;
        private readonly Dictionary<Type, object> _lazyCache = new();

        public TypeResolver(
            Dictionary<Type, Type> typeRegistrations,
            Dictionary<Type, object> instanceRegistrations,
            Dictionary<Type, Func<object>> lazyRegistrations,
            IServiceProvider scopedProvider)
        {
            _typeRegistrations = typeRegistrations;
            _instanceRegistrations = instanceRegistrations;
            _lazyRegistrations = lazyRegistrations;
            _scopedProvider = scopedProvider;
        }

        public object? Resolve(Type? type)
        {
            if (type == null) return null;

            // Check instance registrations first (singletons)
            if (_instanceRegistrations.TryGetValue(type, out var instance))
            {
                return instance;
            }

            // Check lazy registrations (factories)
            if (_lazyRegistrations.TryGetValue(type, out var factory))
            {
                if (!_lazyCache.TryGetValue(type, out var cached))
                {
                    cached = factory();
                    _lazyCache[type] = cached;
                }
                return cached;
            }

            // Check type registrations (transient services - commands)
            if (_typeRegistrations.TryGetValue(type, out var implementationType))
            {
                return InstantiateType(implementationType, _scopedProvider);
            }

            // Fall back to scoped provider
            return _scopedProvider.GetService(type);
        }

        private object? InstantiateType(Type type, IServiceProvider serviceProvider)
        {
            // Find the best constructor (the one with the most parameters)
            var constructors = type.GetConstructors();
            if (constructors.Length == 0)
            {
                // Use parameterless constructor if available
                return Activator.CreateInstance(type);
            }

            // Use the constructor with the most parameters
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];

            // Resolve each parameter
            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;

                // Check if it's a type we registered (Settings classes, etc.)
                if (_typeRegistrations.TryGetValue(paramType, out var implType))
                {
                    args[i] = InstantiateType(implType, serviceProvider);
                }
                else
                {
                    // Get from scoped provider
                    args[i] = serviceProvider.GetService(paramType);
                }

                if (args[i] == null && !parameters[i].ParameterType.IsClass && !parameters[i].ParameterType.IsInterface)
                {
                    throw new InvalidOperationException(
                        $"Cannot resolve service for type '{parameters[i].ParameterType.Name}' " +
                        $"while activating '{type.Name}'");
                }
            }

            return Activator.CreateInstance(type, args)!;
        }
    }
}
