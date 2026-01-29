# FelizesTracker

A comprehensive tracking system built with Clean Architecture principles on .NET 8 backend and React 18 with TypeScript frontend.

## Architecture

This project follows **Clean Architecture (Onion)** principles and is organized as a monorepo with separate backend and frontend projects.

### Backend Architecture (.NET 8)

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation                         │
│                 (Api - Controllers, DTOs)               │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                   Application                           │
│              (Services, Use Cases, Interfaces)          │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                     Domain                              │
│            (Entities, Value Objects)                    │
└─────────────────────┬───────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────┐
│                  Infrastructure                         │
│        (EF Core, External APIs, Data Access)            │
└─────────────────────────────────────────────────────────┘
```

### Project Structure

```
MyRalph/
├── backend/
│   └── src/
│       ├── FelizesTracker.sln           # Solution file
│       ├── FelizesTracker.Api/          # Presentation layer - Web API
│       ├── FelizesTracker.Application/  # Application layer - Business logic
│       ├── FelizesTracker.Core/         # Domain layer - Entities and value objects
│       └── FelizesTracker.Infrastructure/# Infrastructure - Data access, external services
├── frontend/
│   ├── src/                            # React application source
│   ├── public/                         # Static assets
│   ├── package.json                    # NPM dependencies
│   └── vite.config.ts                  # Vite configuration
├── .editorconfig                       # PO Team code formatting standards
├── .gitignore                          # Git ignore rules
└── README.md                           # This file
```

## Tech Stack

### Backend
- **.NET 8** - Latest LTS .NET framework
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core** - ORM for data access
- **C# 12** - Language features
- **xUnit** - Testing framework
- **Clean Architecture** - Onion architecture pattern

### Frontend
- **React 18** - UI library
- **TypeScript 5** - Type-safe JavaScript
- **Vite 5** - Build tool and dev server
- **React Router** - Client-side routing
- **Axios** - HTTP client
- **ESLint + Prettier** - Code quality and formatting

## Local Development Setup

### Prerequisites

#### Backend
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- Any IDE (Visual Studio 2022, VS Code with C# extension, or Rider)

#### Frontend
- [Node.js 18+](https://nodejs.org/) (LTS version recommended)
- npm or yarn

### Getting Started

#### 1. Clone the Repository

```bash
git clone <repository-url>
cd MyRalph
```

#### 2. Backend Setup

```bash
cd backend/src

# Restore NuGet packages
dotnet restore

# Build the solution
dotnet build

# Run the API (from Api project directory)
cd FelizesTracker.Api
dotnet run
```

The API will be available at `http://localhost:5000`

#### 3. Frontend Setup

```bash
# From repository root
cd frontend

# Install dependencies
npm install

# Start development server
npm run dev
```

The frontend will be available at `http://localhost:5173`

### Available Scripts

#### Backend

```bash
# Build the entire solution
dotnet build

# Run tests
dotnet test

# Run the API
cd FelizesTracker.Api
dotnet run

# Restore packages
dotnet restore

# Clean build artifacts
dotnet clean
```

#### Frontend

```bash
# Install dependencies
npm install

# Start development server (with hot module replacement)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run type checking
npm run type-check

# Run linter
npm run lint

# Run linter with auto-fix
npm run lint:fix

# Run tests
npm run test
```

## Development Workflow

### Code Formatting

This project uses `.editorconfig` for consistent code formatting across the team. Most modern IDEs will apply these settings automatically.

- **Visual Studio**: Settings are applied automatically
- **VS Code**: Install the [EditorConfig](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig) extension
- **JetBrains IDEs**: Settings are applied automatically

### Backend Development

1. **Domain Layer (Core)**: Add entities, value objects, and domain logic
2. **Application Layer**: Add interfaces, use cases, and business logic
3. **Infrastructure Layer**: Implement interfaces from Application layer
4. **API Layer**: Create controllers and DTOs to expose functionality

### Frontend Development

1. **Components**: Create reusable React components in `src/components`
2. **Pages**: Add page-level components in `src/pages`
3. **Services**: Create API client services in `src/services`
4. **Hooks**: Create custom React hooks in `src/hooks`
5. **Types**: Define TypeScript types in `src/types`

## Testing

### Backend Tests

```bash
cd backend/src
dotnet test
```

### Frontend Tests

```bash
cd frontend
npm run test
```

## Code Quality

### PO Team Standards

This project follows PO Team architecture patterns and coding standards:

- Clean Architecture principles
- Repository pattern for data access
- Dependency injection throughout
- Specification pattern for complex queries
- Domain events for cross-cutting concerns
- 80%+ test coverage requirement
- Semantic versioning for releases

### Code Review Checklist

- [ ] Code follows Clean Architecture principles
- [ ] Dependencies point inward (no violations)
- [ ] Tests included with 80%+ coverage
- [ ] TypeScript strict mode compliance
- [ ] EditorConfig standards applied
- [ ] No hardcoded configuration values
- [ ] Proper error handling and logging
- [ ] Documentation updated as needed

## Branch Strategy

- `main` - Production branch
- `feature/felizes-tracker-bootstrap` - Current feature branch
- `feature/*` - Feature branches
- `bugfix/*` - Bug fix branches

## Contributing

1. Create a feature branch from `main`
2. Implement your feature following Clean Architecture
3. Write tests with 80%+ coverage
4. Ensure code formatting follows `.editorconfig`
5. Submit pull request for review
6. Address feedback from code review
7. Merge after approval

## License

[Add your license here]

## Contact

[Add contact information]

## Roadmap

### Phase 1: Bootstrap (Current)
- [x] Repository structure
- [x] Backend solution setup
- [x] Frontend project setup
- [ ] EditorConfig configuration
- [ ] Git ignore configuration
- [ ] README documentation

### Phase 2: Core Features
- [ ] Authentication & Authorization
- [ ] User management
- [ ] Dashboard
- [ ] Basic tracking features

### Phase 3: Advanced Features
- [ ] Real-time updates
- [ ] Reporting & Analytics
- [ ] Notifications
- [ ] Mobile responsiveness

## Resources

- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [React Documentation](https://react.dev/)
- [TypeScript Documentation](https://www.typescriptlang.org/docs/)
- [Vite Documentation](https://vitejs.dev/)
- [Clean Architecture by Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
