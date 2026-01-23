# .NET CLI Project Management System

A command-line project management tool that integrates Claude AI for autonomous software development workflow management.

## Features

- **Work Item Management** - Create and track User Stories and Bugs
- **AI-Powered Refinement** - Automatically break down work items into developer stories using Claude API
- **Smart Dependency Resolution** - Topological sorting with priority-based selection
- **Autonomous Implementation** - Execute developer stories using Claude Code CLI
- **Git Integration** - Automatic branch and worktree management per story
- **State Machine Validation** - Enforced state transitions for all entities

## Commands

```bash
# Create a new work item (User Story or Bug)
pm create

# Refine a work item into developer stories using Claude AI
pm refine <ID>

# Show the next available story for implementation
pm next

# Implement a developer story using Claude Code
pm implement <STORY_ID> <main-branch>

# List work items or developer stories
pm list [--stories] [-s|--status <STATUS>]
```

## Installation

### Prerequisites

- .NET 9.0 SDK or later
- Claude Code CLI (for implementation feature)
- Claude API key (for refinement feature)

### Build from Source

```bash
# Clone the repository
git clone <repository-url>
cd ProjectManagement

# Build the solution
dotnet build

# Run tests
dotnet test
```

## Configuration

Create an `appsettings.json` file in the CLI project directory:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=projectmanagement.db"
  },
  "Claude": {
    "ApiKey": "your-api-key-here",
    "Model": "claude-sonnet-4-20250514",
    "MaxTokens": 4096,
    "Timeout": "00:30:00"
  },
  "Git": {
    "DefaultBranch": "main",
    "WorktreeBasePath": "./worktrees"
  }
}
```

### API Key Storage (Recommended)

For local development, use dotnet user-secrets:

```bash
cd src/ProjectManagement.CLI
dotnet user-secrets init
dotnet user-secrets set "Claude:ApiKey" "your-api-key-here"
```

## Usage Examples

### Creating a Work Item

```bash
pm create
```

Interactive prompts will guide you through:
- Type selection (User Story or Bug)
- Title, Description, Acceptance Criteria (optional)
- Priority (1-9, where 1 is highest)

### Refining a Work Item

```bash
pm refine 1
```

This calls Claude API to break down the work item into:
- Implementation stories
- Unit tests
- Feature tests
- Documentation

### Selecting the Next Story

```bash
pm next
```

Shows the next ready story based on:
- Dependency resolution (all dependencies completed)
- Priority (lower number = higher priority)
- Business rule: only one UserStory can be active at a time

### Implementing a Story

```bash
pm implement 5 main
```

This will:
1. Create a Git branch if this is the first story for the work item
2. Create a Git worktree for isolated development
3. Execute Claude Code non-interactively
4. Clean up worktree on completion

### Listing Items

```bash
# List all work items
pm list

# List developer stories
pm list --stories

# Filter by status
pm list -s Completed
pm list --stories -s Pending
```

## Work Flow

```
1. pm create          → Create a User Story or Bug
2. pm refine <id>      → Claude AI breaks it into developer stories
3. pm next            → Shows the next story to implement
4. pm implement <id>   → Claude Code implements the story
5. Repeat 3-4 until all stories complete
```

## State Machine

### WorkItem States

```
Pending → Refining → Refined → InProgress → Completed
   ↓         ↓          ↓           ↓
   └───────→ Error ←───────────────┘
```

### DeveloperStory States

```
Pending → Ready → InProgress → Completed
   ↓         ↓          ↓
   └────────→ Blocked ←──────────┘
              ↓
           Error
```

## Database Schema

The system uses SQLite with the following tables:

- **WorkItems** - User Stories and Bugs
- **DeveloperStories** - Implementation, tests, docs
- **DeveloperStoryDependencies** - Story dependency graph
- **ExecutionLogs** - Audit trail for implementations

## Development

### Running Tests

```bash
# Run all tests
dotnet test

# Run with coverage collection
dotnet test --collect:"XPlat Code Coverage"

# Run specific project
dotnet test tests/ProjectManagement.Core.Tests
```

### Adding Database Migrations

```bash
cd src/ProjectManagement.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../ProjectManagement.CLI
```

### Project Structure

```
ProjectManagement.sln
├── src/
│   ├── ProjectManagement.Core/          # Domain models, interfaces, enums
│   ├── ProjectManagement.Infrastructure/  # EF Core, Git, Claude integration
│   ├── ProjectManagement.Application/     # Business logic services
│   └── ProjectManagement.CLI/             # Console commands
└── tests/                                 # Unit tests (133 tests total)
```

## Requirements

| Component | Version |
|-----------|--------|
| .NET SDK | 9.0+ |
| SQLite | Included with EF Core |
| Claude Code CLI | Latest |
| Anthropic API Key | Required for refinement |

## License

MIT License
