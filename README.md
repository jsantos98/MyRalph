# Project Management CLI

A .NET CLI-based project management system that integrates Claude AI for autonomous development workflow management.

## Purpose

This tool helps manage software development projects by:
- Creating and tracking User Stories and Bugs
- Breaking down work items into developer stories using Claude AI
- Managing dependencies between developer stories
- Orchestrating implementation workflow with Git integration

## Requirements

- .NET 9.0 SDK or later
- SQLite (included with EF Core)
- Claude API key for AI-powered refinement
- Git (for branch/worktree management)
- Claude Code CLI (optional, for autonomous implementation)

## Installation

1. Clone the repository
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the solution:
   ```bash
   dotnet build
   ```

## Configuration

Create an `appsettings.json` file or use `dotnet user-secrets`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=projectmanagement.db"
  },
  "Claude": {
    "ApiKey": "your-anthropic-api-key",
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

Set your Claude API key using user-secrets:
```bash
dotnet user-secrets init
dotnet user-secrets set "Claude:ApiKey" "your-api-key-here"
```

## Commands

### Create Work Item
```bash
pm create
```
Interactive prompts to create a new User Story or Bug with:
- Type (UserStory/Bug)
- Title
- Description
- Acceptance Criteria (optional)
- Priority (1-9, default 5)

### Refine Work Item
```bash
pm refine <ID>
```
Uses Claude AI to break down a User Story into developer stories:
- Implementation stories
- Unit test stories
- Feature test stories
- Documentation stories

Also sets up dependencies between stories based on Claude's analysis.

### Select Next Story
```bash
pm next
```
Displays the next available developer story based on:
- Priority (highest first)
- Dependency resolution (only stories with completed dependencies)
- Business rules (only one UserStory can be InProgress at a time)

### Implement Story
```bash
pm implement <STORY_ID> <MAIN_BRANCH>
```
Implements a developer story:
1. Creates Git branch for the parent WorkItem (if needed)
2. Creates Git worktree for the story
3. Marks story as InProgress
4. Executes Claude Code in non-interactive mode
5. On success: marks story as Completed, removes worktree
6. On failure: marks story as Error with error message

### List Work Items
```bash
pm list [status]
```
Lists all work items, optionally filtered by status:
- `pm list` - all items
- `pm list pending` - only pending items
- `pm list refining` - only items being refined
- `pm list refined` - only refined items
- `pm list inprogress` - only items in progress
- `pm list completed` - only completed items

## Database Schema

### WorkItem
- `Id`: Unique identifier (auto-increment)
- `Type`: UserStory (0) or Bug (1)
- `Title`, `Description`, `AcceptanceCriteria`
- `Priority`: 1-9 (1=highest, default 5)
- `Status`: Pending, Refining, Refined, InProgress, Completed, Error
- `CreatedAt`, `UpdatedAt`: Timestamps
- `ErrorMessage`: Error details if status is Error

### DeveloperStory
- `Id`: Unique identifier (separate sequence from WorkItem)
- `WorkItemId`: Reference to parent WorkItem
- `StoryType`: Implementation, UnitTests, FeatureTests, Documentation
- `Title`, `Description`, `Instructions`
- `Priority`: Inherited from parent WorkItem
- `Status`: Pending, Ready, InProgress, Completed, Error, Blocked
- `GitBranch`, `GitWorktree`: Git tracking
- `StartedAt`, `CompletedAt`: Execution timestamps
- `ErrorMessage`, `Metadata`: Additional info

### DeveloperStoryDependency
- `DependentStoryId`: Story that has the dependency
- `RequiredStoryId`: Story that must complete first
- `Description`: Why this dependency exists

## State Transitions

### WorkItem
```
Pending -> Refining -> Refined -> InProgress -> Completed
    |          |          |           |
    +----------+----------+-----------+-> Error
```

### DeveloperStory
```
Pending -> Ready -> InProgress -> Completed
    |         |          |
    +---------+----------+----> Blocked
                           |
                           v
                         Error
```

## Business Rules

1. **Single Active User Story**: Only one UserStory can be `InProgress` at a time
2. **Dependency Resolution**: A story can only be `Ready` if all its dependencies are `Completed`
3. **Priority Ordering**: Stories are selected by Priority (1=highest), then by ID (FIFO)
4. **Bug InProgress**: Bugs cannot be `InProgress` (they're worked on differently)

## Testing

Run all tests:
```bash
dotnet test
```

Run tests with coverage:
```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
```

## Building

Build the CLI executable:
```bash
dotnet publish src/ProjectManagement.CLI -c Release -o ./publish
```

Run the published CLI:
```bash
./publish/ProjectManagement.CLI.exe
```

## Project Structure

```
ProjectManagement.sln
├── src/
│   ├── ProjectManagement.Core/         # Domain models, interfaces, enums
│   ├── ProjectManagement.Infrastructure/ # EF Core, Git, Claude integration
│   ├── ProjectManagement.Application/    # Business logic and services
│   └── ProjectManagement.CLI/           # Spectre.Console commands
└── tests/
    ├── ProjectManagement.Core.Tests/
    ├── ProjectManagement.Infrastructure.Tests/
    ├── ProjectManagement.Application.Tests/
    └── ProjectManagement.CLI.Tests/
```

## License

MIT License
