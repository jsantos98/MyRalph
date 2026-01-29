# PO Team - Multi-Agent Development System

A coordinated team of 7 specialized agents for software product development, orchestrated by Claude Code.

## Overview

The PO Team system transforms product requirements into delivered software through specialized agents working together:

```
Product Owner (You)
    │
    ▼
Senior Project Manager (pm)
    │
    ├──► Senior Software Architect (sa)
    ├──► Senior UX Designer (ux)
    ├──► Senior Backend Developer (be-dev)
    ├──► Senior Frontend Developer (fe-dev)
    ├──► Senior Database Developer (db-dev)
    └──► Senior QA Tester (qa-tester)
```

## Agent Skills

| Agent | Skill Name | Role | Command |
|-------|------------|------|---------|
| **PM** | `pm` | Orchestrates team, manages Asana, coordinates features | `/pm` |
| **SA** | `sa` | Architecture design, code reviews, task breakdown | `/sa` |
| **BE** | `be-dev` | .NET/C# backend implementation | `/be-dev` |
| **FE** | `fe-dev` | React/TypeScript frontend implementation | `/fe-dev` |
| **DB** | `db-dev` | PostgreSQL schema, migrations, optimization | `/db-dev` |
| **UX** | `ux-designer` | UI/UX mockups and design systems | `/ux-designer` |
| **QA** | `qa-tester` | Integration and e2e testing | `/qa-tester` |

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Backend** | .NET 8, C# 12, ASP.NET Core, Entity Framework Core |
| **Frontend** | React 18, TypeScript 5, Tailwind CSS, React Query |
| **Database** | PostgreSQL 16, EF Core Migrations |
| **Testing** | xUnit, React Testing Library, Playwright |
| **Infrastructure** | Asana (project management), Git (version control) |

## Quick Start

### 1. Feature Workflow

As a Product Owner, here's how to implement a feature:

```
1. YOU → PM: "I want feature X"
2. PM clarifies requirements with you
3. PM coordinates UX for mockups (if UI changes needed)
4. PM gets UX designs approved by you
5. PM consults SA for architecture and task breakdown
6. PM orchestrates development agents (BE/FE/DB) to implement
7. PM coordinates QA for integration tests
8. PM gets PR validated by SA
9. YOU test in validation environment
10. If approved: feature merges
11. If rejected: PM incorporates feedback and iterates
```

### 2. Using the Skills

```bash
# Start a feature (PM skill)
/pm "I want to implement user authentication with JWT tokens"

# Request architecture review (SA skill)
/sa "Review the authentication architecture for security issues"

# Implement backend (BE skill)
/be-dev "Implement the JWT token service with access and refresh tokens"

# Implement frontend (FE skill)
/fe-dev "Create a login form component with validation"

# Design a UI (UX skill)
/ux-designer "Create mockups for the user profile page"

# Write tests (QA skill)
/qa-tester "Create e2e tests for the login flow"
```

### 3. Asana Integration Setup

The PM agent manages Asana for project tracking:

```bash
# Set up Asana credentials
export ASANA_PAT="your_personal_access_token"
export ASANA_WORKSPACE_GID="your_workspace_id"
export ASANA_PROJECT_GID="your_project_id"
```

The PM will:
- Create sections for each feature
- Create tasks for each development item
- Update task status throughout development
- Add comments for milestones

### 4. Git Workflow

```
main
└── feature/feature-name
    ├── [BE] commits
    ├── [FE] commits
    ├── [DB] commits
    └── [QA] commits
```

Commit format: `[AGENT-ID] type(scope): description`

## Feature Implementation Flow

### Phase 1: Requirements & Design

