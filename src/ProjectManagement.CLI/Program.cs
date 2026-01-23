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
                services.AddDbContext<ProjectManagementDbContext>();

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
        private bool _disposed = false;

        public TypeRegistrar(IServiceProvider services)
        {
            // Create a scope to properly manage scoped services
            _scope = services.CreateScope();
        }

        public ITypeResolver Build()
        {
            return new TypeResolver(_scope.ServiceProvider);
        }

        public void Register(Type service, Type implementation)
        {
            // Services are registered via DI container
        }

        public void RegisterInstance(Type service, object implementation)
        {
            // Services are registered via DI container
        }

        public void RegisterLazy(Type service, Func<object> factory)
        {
            // Services are registered via DI container
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
        private readonly IServiceProvider _provider;

        public TypeResolver(IServiceProvider provider)
        {
            _provider = provider;
        }

        public object? Resolve(Type? type)
        {
            if (type == null) return null;
            return _provider.GetService(type);
        }
    }
}
