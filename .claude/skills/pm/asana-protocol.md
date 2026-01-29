# Asana Integration Protocol for PM

This document defines the Asana MCP server integration patterns and commands for the Senior Project Manager.

## Prerequisites

### Setup

1. **Asana MCP Server must be configured**
   - The Asana MCP server should be installed and configured in Claude Code
   - Environment variables (ASANA_PAT, etc.) should be set in MCP config

2. **First-Time Pattern - Always Start Here**
   ```
   Step 1: List available workspaces
   Tool: asana_list_workspaces

   Step 2: Get projects in workspace
   Tool: asana_get_projects(workspace="<workspace_gid>", team="<team_gid>")

   Step 3: Get project sections
   Tool: asana_get_project_sections(project_id="<project_gid>")
   ```

## MCP Operations Reference

### Workspace & Project Discovery

#### Get All Workspaces

```
Tool: asana_list_workspaces()
Returns: [{gid, name, organization}, ...]
```

#### Get Projects in Workspace

```
Tool: asana_get_projects(
  workspace: "<workspace_gid>",
  team: "<team_gid>",        // Optional: filter by team
  archived: false            // Optional: exclude archived
)
Returns: [{gid, name, owner, ...}, ...]
```

#### Get Project Details

```
Tool: asana_get_project(
  project_id: "<project_gid>",
  opt_fields: "name,notes,owner,members,custom_fields"
)
Returns: {gid, name, notes, owner, members, custom_fields, ...}
```

### Sections (Feature Grouping)

#### Get Sections in Project

```
Tool: asana_get_project_sections(
  project_id: "<project_gid>",
  opt_fields: "name,projects"
)
Returns: [{gid, name}, ...]
```

**Note:** Asana MCP does not have a create_section tool. Use the project's existing sections
or create sections manually in Asana UI before adding tasks.

### Tasks

#### Create a Task

```
Tool: asana_create_task(
  name: "[BE] Implement JWT authentication endpoint",
  project_id: "<project_gid>",
  section_id: "<section_gid>",      // Optional: add to specific section
  html_notes: "<h3>Acceptance Criteria:</h3>
              <ul>
                <li>Endpoint accepts username/password</li>
                <li>Returns JWT access + refresh tokens</li>
                <li>Tokens are stored securely</li>
                <li>Unit tests with 90%+ coverage</li>
              </ul>
              <p><strong>Branch:</strong> feature/user-auth</p>",
  assignee: "me",                    // or email or user GID
  due_on: "2025-02-01",              // Optional
  start_on: "2025-01-25",            // Optional
  followers: "user@example.com",     // Optional
  completed: false,                  // Optional
  resource_subtype: "default_task"   // or "milestone", "approval"
)
Returns: {gid, name, created_at, ...}
```

#### Get Task Details

```
Tool: asana_get_task(
  task_id: "<task_gid>",
  opt_fields: "name,notes,completed,assignee,due_on,custom_fields,memberships"
)
Returns: {gid, name, notes, completed, assignee, ...}
```

#### Update Task Properties

```
Tool: asana_update_task(
  task_id: "<task_gid>",
  name: "Updated task name",          // Optional
  notes: "Updated description",       // Optional
  assignee: "user@example.com",       // Optional
  completed: false,                   // Optional
  due_on: "2025-02-15",               // Optional
  custom_fields: '{"field_gid": "value"}'  // Optional JSON string
)
Returns: {gid, name, ...}
```

#### Add Comment to Task (Status Update)

```
Tool: asana_create_task_story(
  task_id: "<task_gid>",
  text: "Status: In Progress

Agent: [BE]
Commit: abc123def
Coverage: 92%

Started implementation of JWT endpoint.
All unit tests passing."
)
Returns: {gid, created_at, ...}
```

#### Mark Task Complete

```
Tool: asana_update_task(
  task_id: "<task_gid>",
  completed: true
)
```

#### Delete Task

```
Tool: asana_delete_task(
  task_id: "<task_gid>"
)
```

### Task Dependencies

