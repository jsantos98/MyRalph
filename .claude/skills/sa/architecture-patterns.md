# Architecture Patterns Reference

Comprehensive reference of software architecture patterns for the Senior Software Architect.

## Table of Contents

1. [Architectural Styles](#architectural-styles)
2. [Design Patterns](#design-patterns)
3. [Domain-Driven Design](#domain-driven-design)
4. [Database Patterns](#database-patterns)
5. [API Patterns](#api-patterns)
6. [Frontend Patterns](#frontend-patterns)

---

## Architectural Styles

### Clean Architecture (Onion)

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation                         │
│                 (Controllers, DTOs)                     │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                   Application                           │
│              (Services, Use Cases)                      │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                     Domain                              │
│            (Entities, Value Objects)                    │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                  Infrastructure                         │
│        (EF Core, External APIs, File System)            │
└─────────────────────────────────────────────────────────┘

Key Principles:
- Dependencies point inward
- Domain layer has zero external dependencies
- Business logic is independent of frameworks
```

**When to Use:**
- Complex business domains
- Long-term projects
- Teams prioritizing maintainability

**Trade-offs:**
- More initial setup
- More code for simple features
- Steeper learning curve

### Hexagonal Architecture (Ports and Adapters)

```
         ┌─────────────────┐
         │   Application   │
         │     Core        │
         └────────┬────────┘
                  │
      ┌───────────┼───────────┐
      │           │           │
      ▼           ▼           ▼
  ┌──────┐   ┌──────┐   ┌──────┐
  │ Web  │   │  DB  │   │  MQ  │
  │  API │   │ Port │   │ Port │
  └──────┘   └──────┘   └──────┘

Key Principles:
- Application core is isolated
- All I/O goes through ports (interfaces)
- Adapters implement ports for specific technologies
```

**When to Use:**
- Multiple I/O mechanisms needed
- Testing in isolation is critical
- Technology decisions may change

### Event-Driven Architecture

```
┌──────────┐   ┌──────────┐   ┌──────────┐
│ Producer │──▶│  Event   │──▶│ Consumer │
│          │   │  Bus     │   │          │
└──────────┘   └──────────┘   └──────────┘
                    │
                    ▼
              ┌──────────┐
              │ Consumer │
              │          │
              └──────────┘

Key Principles:
- Services communicate via events
- Loose coupling between services
- Asynchronous communication
```

**When to Use:**
- Microservices
- High scalability needs
- Complex business workflows

---

## Design Patterns

### Creational Patterns

#### Factory Method

```csharp
// Abstract creator
public interface IPaymentGatewayFactory
{
    IPaymentGateway Create(PaymentProvider provider);
}

// Concrete creator
public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IServiceProvider _services;

    public IPaymentGateway Create(PaymentProvider provider)
    {
        return provider switch
        {
            PaymentProvider.Stripe => _services.GetRequiredService<StripeGateway>(),
            PaymentProvider.PayPal => _services.GetRequiredService<PayPalGateway>(),
            _ => throw new ArgumentException($"Unknown provider: {provider}")
        };
    }
}

// Usage
var gateway = _factory.Create(PaymentProvider.Stripe);
```

#### Dependency Injection (DI)

```csharp
// ✅ DO: Use constructor injection
public class OrderService
{
    private readonly IOrderRepository _repository;
    private readonly IPaymentGateway _payment;

    public OrderService(IOrderRepository repository, IPaymentGateway payment)
    {
        _repository = repository;
        _payment = payment;
    }
}

// ❌ DON'T: Use service locator
public class OrderService
{
    private readonly IServiceProvider _provider;

    public OrderService(IServiceProvider provider)
    {
        _provider = provider;
    }

    public void CreateOrder()
    {
        var repo = _provider.GetService<IOrderRepository>(); // Hidden dependency
    }
}
```

### Structural Patterns

#### Adapter

```csharp
// Third-party interface we can't change
public interface ILegacyPaymentSystem
{
    void ProcessPayment(string account, decimal amount);
}

// Our domain interface
public interface IPaymentGateway
{
    Task ChargeAsync(CardDetails card, Money amount);
}

// Adapter to bridge the two
public class LegacyPaymentAdapter : IPaymentGateway
{
    private readonly ILegacyPaymentSystem _legacy;

    public async Task ChargeAsync(CardDetails card, Money amount)
    {
        // Adapt our interface to legacy interface
        _legacy.ProcessPayment(card.AccountNumber, amount.Value);
    }
}
```

#### Decorator

```csharp
// Core interface
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan ttl);
}

// Core implementation
public class MemoryCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key) => /* ... */;
    public Task SetAsync<T>(string key, T value, TimeSpan ttl) => /* ... */;
}

// Decorator: Add logging
public class LoggingCacheDecorator : ICacheService
{
    private readonly ICacheService _inner;
    private readonly ILogger _logger;

    public LoggingCacheDecorator(ICacheService inner, ILogger logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        _logger.LogDebug("Cache GET: {Key}", key);
        var result = await _inner.GetAsync<T>(key);
        _logger.LogDebug("Cache HIT: {Hit}", result != null);
        return result;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        _logger.LogDebug("Cache SET: {Key}, TTL: {Ttl}", key, ttl);
        await _inner.SetAsync(key, value, ttl);
    }
}

// Decorator: Add distributed cache
public class DistributedCacheDecorator : ICacheService
{
    private readonly ICacheService _local;
    private readonly IDistributedCache _remote;

    public async Task<T?> GetAsync<T>(string key)
    {
        // Try local first
        var value = await _local.GetAsync<T>(key);
        if (value != null) return value;

        // Fallback to distributed
        var remoteValue = await _remote.GetStringAsync(key);
        return remoteValue != null ? JsonSerializer.Deserialize<T>(remoteValue) : default;
    }
}

// Usage: Stack decorators
var cache = new DistributedCacheDecorator(
    new LoggingCacheDecorator(
        new MemoryCacheService(),
        logger
    ),
    distributedCache
);
```

### Behavioral Patterns

#### Strategy

```csharp
// Strategy interface
public interface IDiscountStrategy
{
    decimal CalculateDiscount(Order order);
}

// Concrete strategies
public class VolumeDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(Order order)
    {
        return order.TotalItems > 100 ? order.Total * 0.15m : 0;
    }
}

