# .NET/C# Patterns Reference

Comprehensive reference of .NET patterns and best practices for the Senior Backend Developer.

## Table of Contents

1. [Dependency Injection](#dependency-injection)
2. [Entity Framework Core](#entity-framework-core)
3. [ASP.NET Core API Patterns](#aspnet-core-api-patterns)
4. [Async/Await Patterns](#asyncawait-patterns)
5. [Error Handling](#error-handling)
6. [Configuration](#configuration)
7. [Logging](#logging)
8. [Security](#security)

---

## Dependency Injection

### Service Lifetimes

```csharp
// ✅ Transient: New instance each time (lightweight, stateless)
services.AddTransient<IEmailService, SmtpEmailService>();

// ✅ Scoped: One instance per HTTP request (DbContext, repositories)
services.AddScoped<IUserRepository, EfUserRepository>();
services.AddScoped<AppDbContext>();

// ✅ Singleton: One instance for app lifetime (stateless services)
services.AddSingleton<ICacheService, MemoryCacheService>();
services.AddSingleton<IPasswordHasher, ArgonPasswordHasher>();

// ✅ Transient with factory (for complex creation)
services.AddTransient(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var apiKey = config["ExternalApi:Key"];
    return new ExternalApiClient(apiKey);
});
```

### Constructor Injection (Preferred)

```csharp
// ✅ DO: All dependencies via constructor
public class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IPaymentGateway _payment;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orders,
        IProductRepository products,
        IPaymentGateway payment,
        ILogger<OrderService> logger)
    {
        _orders = orders;
        _products = products;
        _payment = payment;
        _logger = logger;
    }
}

// ❌ DON'T: Service locator pattern
public class OrderService
{
    private readonly IServiceProvider _provider;

    public OrderService(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void CreateOrder()
    {
        var repo = _provider.GetRequiredService<IOrderRepository>(); // Hidden dependency
    }
}
```

---

## Entity Framework Core

### DbContext Configuration

```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Product> Products => Set<Product>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Global query filters (soft delete)
        modelBuilder.Entity<User>()
            .HasQueryFilter(u => !u.IsDeleted);

        // Configure relationships
        modelBuilder.Entity<Order>()
            .HasOne(o => o.Customer)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Entity Configuration

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255)
            .HasConversion(
                email => email.ToLowerInvariant(),
                email => email);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW() AT TIME ZONE 'utc'");

        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email");

        // Value object conversion
        builder.OwnsOne(u => u.ShippingAddress, a =>
        {
            a.Property(sa => sa.Street).HasColumnName("shipping_street");
            a.Property(sa => sa.City).HasColumnName("shipping_city");
            a.Property(sa => sa.ZipCode).HasColumnName("shipping_zip");
        });

        // Many-to-many with join entity
        builder.HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity<UserRole>(
                j => j.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RoleId),
                j => j.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UserId));
    }
}
```

### Query Patterns

```csharp
// ✅ GOOD: Projection with Select (efficient)
public async Task<UserDto?> GetUserAsync(Guid id, CancellationToken ct)
{
    return await _context.Users
        .Where(u => u.Id == id)
        .Select(u => new UserDto
        {
            Id = u.Id,
            Email = u.Email,
            FullName = u.FirstName + " " + u.LastName,
            OrderCount = u.Orders.Count
        })
        .FirstOrDefaultAsync(ct);
}

// ✅ GOOD: Eager loading with Include
public async Task<User?> GetUserWithOrdersAsync(Guid id, CancellationToken ct)
{
    return await _context.Users
        .Include(u => u.Orders)
            .ThenInclude(o => o.Items)
        .FirstOrDefaultAsync(u => u.Id == id, ct);
}

// ✅ GOOD: Split query for complex includes (prevents cartesian explosion)
public async Task<User?> GetUserWithEverythingAsync(Guid id, CancellationToken ct)
{
    return await _context.Users
        .Include(u => u.Orders)
        .Include(u => u.Roles)
        .AsSplitQuery()
        .FirstOrDefaultAsync(u => u.Id == id, ct);
}

// ❌ BAD: N+1 query problem
public async Task<List<UserDto>> GetUsersWithOrdersAsync()
{
    var users = await _context.Users.ToListAsync();

    var result = new List<UserDto>();
    foreach (var user in users)
    {
        // N+1: One query for users, then N queries for orders
        var orders = await _context.Orders
            .Where(o => o.UserId == user.Id)
            .ToListAsync();

        result.Add(new UserDto { ... });
    }
    return result;
}
```

### Raw SQL Queries

```csharp
// ✅ GOOD: Parameterized query (safe from SQL injection)
public async Task<List<User>> GetActiveUsersAsync(DateTime since, CancellationToken ct)
{
    return await _context.Users
        .FromSqlRaw(
            "SELECT * FROM users WHERE is_active = true AND created_at > {0}",
            since
        )
        .ToListAsync(ct);
}

// ✅ GOOD: Parameterized query with SqlParameter
public async Task<List<User>> SearchUsersAsync(string term, CancellationToken ct)
{
    var pattern = $"%{term}%";
    return await _context.Users
        .FromSqlRaw(
            "SELECT * FROM users WHERE email LIKE {0} OR name LIKE {0}",
            pattern
        )
        .ToListAsync(ct);
}

// ❌ BAD: String interpolation (SQL injection risk!)
public async Task<List<User>> SearchUsersBadAsync(string term)
{
    return await _context.Users
        .FromSqlRaw($"SELECT * FROM users WHERE email LIKE '%{term}%'")  // DANGEROUS!
        .ToListAsync();
}
```

### Migration Best Practices

```csharp
// Create migration
dotnet ef migrations add AddUserTable

// Generate SQL script (for review)
dotnet ef migrations script 2024-01-01-00-00-00_AddUserTable

// Apply migration
dotnet ef database update

// Rollback to specific migration
dotnet ef database update 2023-12-01-00-00-00_AddUserTable
```

---

## ASP.NET Core API Patterns

### Controller Best Practices

```csharp
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class UsersController : ControllerBase
{
    private readonly IUserService _users;
    private readonly ILogger<UsersController> _logger;

    /// <summary>
    /// Gets a user by ID
    /// </summary>
    /// <param name="id">User ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>User details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(
        Guid id,
        CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(id, ct);
        if (user is null)
            return NotFound(new ErrorResponse
            {
                Code = "USER_NOT_FOUND",
                Message = $"User with ID {id} not found"
            });

        return Ok(user);
    }
}
```

### Request Validation

```csharp
// FluentValidation validator
public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches("[A-Z]").WithMessage("Must contain uppercase letter")
            .Matches("[a-z]").WithMessage("Must contain lowercase letter")
            .Matches("[0-9]").WithMessage("Must contain digit")
            .Matches("[^a-zA-Z0-9]").WithMessage("Must contain special character");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords must match");
    }
}

// Register in Program.cs
services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

// Auto-validate in controller
services.AddFluentValidationAutoValidation();
```

### Response Caching

```csharp
[HttpGet("{id:guid}")]
[ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any)]
public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken ct)
{
    // Response will be cached for 60 seconds
}

// Or use Redis cache
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = config.GetConnectionString("Redis");
});
```

---

## Async/Await Patterns

### ConfigureAwait

```csharp
// ✅ In library code: Use ConfigureAwait(false)
public async Task<User> GetUserAsync(Guid id, CancellationToken ct)
{
    return await _repository.GetByIdAsync(id, ct)
        .ConfigureAwait(false);  // Don't capture context
}

// ✅ In ASP.NET Core: No ConfigureAwait needed (context is required)
[HttpGet("{id}")]
public async Task<ActionResult<User>> GetUser(Guid id, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(id, ct);
    return Ok(user);
}
```

### CancellationToken

```csharp
// ✅ Always accept and pass CancellationToken
public async Task<IReadOnlyList<User>> ListUsersAsync(CancellationToken ct)
{
    return await _context.Users
        .ToListAsync(ct);  // Pass token through
}

// ✅ For long operations, check token
public async Task<string> ProcessLargeFileAsync(string path, CancellationToken ct)
{
    using var reader = new StreamReader(path);
    var content = new StringBuilder();

    while (!reader.EndOfStream)
    {
        ct.ThrowIfCancellationRequested();  // Check if cancelled
        var line = await reader.ReadLineAsync(ct);
        content.AppendLine(line);
    }

    return content.ToString();
}
```

### Async Streams

```csharp
// ✅ Yield return with async
public async IAsyncEnumerable<User> StreamUsersAsync(
    [EnumeratorCancellation] CancellationToken ct = default)
{
    await foreach (var user in _users.QueryAsync(ct))
    {
        yield return user;
    }
}

// Consume
await foreach (var user in _users.StreamUsersAsync(ct))
{
    ProcessUser(user);
}
```

---

## Error Handling

### Custom Exception Types

```csharp
// Domain exception
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}

public class NotFoundException : DomainException
{
    public NotFoundException(string resource, object key)
        : base("NOT_FOUND", $"{resource} with key {key} not found")
    {
    }
}

public class ValidationException : DomainException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("VALIDATION_ERROR", "One or more validation errors occurred")
    {
        Errors = errors;
    }
}

// Usage
public async Task<User> GetUserAsync(Guid id, CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(id, ct);
    if (user is null)
        throw new NotFoundException("User", id);
    return user;
}
```

### Global Exception Handler

```csharp
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, code, message) = exception switch
        {
            DomainException de => (StatusCodes.Status400BadRequest, de.Code, de.Message),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "UNAUTHORIZED", "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "NOT_FOUND", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "ERROR", "An error occurred")
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Code = code,
            Message = message,
            TraceId = httpContext.TraceIdentifier
        }, cancellationToken);

        return true;
    }
}
```

---

## Configuration

### Options Pattern

```csharp
// appsettings.json
{
  "Jwt": {
    "Issuer": "https://api.example.com",
    "Audience": "https://example.com",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}

// Options class
public class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }
}

// Register
services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Use
public class JwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }
}
```

---

## Logging

### Structured Logging

```csharp
// ✅ GOOD: Structured logging
public async Task<User> CreateUserAsync(CreateUserRequest request, CancellationToken ct)
{
    _logger.LogInformation("Creating user with email: {Email}", request.Email);

    var user = User.Create(request.Email, request.PasswordHash);
    user = await _repository.AddAsync(user, ct);

    _logger.LogInformation("User created successfully: {UserId} {Email}", user.Id, user.Email);

    return user;
}

// ❌ BAD: String interpolation (not structured)
_logger.LogInformation($"Creating user with email: {request.Email}");
```

### Log Levels

```csharp
// Trace: Most detailed
_logger.LogDebug("Processing request: {RequestId}", requestId);

// Debug: Internal flow
_logger.LogDebug("Repository call: {RepoMethod}", "GetByIdAsync");

// Information: Important events
_logger.LogInformation("User logged in: {UserId}", userId);

// Warning: Something unexpected but not error
_logger.LogWarning("Rate limit exceeded for IP: {IP}", ipAddress);

// Error: Error that can be handled
_logger.LogError(exception, "Failed to process payment for order: {OrderId}", orderId);

// Critical: System-wide failure
_logger.LogCritical(exception, "Database connection lost");
```

---

## Security

### Authentication & Authorization

```csharp
// Configure authentication
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };
    });

// Authorization policy
services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));

    options.AddPolicy("CanManageUsers", policy =>
        policy.RequireClaim("permission", "users.manage"));
});

// Use in controller
[Authorize]
[HttpGet("{id}")]
public async Task<ActionResult<User>> GetUser(Guid id)
{
    // Only authenticated users
}

[Authorize(Policy = "AdminOnly")]
[HttpDelete("{id}")]
public async Task<ActionResult> DeleteUser(Guid id)
{
    // Only admins
}
```

### Data Protection

```csharp
// Hash passwords
public class PasswordHasher
{
    public string Hash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool Verify(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}

// Sensitive data protection
public class DataProtector
{
    private readonly IDataProtector _protector;

    public DataProtector(IDataProtectionProvider protector)
    {
        _protector = protector.CreateProtector("MyPurpose");
    }

    public string Protect(string plainText)
        => _protector.Protect(plainText);

    public string Unprotect(string cipherText)
        => _protector.Unprotect(cipherText);
}
```

---

## Performance Tips

1. **Use async/await correctly** - Don't block on async code
2. **Project queries with Select** - Avoid fetching unnecessary columns
3. **Use pagination** - Don't return all records
4. **Cache frequently accessed data** - Use IMemoryCache or IDistributedCache
5. **Compile Regex for repeated use** - `Regex.CompileToAssembly()`
6. **Use ArrayPool<T>** - For large temporary arrays
7. **Use Span<T>** - For zero-allocation string manipulation
8. **Disable Entity Framework query tracking** - `.AsNoTracking()` for read-only