#### Set Task Dependencies (Prerequisites)

```
Tool: asana_set_task_dependencies(
  task_id: "<task_gid>",
  dependencies: ["<depends_on_task_gid_1>", "<depends_on_task_gid_2>"]
)
```

**Example:** FE task depends on BE task completion
```
asana_set_task_dependencies(
  task_id: "<fe_task_gid>",
  dependencies: ["<be_task_gid>"]
)
```

#### Set Task Dependents (Blocking)

```
Tool: asana_set_task_dependents(
  task_id: "<task_gid>",
  dependents: ["<blocked_task_gid_1>", "<blocked_task_gid_2>"]
)
```

**Example:** BE task blocks FE and QA tasks
```
asana_set_task_dependents(
  task_id: "<be_task_gid>",
  dependents: ["<fe_task_gid>", "<qa_task_gid>"]
)
```

### Search and Query

#### Search Tasks

```
Tool: asana_search_tasks(
  workspace: "<workspace_gid>",
  text: "authentication",              // Optional: search term
  projects_any: "<project_gid>",       // Optional: filter by project
  assignee_any: "me",                  // Optional: my tasks
  completed: false,                    // Optional: incomplete only
  due_on_before: "2025-02-01",         // Optional: due date filter
  resource_subtype: "default_task",    // Optional: task type
  limit: 50                            // Optional: result count
)
Returns: [{gid, name, ...}, ...]
```

#### Get Tasks for Project

```
Tool: asana_get_tasks(
  project: "<project_gid>",
  section: "<section_gid>",            // Optional: filter by section
  completed_since: "2025-01-01T00:00:00Z",  // Optional
  opt_fields: "name,assignee,completed"
)
Returns: [{gid, name, assignee, completed}, ...]
```

### Users and Teams

#### Get Current User Info

```
Tool: asana_get_user(
  user_id: "me",                       // or omit (defaults to "me")
  opt_fields: "name,email,workspaces"
)
Returns: {gid, name, email, workspaces, ...}
```

#### Get User by Email

```
Tool: asana_get_user(
  user_id: "user@example.com",
  opt_fields: "name,email"
)
```

#### Get Teams in Workspace

```
Tool: asana_get_teams_for_workspace(
  workspace_gid: "<workspace_gid>",
  opt_fields: "name,description"
)
Returns: [{gid, name}, ...]
```

#### Get Team Members

```
Tool: asana_get_team_users(
  team_id: "<team_gid>",
  opt_fields: "name,email"
)
Returns: [{gid, name, email}, ...]
```

### Task Followers

#### Add Followers to Task

```
Tool: asana_add_task_followers(
  task_id: "<task_gid>",
  followers: "user1@example.com,user2@example.com"  // comma-separated
)
```

#### Remove Followers from Task

```
Tool: asana_remove_task_followers(
  task_id: "<task_gid>",
  followers: "user1@example.com"  // comma-separated
)
```

### Project Status

#### Create Project Status Update

```
Tool: asana_create_project_status(
  project_gid: "<project_gid>",
  color: "green",                   // "green", "yellow", "red", "blue"
  title: "Feature Complete - Ready for PO",
  html_text: "<p>All development tasks completed.</p>
              <p>Ready for Product Owner validation.</p>"
)
```

#### Get Project Statuses

```
Tool: asana_get_project_statuses(
  project_gid: "<project_gid>",
  limit: 10
)
Returns: [{gid, color, title, text, created_at}, ...]
```

### Attachments

#### Get Attachments for Task

```
Tool: asana_get_attachments_for_object(
  parent: "<task_gid>",
  limit: 50
)
Returns: [{gid, name, download_url, view_url}, ...]
```

#### Get Attachment Details

```
Tool: asana_get_attachment(
  attachment_gid: "<attachment_gid>",
  opt_fields: "name,download_url,view_url"
)
Returns: {gid, name, download_url, view_url, ...}
```

## Standard Task Templates

### Development Task Template