public class LoyaltyDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(Order order)
    {
        return order.Customer.LoyaltyYears * 0.02m * order.Total;
    }
}

public class FirstOrderDiscountStrategy : IDiscountStrategy
{
    public decimal CalculateDiscount(Order order)
    {
        return order.Customer.IsFirstOrder ? order.Total * 0.10m : 0;
    }
}

// Context
public class DiscountCalculator
{
    private readonly IEnumerable<IDiscountStrategy> _strategies;

    public decimal CalculateBestDiscount(Order order)
    {
        return _strategies
            .Select(s => s.CalculateDiscount(order))
            .Max();
    }
}
```

#### Specification Pattern

```csharp
// Specification interface
public interface ISpecification<T>
{
    bool IsSatisfiedBy(T entity);
    Expression<Func<T, bool>> ToExpression();
}

// Base implementation
public abstract class Specification<T> : ISpecification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();

    public bool IsSatisfiedBy(T entity)
    {
        var predicate = ToExpression().Compile();
        return predicate(entity);
    }

    // Compose specifications
    public ISpecification<T> And(ISpecification<T> other)
        => new AndSpecification<T>(this, other);

    public ISpecification<T> Or(ISpecification<T> other)
        => new OrSpecification<T>(this, other);

    public ISpecification<T> Not()
        => new NotSpecification<T>(this);
}

// Concrete specifications
public class ActiveUserSpecification : Specification<User>
{
    public override Expression<Func<User, bool>> ToExpression()
        => u => u.IsActive && !u.IsDeleted;
}

public class CreatedAfterSpecification : Specification<User>
{
    private readonly DateTime _cutoff;

    public CreatedAfterSpecification(DateTime cutoff) => _cutoff = cutoff;

    public override Expression<Func<User, bool>> ToExpression()
        => u => u.CreatedAt > _cutoff;
}

public class HasOrdersSpecification : Specification<User>
{
    public override Expression<Func<User, bool>> ToExpression()
        => u => u.Orders.Any();
}

// Usage
var spec = new ActiveUserSpecification()
    .And(new CreatedAfterSpecification(lastMonth))
    .And(new HasOrdersSpecification());

