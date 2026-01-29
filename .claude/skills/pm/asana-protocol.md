# Asana Integration Protocol for PM

This document defines the Asana API integration patterns and commands for the Senior Project Manager.

## Prerequisites

### Setup

1. **Get your Asana Personal Access Token (PAT)**
   - Go to https://app.asana.com/-/developer_console
   - Create a new Personal Access Token
   - Copy and store securely

2. **Identify your Workspace and Project GIDs**
   ```bash
   # List workspaces
   curl -X GET https://app.asana.com/api/1.0/workspaces \
     -H "Authorization: Bearer $ASANA_PAT"

   # List projects in workspace
   curl -X GET https://app.asana.com/api/1.0/projects \
     -H "Authorization: Bearer $ASANA_PAT" \
     -G --data-urlencode "workspace=$WORKSPACE_GID"
   ```

3. **Set environment variables**
   ```bash
   export ASANA_PAT="your_pat_here"
   export ASANA_WORKSPACE_GID="123456789"
   export ASANA_PROJECT_GID="987654321"
   ```

## API Operations Reference

### Sections (Feature Grouping)

#### Create a New Section (Feature)

```bash
create_section() {
  local project_gid="$1"
  local section_name="$2"

  curl -X POST https://app.asana.com/api/1.0/sections \
    -H "Authorization: Bearer $ASANA_PAT" \
    -d "project=$project_gid" \
    -d "name=$section_name"
}

# Usage
create_section "$ASANA_PROJECT_GID" "Feature: User Authentication"
```

#### List Sections in Project

```bash
list_sections() {
  local project_gid="$1"

  curl -X GET "https://app.asana.com/api/1.0/projects/$project_gid/sections" \
    -H "Authorization: Bearer $ASANA_PAT"
}
```

### Tasks

#### Create a Task

```bash
create_task() {
  local name="$1"
  local notes="$2"
  local section_gid="$3"  # Optional: add to specific section

  local data="projects[0]=$ASANA_PROJECT_GID"
  data="$data&name=$(echo "$name" | jq -sRr @uri)"
  data="$data&notes=$(echo "$notes" | jq -sRr @uri)"

  if [ -n "$section_gid" ]; then
    data="$data&membership=$section_gid"
  fi

  curl -X POST https://app.asana.com/api/1.0/tasks \
    -H "Authorization: Bearer $ASANA_PAT" \
    -d "$data"
}

# Usage example
create_task \
  "[BE] Implement JWT authentication endpoint" \
  "Acceptance Criteria:
- Endpoint accepts username/password
- Returns JWT access + refresh tokens
- Tokens are stored securely
- Unit tests with 90%+ coverage

Branch: feature/user-auth
Related SA Task: #123456" \
  "$SECTION_GID"
```

#### Update Task Status (via Comment)

```bash
add_task_comment() {
  local task_gid="$1"
  local comment="$2"

  curl -X POST "https://app.asana.com/api/1.0/tasks/$task_gid/stories" \
    -H "Authorization: Bearer $ASANA_PAT" \
    -d "text=$(echo "$comment" | jq -sRr @uri)"
}

# Usage: Status update
add_task_comment "$TASK_GID" "Status: In Progress

Agent: [BE]
Commit: abc123def
Coverage: 92%

Started implementation of JWT endpoint.
All unit tests passing."

# Usage: Milestone comment
add_task_comment "$TASK_GID" "🎉 Milestone: Backend API Complete

All endpoints implemented and tested.
Ready for frontend integration."
```

#### Get Task Details

```bash
get_task() {
  local task_gid="$1"

  curl -X GET "https://app.asana.com/api/1.0/tasks/$task_gid?opt_fields=name,notes,completed,memberships" \
    -H "Authorization: Bearer $ASANA_PAT"
}
```

#### Mark Task Complete

```bash
complete_task() {
  local task_gid="$1"

  curl -X PUT "https://app.asana.com/api/1.0/tasks/$task_gid" \
    -H "Authorization: Bearer $ASANA_PAT" \
    -d "completed=true"
}
```

### Task Dependencies

#### Add Dependency

```bash
add_dependency() {
  local task_gid="$1"
  local depends_on_task_gid="$2"

  curl -X POST "https://app.asana.com/api/1.0/tasks/$task_gid/dependencies" \
    -H "Authorization: Bearer $ASANA_PAT" \
    -d "depend_on=$depends_on_task_gid"
}

# Usage: FE task depends on BE task
add_dependency "$FE_TASK_GID" "$BE_TASK_GID"
```

### Search and Query

#### Search Tasks

```bash
search_tasks() {
  local project_gid="$1"
  local search_term="$2"

  curl -X GET "https://app.asana.com/api/1.0/projects/$project_gid/tasks" \
    -H "Authorization: Bearer $ASANA_PAT" \
    -G --data-urlencode "search=$search_term"
}
```

## Standard Task Templates

### Development Task Template

```
Name: [Agent_ID] [Verb] [Component]

Notes:
REQUIREMENTS:
  1. [Specific requirement 1]
  2. [Specific requirement 2]

ACCEPTANCE CRITERIA:
  - [ ] [Criterion 1]
  - [ ] [Criterion 2]

DELIVERABLES:
  - [File or component 1]
  - [File or component 2]

CONSTRAINTS:
  - Branch: feature/[feature-name]
  - Related: [Link to requirements/design]
  - Tech: [Technology specifics]
  - Must NOT: [Things this agent should not do]

Coverage Target: [80%+]
```