```
Name: [Agent_ID] [Verb] [Component]

html_notes:
<h3>REQUIREMENTS:</h3>
<ol>
  <li>Specific requirement 1</li>
  <li>Specific requirement 2</li>
</ol>

<h3>ACCEPTANCE CRITERIA:</h3>
<ul>
  <li><input type="checkbox"> Criterion 1</li>
  <li><input type="checkbox"> Criterion 2</li>
</ul>

<h3>DELIVERABLES:</h3>
<ul>
  <li>File or component 1</li>
  <li>File or component 2</li>
</ul>

<h3>CONSTRAINTS:</h3>
<ul>
  <li>Branch: feature/[feature-name]</li>
  <li>Related: [Link to requirements/design]</li>
  <li>Tech: [Technology specifics]</li>
  <li>Must NOT: [Things this agent should not do]</li>
</ul>

<p><strong>Coverage Target:</strong> 80%+</p>
```

### Code Review Task Template

```
Name: SA Review: [Feature/Subtask]

html_notes:
<p><strong>PR:</strong> [Link to PR]</p>
<p><strong>Author:</strong> [Agent who implemented]</p>
<p><strong>Files Changed:</strong> [Number]</p>

<h3>REVIEW CHECKLIST:</h3>

<h4>Architecture:</h4>
<ul>
  <li><input type="checkbox"> Follows established patterns</li>
  <li><input type="checkbox"> Proper separation of concerns</li>
  <li><input type="checkbox"> No security vulnerabilities</li>
  <li><input type="checkbox"> Performance considered</li>
</ul>

<h4>Code Quality:</h4>
<ul>
  <li><input type="checkbox"> Clean, readable code</li>
  <li><input type="checkbox"> Proper error handling</li>
  <li><input type="checkbox"> Meaningful variable names</li>
  <li><input type="checkbox"> Appropriate comments</li>
</ul>

<h4>Testing:</h4>
<ul>
  <li><input type="checkbox"> Unit tests comprehensive</li>
  <li><input type="checkbox"> Edge cases covered</li>
  <li><input type="checkbox"> Tests are meaningful</li>
  <li><input type="checkbox"> Coverage target met ([X]%)</li>
</ul>

<h4>Decision:</h4>
<ul>
  <li><input type="checkbox"> APPROVED</li>
  <li><input type="checkbox"> NEEDS REVISION (feedback below)</li>
</ul>
```

## Task Status Flow Management

### Status Transition Patterns

#### To Do → In Progress

```
asana_create_task_story(
  task_id: "<task_gid>",
  text: "Status: In Progress

Agent: [BE]
Started: 2025-01-25T10:00:00Z
Branch: feature/user-auth"
)
```

#### In Progress → Ready for Review

```
asana_create_task_story(
  task_id: "<task_gid>",
  text: "Status: Ready for Review

Agent: [BE]
Completed: 2025-01-26T15:30:00Z
Commit: abc123def456
Test Coverage: 92%

Ready for SA review."
)
```

#### Feature Complete → Ready for PO

```
asana_create_project_status(
  project_gid: "<project_gid>",
  color: "green",
  title: "Feature Complete: Ready for PO Validation",
  html_text: "<p><strong>Feature:</strong> User Authentication</p>
              <p>All development tasks completed.</p>
              <p>All code reviews approved.</p>
              <p>Integration tests passing.</p>
              <p>Coverage: 85%+</p>
              <p>Ready for Product Owner acceptance testing.</p>"
)
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
asana_create_task_story(
  task_id: "<task_gid>",
  text: "Progress Update

Agent: [BE]
Task: Implement login endpoint

Completed:
  ✅ Created LoginController
  ✅ Implemented token generation

In Progress:
  🔄 Adding unit tests

Next:
  ➡️ Complete error handling tests

Commit: abc123
Coverage: 78%"
)
```

### Blocker Comment

```
asana_create_task_story(
  task_id: "<task_gid>",
  text: "BLOCKER

Task: [Task name]
Blocker Type: Technical

Description:
Missing JWT library configuration for refresh token rotation.

Impact:
- Cannot complete token service
- FE integration blocked

Proposed Resolution:
Consult SA for architecture guidance on token rotation strategy.

Needs:
SA input on approach"
)
```

