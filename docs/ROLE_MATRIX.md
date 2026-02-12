# API Role Matrix

This document describes the current effective authorization behavior in `TaskManagement.Api`.

Legend:
- `Yes`: role can perform this action (subject to normal validation rules)
- `Scoped`: allowed only within accessible project scope
- `No`: role cannot perform this action

## Projects

| Endpoint / Action | Administrator | ProjectManager | User | Notes |
|---|---|---|---|---|
| `POST /api/projects` (create) | Yes | Yes | No | Policy `CanManageProjects` |
| `PUT /api/projects/{id}` (update) | Yes | Yes | No | PM/Admin not restricted by owner |
| `DELETE /api/projects/{id}` (delete) | Yes | Yes | No | PM/Admin not restricted by owner |
| `GET /api/projects/{id}` (read one) | Yes | Yes | Scoped | User must be owner/member |
| `GET /api/projects` (read list) | Yes (all) | Scoped | Scoped | Non-admin: owner/member projects |
| `GET /api/projects/my-projects` | Scoped | Scoped | Scoped | Owner/member projects |
| `GET /api/projects/{id}/members` | Yes | Scoped | Scoped | Non-admin must be member |

## Task Items

| Endpoint / Action | Administrator | ProjectManager | User | Notes |
|---|---|---|---|---|
| `POST /api/taskitems` (create) | Yes | Scoped | Scoped | PM/User must be owner/member |
| `PUT /api/taskitems/{id}` (update) | Yes | Scoped | Scoped | PM/User must be owner/member/assignee (member allowed) |
| `DELETE /api/taskitems/{id}` (delete) | Yes | Scoped | Scoped (owner-only) | PM must be owner/member; User remains stricter |
| `GET /api/taskitems/{id}` (read one) | Yes | Scoped | Scoped | PM/User must be owner/member |
| `GET /api/taskitems/project/{projectId}` | Yes | Yes | Scoped | PM has broad read; User must be owner/member |
| `GET /api/taskitems` (filtered list) | Yes (all) | Scoped | Scoped | Non-admin constrained to accessible projects |

### `GET /api/taskitems` filters

Supported query params:
- `projectId`
- `assignedUserId`
- `status`
- `unassignedOnly`
- `limit` (default `100`, max `500`)

## Activity

| Endpoint / Action | Administrator | ProjectManager | User | Notes |
|---|---|---|---|---|
| `GET /api/activity` | Yes (all) | Scoped | Scoped | Non-admin limited to member projects |
| SignalR `/hubs/activity` `JoinAllProjects` | Yes (admin group) | Scoped | Scoped | Admin auto-subscribed to global admin group |

## Known Intention Choices

- `ProjectManager` can manage projects globally.
- `ProjectManager` can manage tasks, but inside project scope.
- `User` can manage tasks, but task delete is intentionally stricter (owner-only in handler).
