# PO Team Shared Protocols and Knowledge

This file contains shared protocols, conventions, and knowledge used by all agents in the PO Team system.

## Agent Roster

All agents should be aware of the full team composition:

| Agent | Role | Capabilities | Tech Stack |
|-------|------|--------------|------------|
| **PM** | Orchestrator | Requirements, coordination, Asana | Project management |
| **SA** | Technical Lead | Architecture, reviews, task splitting | Full-stack |
| **BE** | Backend Developer | .NET/C#, APIs, security, performance | .NET 8+, C# 12 |
| **FE** | Frontend Developer | React, TypeScript, responsive UI | React 18+, TS |
| **DB** | Database Developer | PostgreSQL, optimization, migrations | PostgreSQL 16+ |
| **UX** | UX Designer | Mockups, Figma, design systems | Figma, design tools |
| **QA** | QA Tester | Integration tests, e2e, coverage | Playwright, xUnit |

## Communication Protocols

### Task Handoff Format

When delegating work, use this format:

```
TASK: [Brief title]
CONTEXT:
  - Feature: [Feature name]
  - Branch: [feature/xxx]
  - Asana Task: [task URL or ID]
  - Dependencies: [what must be done first]

REQUIREMENTS:
  1. [Specific requirement 1]
  2. [Specific requirement 2]

ACCEPTANCE CRITERIA:
  - [ ] [Criterion 1]
  - [ ] [Criterion 2]

DELIVERABLES:
  - [Code files or artifacts to deliver]
  - [Tests to write]
  - [Documentation to update]

CONSTRAINTS:
  - [Technology constraints]
  - [Security considerations]
  - [Performance requirements]
```

### Status Report Format

When reporting back, use this format:

```
STATUS: [completed | in_progress | blocked]
TASK: [Task title]

PROGRESS:
  - [What has been accomplished]

DELIVERED:
  - [Commit hash: xxxxx]
  - [Files changed: x]
  - [Test coverage: X%]

BLOCKERS (if any):
  - [Blocker description]
  - [Suggested resolution]

NEXT:
  - [Recommended next step]
```

## Git Conventions

### Branch Naming

```
feature/[feature-name]     # Main feature branch (SA creates)
fix/[bug-name]             # Bug fix branch
hotfix/[critical-fix]      # Production hotfix
```

### Commit Message Format

```
[AGENT-ID] type(scope): description

Body (optional):
  - Additional context
  - References to Asana tasks
  - Breaking changes notes

Footer (optional):
  Co-Authored-By: Claude (GLM-4.7) <noreply@anthropic.com>
```

**Types:** feat, fix, perf, refactor, test, docs, chore

**Examples:**
```
[BE] feat(auth): add JWT token refresh endpoint

Implements automatic token refresh 5 minutes before expiration.
Related: Asana #123456

Co-Authored-By: Claude (GLM-4.7) <noreply@anthropic.com>
```

```
[FE] fix(cart): resolve race condition in add to cart

Users clicking rapidly could create duplicate items.
Now debouncing add operations with 300ms delay.
Related: Asana #123457
```

## Code Quality Standards

### .NET/C# Standards

```csharp
// ✅ DO: Use dependency injection
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}

// ❌ DON'T: Use static service locators
public class UserService
{
    public void DoSomething()
    {
        var repo = ServiceLocator.GetService<IUserRepository>();
    }
}
```

### React/TypeScript Standards

```typescript
// ✅ DO: Use proper typing and hooks
interface UserProps {
  userId: string;
  onUpdate: (user: User) => void;
}

export function UserProfile({ userId, onUpdate }: UserProps) {
  const [user, setUser] = useState<User | null>(null);
  const { data, error, isLoading } = useQuery({
    queryKey: ['user', userId],
    queryFn: () => fetchUser(userId)
  });

  // Component logic...
}

// ❌ DON'T: Use any types or ignore errors
export function UserProfile(props: any) {
  const [user, setUser] = useState(null);
  // No error handling...
}
```

### Database Standards

```sql
-- ✅ DO: Use indexed queries with proper pagination
SELECT u.id, u.name, u.email
FROM users u
WHERE u.status = 'active'
  AND u.created_at > @last_month
ORDER BY u.created_at DESC
LIMIT 50 OFFSET @page_offset;

-- ❌ DON'T: Use SELECT * or N+1 queries
SELECT * FROM users;
-- Then loop and fetch related items individually
```

## Security Best Practices

### Backend Security
- Validate ALL input parameters
- Use parameterized queries (no SQL injection)
- Implement rate limiting on public endpoints
- Never log sensitive data (passwords, tokens, PII)
- Use HTTPS everywhere
- Implement proper CORS policies
- Validate file uploads (type, size, content)

### Frontend Security
- Store tokens securely (httpOnly cookies or secure storage)
- Implement CSRF protection
- Sanitize user input before display
- Use Content Security Policy headers
- Implement proper authentication checks
- Never expose sensitive data in client code

### Authentication Flow
```
1. User submits credentials
2. Backend validates and returns JWT (access + refresh tokens)
3. Frontend stores tokens securely
4. Each request includes Bearer token
5. Backend validates token on each request
6. On token expiration, use refresh token
7. On refresh token expiration, re-authenticate
```