### Milestone Comment

```
asana_create_task_story(
  task_id: "<task_gid>",
  text: "Milestone: Backend API Complete

Feature: User Authentication
Completed: 2025-01-26

Summary:
All authentication endpoints implemented and tested.
Ready for frontend integration.

Stats:
- Tasks: 5 completed
- Commits: 12
- Coverage: 94%

Next:
FE can now integrate with authentication API"
)
```

## Bulk Operations

### Create Feature Tasks from SA Breakdown

```
# After getting project_id and section_id from previous calls:

# Backend tasks
asana_create_task(
  name: "[BE] Implement user repository",
  project_id: "<project_gid>",
  section_id: "<section_gid>",
  html_notes: "<h3>Acceptance Criteria:</h3>..."
)

asana_create_task(
  name: "[BE] Implement authentication service",
  project_id: "<project_gid>",
  section_id: "<section_gid>",
  html_notes: "<h3>Acceptance Criteria:</h3>...",
  followers: "backend-lead@example.com"
)

# Frontend tasks
asana_create_task(
  name: "[FE] Create login page component",
  project_id: "<project_gid>",
  section_id: "<section_gid>",
  html_notes: "<h3>Acceptance Criteria:</h3>...",
  assignee: "fe-dev@example.com"
)

asana_create_task(
  name: "[FE] Implement authentication context",
  project_id: "<project_gid>",
  section_id: "<section_gid>",
  html_notes: "<h3>Acceptance Criteria:</h3>..."
)

# Database tasks
asana_create_task(
  name: "[DB] Create users table migration",
  project_id: "<project_gid>",
  section_id: "<section_gid>",
  html_notes: "<h3>Acceptance Criteria:</h3>..."
)
```

### Setup Dependencies After Creating Tasks

```
# After creating tasks and capturing their GIDs:

asana_set_task_dependencies(
  task_id: "<fe_task_gid>",
  dependencies: ["<be_task_gid>", "<db_task_gid>"]
)

asana_set_task_dependents(
  task_id: "<be_task_gid>",
  dependents: ["<qa_task_gid>"]
)
```

## Error Handling

### Common MCP Error Patterns

The MCP tools will return error objects. Handle them gracefully:

**Example Pattern:**
```
1. If asana_create_task fails with "Project not found"
   → First call asana_get_projects to verify project_gid

2. If asana_update_task fails with "Task not found"
   → First call asana_get_task to verify task_gid

3. If asana_set_task_dependencies fails with "Dependency not found"
   → Verify dependency task GIDs are correct
```

## Quick Reference Workflow

```
# Initial Setup (do this once per session)
1. asana_list_workspaces()           # Get workspace_gid
2. asana_get_projects(workspace=...) # Get project_gid
3. asana_get_project_sections(...)   # Get section_gid

# Feature Creation
4. asana_create_task(...)            # Create tasks
5. asana_set_task_dependencies(...)  # Set dependencies

# Status Updates
6. asana_create_task_story(...)      # Add status comments
7. asana_update_task(...)            # Mark complete

# Queries
8. asana_search_tasks(...)           # Find tasks
9. asana_get_task(...)               # Get task details
```

## Best Practices

1. **Always discover workspace/project first** - Don't hardcode GIDs; use asana_list_workspaces and asana_get_projects
2. **Include context in html_notes** - Use HTML formatting for readable task descriptions
3. **Update status promptly** - Keep the project visible to PO with asana_create_task_story
4. **Use meaningful task names** - Start with [AGENT] prefix, include verb and component
5. **Document dependencies** - Use asana_set_task_dependencies to link related tasks
6. **Celebrate milestones** - Add milestone comments with emojis
7. **Document blockers** - Use asana_create_task_story to flag issues immediately
8. **Track coverage** - Always include test coverage percentage in status updates
9. **Use opt_fields** - Request only the fields you need for efficiency
10. **Handle errors gracefully** - Verify GIDs before using them in dependent calls