### Code Review Task Template

```
Name: SA Review: [Feature/Subtask]

Notes:
PR: [Link to PR]
Author: [Agent who implemented]
Files Changed: [Number]

REVIEW CHECKLIST:
Architecture:
  - [ ] Follows established patterns
  - [ ] Proper separation of concerns
  - [ ] No security vulnerabilities
  - [ ] Performance considered

Code Quality:
  - [ ] Clean, readable code
  - [ ] Proper error handling
  - [ ] Meaningful variable names
  - [ ] Appropriate comments

Testing:
  - [ ] Unit tests comprehensive
  - [ ] Edge cases covered
  - [ ] Tests are meaningful
  - [ ] Coverage target met ([X]%)

Decision:
  - [ ] APPROVED
  - [ ] NEEDS REVISION (feedback below)
```

## Task Status Flow Management

### Status Transition Commands

```bash
# To Do → In Progress
transition_to_in_progress() {
  local task_gid="$1"
  local agent="$2"
  add_task_comment "$task_gid" "Status: In Progress

Agent: $agent
Started: $(date -Iseconds)
Branch: feature/[name]"
}

# In Progress → Ready for Review
transition_to_review() {
  local task_gid="$1"
  local agent="$2"
  local commit="$3"
  local coverage="$4"

  add_task_comment "$task_gid" "Status: Ready for Review

Agent: $agent
Completed: $(date -Iseconds)
Commit: $commit
Test Coverage: $coverage%

Ready for SA review."
}

# In Review → Ready for PO (all tasks complete)
transition_to_po() {
  local section_gid="$1"

  add_task_comment "$section_gid" "🎯 Feature Complete: Ready for PO Validation

All development tasks completed.
All code reviews approved.
Integration tests passing.
Coverage: 80%+

Ready for Product Owner acceptance testing."
}
```

## Agent-Specific Task Naming

| Agent | Task Prefix | Examples |
|-------|-------------|----------|
| SA | `[SA]` | `[SA] Design authentication architecture` |
| BE | `[BE]` | `[BE] Implement login endpoint` |
| FE | `[FE]` | `[FE] Create login form component` |
| DB | `[DB]` | `[DB] Add users table and indexes` |
| UX | `[UX]` | `[UX] Design login screen mockups` |
| QA | `[QA]` | `[QA] Create login e2e tests` |

## Comment Patterns

### Progress Update Comment

```
🔄 Progress Update

Agent: [Agent_ID]
Task: [Task name]

Completed:
  ✅ [What was done]

In Progress:
  🔄 [What's being worked on]

Next:
  ➡️ [Next step]

Commit: [hash]
Coverage: [X]%
```

### Blocker Comment

```
🚫 BLOCKER

Task: [Task name]
Blocker Type: [Technical | Business | Dependency]

Description:
[Detailed description of the blocker]

Impact:
- [What tasks are affected]
- [Estimated delay]

Proposed Resolution:
- [Suggested solution]

Needs:
- [Who needs to act]
```

### Milestone Comment

```
🎉 Milestone: [Milestone Name]

Feature: [Feature name]
Completed: [Date]

Summary:
- [Brief summary of what was accomplished]

Stats:
- Tasks: [X] completed
- Commits: [X]
- Coverage: [X]%

Next:
- [What happens next]
```

## Bulk Operations

### Create Multiple Tasks from SA Breakdown

```bash
create_feature_tasks() {
  local feature_name="$1"
  local section_gid="$2"

  # Backend tasks
  create_task "[BE] Implement user repository" \
    "Acceptance Criteria: ..." "$section_gid"

  create_task "[BE] Implement authentication service" \
    "Acceptance Criteria: ..." "$section_gid"

  # Frontend tasks
  create_task "[FE] Create login page component" \
    "Acceptance Criteria: ..." "$section_gid"

  create_task "[FE] Implement authentication context" \
    "Acceptance Criteria: ..." "$section_gid"

  # Database tasks
  create_task "[DB] Create users table migration" \
    "Acceptance Criteria: ..." "$section_gid"
}
```

## Error Handling

### Common API Errors

```bash
# Check for errors in API response
check_asana_error() {
  local response="$1"

  if echo "$response" | jq -e '.errors' > /dev/null; then
    echo "Asana API Error:"
    echo "$response" | jq -r '.errors[] | "\(.message): \(.help)"'
    return 1
  fi
  return 0
}

# Usage with error handling
response=$(create_task "Task name" "Notes" "$SECTION_GID")
if check_asana_error "$response"; then
  task_gid=$(echo "$response" | jq -r '.data.gid')
  echo "Task created: $task_gid"
fi
```

## Quick Reference Commands

```bash
# Quick task create
alias asana-create='create_task'
alias asana-comment='add_task_comment'
alias asana-complete='complete_task'

# Quick status update
asana-status() {
  add_task_comment "$1" "Status: $2

Agent: $3
Commit: ${4:-N/A}
Coverage: ${5:-N/A}%"
}
```

## Best Practices

1. **Always include context in task notes** - Don't make agents hunt for information
2. **Update status promptly** - Keep the project visible to PO
3. **Use meaningful task names** - Start with verb, include component name
4. **Document dependencies** - Link related tasks
5. **Celebrate milestones** - Add 🎉 comments for completed features
6. **Document blockers** - Don't hide problems, flag them immediately
7. **Include commit hashes** - Trace every task to specific commits
8. **Track coverage** - Always include test coverage percentage