var users = await _userRepository.ListAsync(spec);
```

#### Chain of Responsibility

```csharp
// Handler interface
public interface IRequestHandler<T> where T : class
{
    Task HandleAsync(T request);
    IRequestHandler<T> SetNext(IRequestHandler<T> next);
}

// Base handler
public abstract class RequestHandler<T> : IRequestHandler<T> where T : class
{
    private IRequestHandler<T>? _next;

    public IRequestHandler<T> SetNext(IRequestHandler<T> next)
    {
        _next = next;
        return next;
    }

    public virtual async Task HandleAsync(T request)
    {
        if (_next != null)
            await _next.HandleAsync(request);
    }
}

// Concrete handlers
public class ValidationHandler<T> : RequestHandler<T> where T : IRequest
{
    private readonly IValidator<T> _validator;

    public override async Task HandleAsync(T request)
    {
        var result = await _validator.ValidateAsync(request);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);

        await base.HandleAsync(request);
    }
}

public class LoggingHandler<T> : RequestHandler<T> where T : class
{
    private readonly ILogger _logger;

    public override async Task HandleAsync(T request)
    {
        _logger.LogInformation("Processing: {Request}", request);
        await base.HandleAsync(request);
    }
}

public class TransactionHandler<T> : RequestHandler<T> where T : class
{
    private readonly AppDbContext _context;

    public override async Task HandleAsync(T request)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await base.HandleAsync(request);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}

// Build the chain
var pipeline = new ValidationHandler<CreateOrderCommand>(validator)
    .SetNext(new LoggingHandler<CreateOrderCommand>(logger))
    .SetNext(new TransactionHandler<CreateOrderCommand>(context))
    .SetNext(new CreateOrderHandler(repository));

await pipeline.HandleAsync(command);
```

---

## Domain-Driven Design

### Bounded Contexts

```
┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐
│    Sales        │  │   Inventory     │  │   Shipping      │
│                 │  │                 │  │                 │
│ - Order         │  │ - Product       │  │ - Shipment      │
│ - Customer      │  │ - StockItem     │  │ - Package       │
│ - Payment       │  │ - Warehouse     │  │ - Carrier       │
└─────────────────┘  └─────────────────┘  └─────────────────┘
         │                    │                    │
         └────────────────────┼────────────────────┘
                              │
                    Shared Kernel
                    (Product ID, Currency)
```

### Aggregates

```csharp
// Aggregate Root
public class Order : AggregateRoot<Guid>
{
    private readonly List<OrderLine> _lines = new();

    public Guid CustomerId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public OrderStatus Status { get; private set; }
    public Money Total { get; private set; }

    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    // Domain behavior - not just data accessors
    public void AddLine(Product product, int quantity, Money unitPrice)
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Cannot modify confirmed order");

        var line = new OrderLine(product.Id, quantity, unitPrice);
        _lines.Add(line);

        // Domain event
        AddDomainEvent(new OrderLineAddedEvent(Id, line));
        RecalculateTotal();
    }

    public void Confirm()
    {
        if (Status != OrderStatus.Draft)
            throw new InvalidOperationException("Only draft orders can be confirmed");

        if (_lines.Count == 0)
            throw new InvalidOperationException("Cannot confirm empty order");

        Status = OrderStatus.Confirmed;
        AddDomainEvent(new OrderConfirmedEvent(Id));
    }

    private void RecalculateTotal()
    {
        Total = _lines.Sum(l => l.Total);
    }
}

// Entity within aggregate
public class OrderLine : Entity<Guid>
{
    public Guid ProductId { get; }
    public int Quantity { get; }
    public Money UnitPrice { get; }
    public Money Total => UnitPrice * Quantity;
}
```

### Value Objects

```csharp
// Immutable value object
public record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative");
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required");

        Amount = amount;
        Currency = currency;
    }

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
        => new(money.Amount * multiplier, money.Currency);
}

// Usage
var total = new Money(100, "USD") + new Money(50, "USD");
var discount = total * 0.1m;
```

### Domain Events

```csharp
// Base event
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

// Concrete events
public record OrderConfirmedEvent(Guid OrderId) : DomainEvent;
public record PaymentReceivedEvent(Guid OrderId, Money Amount) : DomainEvent;
public record OrderShippedEvent(Guid OrderId, Guid ShipmentId) : DomainEvent;