```
┌─────────────────────────────────────────────────────────────┐
│ Product Owner presents feature request                      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ PM asks clarifying questions                                 │
│ - What problem does this solve?                              │
│ - Who are the users?                                         │
│ - What are the acceptance criteria?                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼ (if UI changes needed)
┌─────────────────────────────────────────────────────────────┐
│ UX creates mockups/designs                                   │
│ - Designs responsive layouts                                 │
│ - Considers accessibility                                    │
│ - Uses design system                                         │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ PO approves designs                                          │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ SA creates architecture and task breakdown                  │
│ - Designs technical approach                                 │
│ - Creates feature branch                                     │
│ - Breaks down into scoped tasks (BE/FE/DB)                   │
│ - Defines dependencies                                       │
└─────────────────────────────────────────────────────────────┘
```

### Phase 2: Development

```
┌─────────────────────────────────────────────────────────────┐
│ PM creates Asana structure and delegates tasks               │
│ - Creates feature section                                    │
│ - Creates tasks for each work item                          │
│ - Delegates to appropriate agents in priority order         │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼ (for each task)
┌─────────────────────────────────────────────────────────────┐
│ Development Agent implements task                           │
│ - Uses clean memory context                                 │
│ - Writes meaningful tests                                    │
│ - Commits after completion                                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ SA performs code review                                     │
│ - Checks architecture compliance                            │
│ - Validates security                                         │
│ - Ensures test coverage                                     │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ PM updates Asana and continues to next task                │
└─────────────────────────────────────────────────────────────┘
```

### Phase 3: Integration & Validation

```
┌─────────────────────────────────────────────────────────────┐
│ All development tasks complete → QA creates tests           │
│ - Integration tests for full feature                        │
│ - E2E tests for user flows                                  │
│ - Validates all scenarios                                   │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ SA validates integration tests                             │
│ - Ensures tests are meaningful                              │
│ - Validates 80%+ coverage                                   │
│ - Checks test quality                                       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ SA creates Pull Request for PO validation                  │
│ - Ensures all quality gates passed                          │
│ - Prepares validation environment                           │
└────────────────────┬────────────────────────────────────────┘
                     │
                     ▼
┌─────────────────────────────────────────────────────────────┐
│ PO tests and accepts/rejects                               │
│                                                              │
│ ACCEPTED:                                                   │
│ - SA merges PR                                              │
│ - Branch deleted                                            │
│ - Asana tasks marked complete                               │
│                                                              │
│ REJECTED:                                                   │
│ - PM gathers feedback                                       │
│ - Creates new tasks for fixes                               │
│ - Restarts development for feedback items                   │
└─────────────────────────────────────────────────────────────┘
```

## Quality Gates

| Gate | Validator | Criteria |
|------|-----------|----------|
| Requirements clear | PM | Unambiguous, testable |
| Designs approved | PO | PO explicitly approved |
| Architecture reviewed | SA | SA approved approach |
| Code reviewed | SA | SA approved each commit |
| Unit tests passing | Agent | All tests green |
| Integration tests passing | QA | All e2e tests green |
| Coverage ≥ 80% | SA | Coverage report verified |
| PO acceptance | PO | PO tested and approved |

## File Structure

```
.claude/skills/po-team/
├── README.md                  # This file
├── ARCHITECTURE.md            # System architecture
├── SHARED.md                  # Shared protocols and knowledge
│
├── pm-skill/                  # Project Manager
│   ├── SKILL.md
│   └── asana-protocol.md
│
├── sa-skill/                  # Software Architect
│   ├── SKILL.md
│   └── architecture-patterns.md
│
├── be-dev-skill/              # Backend Developer
│   ├── SKILL.md
│   └── dotnet-patterns.md
│
├── fe-dev-skill/              # Frontend Developer
│   ├── SKILL.md
│   └── react-patterns.md
│
├── db-dev-skill/              # Database Developer
│   └── SKILL.md
│
├── ux-designer-skill/         # UX Designer
│   └── SKILL.md
│
└── qa-tester-skill/           # QA Tester
    └── SKILL.md
```

## Agent Responsibilities

