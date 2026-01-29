# PO Team Multi-Agent System Architecture

## System Overview

A coordinated team of 7 specialized agents for product software development, orchestrated by a Senior Project Manager (PM) working with the Product Owner (PO).

```
                        ┌─────────────┐
                        │  Product    │
                        │  Owner (PO) │
                        └──────┬──────┘
                               │ defines features
                               ▼
                        ┌─────────────┐
       ┌─────────────────┤ Senior PM   ├─────────────────┐
       │                 │(Orchestrator)│                 │
       │                 └───────┬───────┘                 │
       │                         │                         │
       │      ┌──────────────────┼──────────────────┐      │
       │      │      │           │           │      │      │
       │      ▼      ▼           ▼           ▼      │      │
       │  ┌────┐ ┌────┐     ┌────┐     ┌────┐   ┌────┐ ┌───┐
       └──►│ SA │ │ UX │     │ BE │     │ FE │   │ DB │ │QA│
          └────┘ └────┘     └────┘     └────┘   └────┘ └───┘
           │                                   │
           └─────────── code reviews ───────────┘
```

## Agent Architecture: Claude Skills (.md)

All agents are implemented as Claude Code skills using markdown-based prompt definitions.

### Skill Directory Structure

```
.claude/skills/po-team/
├── ARCHITECTURE.md           # This file
├── SHARED.md                 # Shared knowledge and protocols
├── pm-skill/
│   ├── SKILL.md             # Main skill definition
│   ├── context.md           # State management
│   └── asana-protocol.md    # Asana integration patterns
├── sa-skill/
│   ├── SKILL.md
│   ├── review-checklist.md  # Code review standards
│   └── architecture-patterns.md
├── be-dev-skill/
│   ├── SKILL.md
│   └── dotnet-patterns.md   # .NET/C# specific patterns
├── fe-dev-skill/
│   ├── SKILL.md
│   └── react-patterns.md    # React/Angular patterns
├── db-dev-skill/
│   ├── SKILL.md
│   └── database-patterns.md
├── ux-designer-skill/
│   ├── SKILL.md
│   └── design-standards.md
└── qa-tester-skill/
    ├── SKILL.md
    └── testing-patterns.md
```

## Communication Protocol

### 1. Direct Orchestration (PM-Managed)

The PM directly orchestrates all agents in sequence:

```
PM receives feature request from PO
    │
    ├─► Clarify requirements with PO
    │
    ├─► Consult UX for mockups (if UI changes needed)
    │       │
    │       └─► UX creates mockups → PO approves
    │
    ├─► Consult SA for architecture and task breakdown
    │       │
    │       ├─► SA creates feature branch
    │       ├─► SA splits into scoped sub-tasks
    │       └─► SA returns task list to PM
    │
    ├─► For each development task (in priority order):
    │   │
    │   ├─► PM delegates to appropriate dev agent (BE/FE/DB)
    │   │       │
    │   │       ├─► Agent implements with clean context
    │   │       ├─► Agent writes tests
    │   │       ├─► SA performs code review
    │   │       ├─► Agent fixes issues (if any)
    │   │       └─► Agent commits code
    │   │
    │   └─► PM updates Asana task status
    │
    ├─► Consult QA for integration tests
    │       │
    │       ├─► QA creates integration tests
    │       ├─► SA validates integration tests
    │       └─► QA commits test code
    │
    ├─► Consult SA for final PR validation
    │       │
    │       ├─► SA reviews full feature
    │       ├─► SA validates 80%+ test coverage
    │       └─► SA prepares/stages PR for PO
    │
    └─► Coordinate PO acceptance/rejection
            │
            ├─► If accepted → SA merges, closes Asana tasks
            └─► If rejected → PM gathers feedback, restarts loop
```

### 2. Agent Handoff Protocol

When PM delegates to another agent:

```
PM → [Agent]:
  - Task context and requirements
  - Feature branch name
  - Related Asana task IDs
  - Dependencies and constraints
  - Expected deliverables
  - Acceptance criteria

[Agent] → PM:
  - Implementation status (in_progress/completed/blocked)
  - Test coverage achieved
  - Any issues or blockers
  - Commit hash for verification
```

### 3. State Management

State is managed through:

1. **Asana as Source of Truth**: All task states tracked in Asana
2. **Git Branches**: Feature isolation and workflow tracking
3. **PM Context**: PM maintains the overall project state

## Asana Integration

### Asana Structure

```
Project: [Product Name]
├── Section: Feature 1
│   ├── Task: 1.1 Backend implementation
│   ├── Task: 1.2 Frontend implementation
│   ├── Task: 1.3 Integration tests
│   └── Task: 1.4 Code review & validation
├── Section: Feature 2
│   └── ...
└── Section: Backlog
    └── ...
```

### Task Status Flow

```
To Do → In Progress → Ready for Review → In Review → Ready for PO → Accepted/Rejected
```

### Asana API Operations

| Operation | When | Agent |
|-----------|------|-------|
| Create section for new feature | Feature approved | PM |
| Create tasks under section | After SA breakdown | PM |
| Update task status | On every state change | PM |
| Add comment on milestone | Significant completion | PM |
| Move completed tasks | After PO acceptance | PM |

## Git Workflow

### Branch Strategy

```
main (production)
├── feature/feature-name (created by SA)
    ├── be-subtask-1 (BE commits)
    ├── fe-subtask-1 (FE commits)
    └── db-subtask-1 (DB commits)
```

### Commit Conventions

```
[agent-id] type(scope): description

Examples:
[BE] feat(api): add user authentication endpoint
[FE] fix(login): resolve session timeout issue
[DB] perf(queries): add index on users.email
[QA] test(integration): add e2e checkout flow test
```

### PR Workflow

1. SA creates feature branch
2. Dev agents commit their work
3. SA ensures all commits are reviewed
4. SA validates 80%+ test coverage
5. SA creates PR to main
6. PM coordinates PO validation
7. On acceptance, SA merges and deletes branch

## Test Coverage Enforcement

### Minimum 80% Coverage

- Enforced by SA before PR creation
- Validated using coverage reports
- Must include unit, integration, and e2e tests

### Test Responsibilities

| Agent | Test Type | Coverage Goal |
|-------|-----------|---------------|
| BE | Unit tests for services, controllers, repositories | 90%+ |
| FE | Unit tests for components, services, hooks | 85%+ |
| DB | Unit tests for migrations, procedures | 85%+ |
| QA | Integration & e2e tests for features | Full coverage |

## Tech Stack Specifics

### Backend (.NET/C#)
- ASP.NET Core Web API
- Entity Framework Core
- xUnit for testing
- dependency injection patterns
- repository pattern

### Frontend (React)
- React with TypeScript
- Axios for API calls
- Jest + React Testing Library
- component composition
- custom hooks

### Database
- PostgreSQL (primary)
- EF Core Migrations
- Dapper for performance-critical queries

### Testing
- Playwright for e2e
- xUnit for backend
- Jest for frontend

## Agent Metadata

Each skill must include:

```yaml
# SKILL.md frontmatter
name: [agent-name]
description: [1-line description]
version: 1.0.0
author: PO Team System
agentType: orchestrator | technical | creative
coordinatesWith: [list of related agents]
cleanContext: true | false
techStack: [relevant technologies]
```

## Quality Gates

### Before PR to PO
- [ ] All development tasks completed
- [ ] 80%+ test coverage achieved
- [ ] All unit tests passing
- [ ] All integration tests passing
- [ ] SA code review approved
- [ ] No security vulnerabilities
- [ ] Documentation updated (if needed)

### After PO Acceptance
- [ ] PR merged to main
- [ ] Feature branch deleted
- [ ] Asana tasks marked complete
- [ ] Release notes updated (if needed)