// Aggregate root with events
public abstract class AggregateRoot<TId> : Entity<TId>
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent @event)
        => _domainEvents.Add(@event);

    public void ClearDomainEvents()
        => _domainEvents.Clear();
}

// Event handler
public class OrderConfirmedHandler : INotificationHandler<OrderConfirmedEvent>
{
    private readonly IEmailService _email;

    public async Task Handle(OrderConfirmedEvent @event, CancellationToken ct)
    {
        var order = await _repository.GetAsync(@event.OrderId);
        await _email.SendOrderConfirmationAsync(order.Customer.Email, order);
    }
}
```

---

## Database Patterns

### Repository Pattern

```csharp
// Generic repository
public interface IRepository<T, TId> where T : AggregateRoot<TId>
{
    Task<T?> GetByIdAsync(TId id);
    Task<IReadOnlyList<T>> ListAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

// Specification support
public interface IRepository<T, TId> where T : AggregateRoot<TId>
{
    Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec);
    Task<T?> FirstOrDefaultAsync(ISpecification<T> spec);
    Task<int> CountAsync(ISpecification<T> spec);
}

// EF Core implementation
public class EfRepository<T, TId> : IRepository<T, TId> where T : AggregateRoot<TId>
{
    private readonly AppDbContext _context;
    private readonly DbSet<T> _dbSet;

    public async Task<IReadOnlyList<T>> ListAsync(ISpecification<T> spec)
    {
        var query = ApplySpecification(spec);
        return await query.ToListAsync();
    }

    private IQueryable<T> ApplySpecification(ISpecification<T> spec)
    {
        var query = _dbSet.AsQueryable();
        return spec == null ? query : query.Where(spec.ToExpression());
    }
}
```

### Unit of Work Pattern

```csharp
public interface IUnitOfWork : IDisposable
{
    IRepository<Order, Guid> Orders { get; }
    IRepository<Customer, Guid> Customers { get; }
    Task<int> CommitAsync();
}

public class EfUnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public IRepository<Order, Guid> Orders { get; }
    public IRepository<Customer, Guid> Customers { get; }

    public async Task<int> CommitAsync()
    {
        // Dispatch domain events before commit
        await DispatchDomainEventsAsync();

        return await _context.SaveChangesAsync();
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEvents = _context.ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var @event in domainEvents)
        {
            await _mediator.Publish(@event);
        }

        foreach (var entry in _context.ChangeTracker.Entries<AggregateRoot<Guid>>())
        {
            entry.Entity.ClearDomainEvents();
        }
    }
}
```

### Migration Strategy

```csharp
// Versioned migrations
public abstract class Migration
{
    public abstract string Name { get; }
    public abstract string UpSql { get; }
    public abstract string DownSql { get; }
}

public class CreateUserTableMigration : Migration
{
    public override string Name => "001_create_users_table";

    public override string UpSql => @"
        CREATE TABLE users (
            id UUID PRIMARY KEY,
            email VARCHAR(255) UNIQUE NOT NULL,
            password_hash VARCHAR(255) NOT NULL,
            created_at TIMESTAMP NOT NULL DEFAULT NOW(),
            updated_at TIMESTAMP NOT NULL DEFAULT NOW()
        );

        CREATE INDEX idx_users_email ON users(email);
    ";

    public override string DownSql => @"
        DROP INDEX IF EXISTS idx_users_email;
        DROP TABLE IF EXISTS users;
    ";
}
```

---

## API Patterns

### RESTful API Design

```
GET    /api/users          → List users (paginated)
GET    /api/users/{id}     → Get specific user
POST   /api/users          → Create user
PUT    /api/users/{id}     → Replace user
PATCH  /api/users/{id}     → Partial update
DELETE /api/users/{id}     → Delete user

GET    /api/users/{id}/orders → Get user's orders
POST   /api/users/{id}/orders → Create order for user
```

### HATEOAS Links

```csharp
public record Resource<T>
{
    public T Data { get; init; }
    public Dictionary<string, Link> Links { get; init; }
}

public record Link
{
    public string Href { get; init; }
    public string Method { get; init; }
}