### Senior Project Manager (pm)
- Clarifies requirements with PO
- Breaks down features with SA
- Coordinates all development agents
- Manages Asana integration
- Monitors progress and blockers
- Coordinates PO acceptance

### Senior Software Architect (sa)
- Designs technical architecture
- Creates feature branches
- Breaks down tasks by skill (BE/FE/DB)
- Performs thorough code reviews
- Enforces 80%+ test coverage
- Validates integration tests
- Creates/validates Pull Requests

### Senior Backend Developer (be-dev)
- Uses clean memory context per task
- Implements .NET/C# backend
- Applies security and best practices
- Writes meaningful unit tests (90%+ target)
- Never modifies frontend code

### Senior Frontend Developer (fe-dev)
- Uses clean memory context per task
- Implements React/TypeScript UI
- Bridges UX designs with backend APIs
- Tests responsiveness and accessibility
- Never modifies backend code

### Senior Database Developer (db-dev)
- Uses clean memory context per task
- Optimizes PostgreSQL queries
- Creates migrations and schemas
- Implements procedures and indexes
- Bridges database with backend

### Senior UX Designer (ux)
- Uses clean memory context per task
- Creates responsive mockups
- Applies accessibility standards
- Uses design system
- Gets PO approval before handoff

### Senior QA Tester (qa)
- Uses clean memory context per task
- Creates integration tests
- Implements e2e tests with Playwright
- Validates full user flows
- Ensures test quality

## Environment Setup

### Required Environment Variables

```bash
# Asana Integration
export ASANA_PAT="your_personal_access_token"
export ASANA_WORKSPACE_GID="your_workspace_id"
export ASANA_PROJECT_GID="your_project_id"

# Database
export DATABASE_URL="postgresql://user:pass@localhost:5432/dbname"

# API (for frontend)
export VITE_API_URL="http://localhost:5000"

# JWT Secrets
export JWT_SECRET="your_jwt_secret_here"
export JWT_ISSUER="https://api.example.com"
export JWT_AUDIENCE="https://example.com"
```

## Best Practices

### For Product Owners

1. **Be Specific**: Clear requirements lead to better results
2. **Provide Context**: Share business value and user needs
3. **Review Designs**: Approve UX designs before development starts
4. **Test Thoroughly**: Validate features in the staging environment
5. **Provide Feedback**: Be specific about what needs to change

### For Using the System

1. **Start with PM**: Always begin feature requests with the PM skill
2. **Trust the Process**: Let PM coordinate the team
3. **Ask Questions**: PM will clarify ambiguous requirements
4. **Review Progress**: Check Asana for task status
5. **Test Before Accepting**: Always validate features before approval

## Sources

This system was designed with reference to:
- [How to Build Multi-Agent Systems: Complete 2026 Guide](https://dev.to/eira-wexford/how-to-build-multi-agent-systems-complete-2026-guide-1io6)
- [AI Agents 2026: 6 Design Patterns for Multi-Agent Systems](https://www.linkedin.com/posts/jeganselvarajlinkedin_the-biggest-ai-agent-mistake-in-2025-building-activity-7406656906196418560-tOXu)
- [Top AI Agentic Workflow Patterns That Will Lead in 2026](https://medium.com/lets-code-future/top-ai-agentic-workflow-patterns-that-will-lead-in-2026-0e4755fdc6f6)
- [Multi-Agent In-IDE Coordination: Google's Eight Fundamental Patterns for 2026](https://apticode.in/blogs/multi-agent-in-ide-coordination-google-s-eight-fundamental-patterns-for-2026)
- [Foundational Asana best practices for 2026](https://forum.asana.com/t/foundational-asana-best-practices-for-an-inspired-start-to-2026/1109998)
- [Best practices for developing custom Asana apps & integrations](https://forum.asana.com/t/best-practices-for-developing-custom-asana-app-integrations/1048626)

## License

This system is part of the PO Team Agent System.

## Version

1.0.0 - Initial release with 7 specialized agents