## Test Coverage Requirements

### Coverage Goals by Layer

| Layer | Target Coverage | Critical Paths |
|-------|-----------------|----------------|
| Controllers/API | 90%+ | All endpoints must be covered |
| Services/Business Logic | 95%+ | All business rules, edge cases |
| Repositories/Data | 85%+ | All query methods |
| Components | 85%+ | All user interactions |
| Integration | 100% | All user flows |

### Test Types

```csharp
// ✅ Unit Test - Isolated, fast
[Fact]
public async Task GetUserById_WhenUserExists_ReturnsUser()
{
    // Arrange
    var mockRepo = new Mock<IUserRepository>();
    mockRepo.Setup(r => r.GetByIdAsync(1))
        .ReturnsAsync(new User { Id = 1, Name = "Test" });
    var service = new UserService(mockRepo.Object);

    // Act
    var result = await service.GetUserByIdAsync(1);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("Test", result.Name);
}

// ✅ Integration Test - Real dependencies
[Fact]
public async Task CreateUser_WithValidData_SavesToDatabase()
{
    // Uses test database
    // Tests full stack
}

// ✅ E2E Test - Full user flow
test("user can complete checkout flow", async ({ page }) => {
  // Full browser automation
});
```

## Error Handling Patterns

### Standard Error Response

```csharp
public class ErrorResponse
{
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, string[]> Errors { get; set; }
    public string TraceId { get; set; }
}

// Usage
return BadRequest(new ErrorResponse
{
    Code = "VALIDATION_ERROR",
    Message = "One or more validation errors occurred",
    Errors = new Dictionary<string, string[]>
    {
        ["email"] = new[] { "Email is required", "Email must be valid" }
    },
    TraceId = HttpContext.TraceIdentifier
});
```

### Frontend Error Handling

```typescript
// ✅ DO: Handle errors gracefully
try {
  const result = await apiCall();
  setData(result.data);
} catch (error) {
  if (axios.isAxiosError(error)) {
    if (error.response?.status === 401) {
      // Redirect to login
    } else if (error.response?.status === 403) {
      // Show access denied
    } else {
      // Show generic error
      showError(error.response?.data?.message || 'An error occurred');
    }
  }
  setError(error);
}
```

## Performance Guidelines

### Backend Performance
- Use async/await for I/O operations
- Implement pagination for list endpoints (max 100 items per page)
- Cache frequently accessed data
- Use compression for API responses
- Profile slow queries with EXPLAIN ANALYZE
- Implement response compression (gzip/brotli)

### Frontend Performance
- Lazy load routes and components
- Optimize images (WebP, proper sizing)
- Implement virtual scrolling for long lists
- Debounce search inputs (300ms)
- Use React.memo appropriately
- Minimize re-renders

### Database Performance
- Create appropriate indexes
- Use connection pooling
- Avoid N+1 queries
- Use prepared statements
- Monitor query performance
- Partition large tables when needed

## Asana Integration

### Task States

```
Draft → To Do → In Progress → Ready for Review → In Review → Ready for PO → Accepted/Rejected
```

### Asana Task Templates

**Development Task:**
```
Name: [Agent] [Action] [Component]
Description:
  Requirements: [from SA breakdown]
  Acceptance Criteria: [list]
  Branch: feature/xxx
  Related Tasks: [links]
```

**Code Review Task:**
```
Name: SA Review: [Feature/Subtask]
Description:
  PR: [link]
  Coverage Target: 80%+
  Checklist: [from SA review checklist]
```

## Common Commands Reference

### Git Commands
```bash
# Create feature branch
git checkout -b feature/feature-name

# Commit with format
git commit -m "[BE] feat(api): add user endpoint"

# Push and create PR
git push -u origin feature/feature-name
gh pr create --title "Feature: User authentication" --body "..."
```

### .NET Commands
```bash
# Create new API project
dotnet new webapi -n MyProject

# Run tests
dotnet test

# Check coverage
dotnet test --collect:"XPlat Code Coverage"

# Build release
dotnet build -c Release
```

### React Commands
```bash
# Create new project
npm create vite@latest my-app -- --template react-ts

# Run tests
npm test

# Test coverage
npm test -- --coverage

# Build for production
npm run build
```

## Tracing and Debugging

### Log Format

```csharp
// Structured logging
_logger.LogInformation("Processing request for user {UserId}", userId);
_logger.LogWarning("Rate limit exceeded for IP {IP}", ipAddress);
_logger.LogError(exception, "Failed to process payment for order {OrderId}", orderId);
```

### Frontend Debugging

```typescript
// Use console.group for related logs
console.group('User Login Flow');
console.log('Email:', email);
console.log('Timestamp:', new Date().toISOString());
console.error('Error:', error);
console.groupEnd();
```

## Release Checklist

### Before Release
- [ ] All features tested and validated
- [ ] Test coverage above 80%
- [ ] Security audit passed
- [ ] Performance benchmarks met
- [ ] Documentation updated
- [ ] Migration scripts tested
- [ ] Rollback plan documented

### After Release
- [ ] Monitor error rates
- [ ] Check performance metrics
- [ ] Verify database integrity
- [ ] Validate integrations
- [ ] Update release notes
- [ ] Tag release in git