public class UserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; }

    public Resource<UserResponse> ToResource()
    {
        return new Resource<UserResponse>
        {
            Data = this,
            Links = new()
            {
                ["self"] = new() { Href = $"/api/users/{Id}", Method = "GET" },
                ["orders"] = new() { Href = $"/api/users/{Id}/orders", Method = "GET" },
                ["update"] = new() { Href = $"/api/users/{Id}", Method = "PATCH" },
                ["delete"] = new() { Href = $"/api/users/{Id}", Method = "DELETE" }
            }
        };
    }
}
```

---

## Frontend Patterns

### Container/Presentational Pattern

```typescript
// Presentational component (dumb)
interface LoginFormProps {
  email: string;
  password: string;
  onEmailChange: (email: string) => void;
  onPasswordChange: (password: string) => void;
  onSubmit: () => void;
  isLoading: boolean;
  error?: string;
}

export function LoginForm({
  email,
  password,
  onEmailChange,
  onPasswordChange,
  onSubmit,
  isLoading,
  error
}: LoginFormProps) {
  return (
    <form onSubmit={(e) => { e.preventDefault(); onSubmit(); }}>
      <input
        value={email}
        onChange={(e) => onEmailChange(e.target.value)}
        type="email"
        placeholder="Email"
      />
      <input
        value={password}
        onChange={(e) => onPasswordChange(e.target.value)}
        type="password"
        placeholder="Password"
      />
      {error && <div className="error">{error}</div>}
      <button disabled={isLoading} type="submit">
        {isLoading ? 'Logging in...' : 'Login'}
      </button>
    </form>
  );
}

// Container component (smart)
export function LoginContainer() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const { login, isLoading, error } = useAuth();

  const handleSubmit = useCallback(() => {
    login.mutate({ email, password });
  }, [email, password, login]);

  return (
    <LoginForm
      email={email}
      password={password}
      onEmailChange={setEmail}
      onPasswordChange={setPassword}
      onSubmit={handleSubmit}
      isLoading={isLoading}
      error={error?.message}
    />
  );
}
```

### Custom Hooks for Logic Reuse

```typescript
// Custom hook for authentication
export function useAuth() {
  const queryClient = useQueryClient();
  const [user, setUser] = useState<User | null>(null);

  const login = useMutation({
    mutationFn: (credentials: LoginCredentials) =>
      authApi.login(credentials),
    onSuccess: (data) => {
      setUser(data.user);
      queryClient.invalidateQueries({ queryKey: ['currentUser'] });
    }
  });

  const logout = useCallback(() => {
    authApi.logout();
    setUser(null);
    queryClient.clear();
  }, [queryClient]);

  return { user, login, logout, isAuthenticated: !!user };
}
```

### Compound Components Pattern

```typescript
// Usage
<Dialog>
  <DialogTrigger>Open Dialog</DialogTrigger>
  <DialogContent>
    <DialogTitle>Confirm Action</DialogTitle>
    <DialogDescription>
      Are you sure you want to proceed?
    </DialogDescription>
    <DialogFooter>
      <DialogClose>Cancel</DialogClose>
      <DialogAction>Confirm</DialogAction>
    </DialogFooter>
  </DialogContent>
</Dialog>

// Implementation
const DialogContext = createContext<DialogContextValue>({});

export function Dialog({ children }: DialogProps) {
  const [open, setOpen] = useState(false);
  return (
    <DialogContext.Provider value={{ open, setOpen }}>
      {children}
    </DialogContext.Provider>
  );
}

export function DialogTrigger({ children }: { children: ReactNode }) {
  const { setOpen } = useContext(DialogContext);
  return <button onClick={() => setOpen(true)}>{children}</button>;
}
```

---

## Choosing the Right Pattern

| Scenario | Recommended Pattern |
|----------|-------------------|
| Complex business logic | Clean Architecture + DDD |
| Multiple data sources | Hexagonal Architecture |
| High read/write ratio asymmetry | CQRS |
| Complex workflow | Event-Driven Architecture |
| Need extensibility | Strategy Pattern |
| Cross-cutting concerns | Decorator Pattern |
| Complex queries | Specification Pattern |
| API versioning | Adapter Pattern |
| Testing in isolation | Dependency Injection |
| Domain events | Observer/Pub-Sub |
