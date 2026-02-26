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
| `PATCH /api/projects/{id}` (partial update) | Yes | Yes | No | PM/Admin not restricted by owner |
| `DELETE /api/projects/{id}` (delete) | Yes | Scoped | No | PM must be project owner |
| `GET /api/projects/{id}` (read one) | Yes | Yes | Scoped | User must be owner/member |
| `GET /api/projects` (read list) | Yes (all) | Scoped | Scoped | Non-admin: owner/member projects |
| `GET /api/projects/my-projects` | Scoped | Scoped | Scoped | Owner/member projects |
| `GET /api/projects/{id}/members` | Yes | Scoped | Scoped | Non-admin must be member |

## Task Items

| Endpoint / Action | Administrator | ProjectManager | User | Notes |
|---|---|---|---|---|
| `POST /api/taskitems` (create) | Yes | Scoped | Scoped | PM/User must be owner/member |
| `PUT /api/taskitems/{id}` (update) | Yes | Scoped | Scoped | PM/User must be owner/member/assignee (member allowed) |
| `PATCH /api/taskitems/{id}` (partial update) | Yes | Scoped | Scoped | PM/User must be owner/member/assignee (member allowed) |
| `DELETE /api/taskitems/{id}` (delete) | Yes | Scoped | Scoped (assignee-only) | PM must be owner/member; User must be task assignee |
| `GET /api/taskitems/{id}` (read one) | Yes | Scoped | Scoped | PM/User must be owner/member |
| `GET /api/taskitems/project/{projectId}` | Yes | Yes | Scoped | PM has broad read; User must be owner/member |
| `GET /api/taskitems` (filtered list) | Yes (all) | Scoped | Scoped | Non-admin constrained to accessible projects |

### `GET /api/taskitems` filters

Supported query params:
- `projectId`
- `assignedUserId`
- `updatedByUserId`
- `search`
- `lastModifiedFrom`
- `lastModifiedTo`
- `status`
- `unassignedOnly`
- `page` (default `1`)
- `pageSize` (default `50`, max `500`)
- `limit` (legacy compatibility: first page only, max `500`)

## Activity

| Endpoint / Action | Administrator | ProjectManager | User | Notes |
|---|---|---|---|---|
| `GET /api/activity` | Yes (all) | Scoped | Scoped | Non-admin: member projects + own `ProjectDeleted` events |
| SignalR `/hubs/activity` `JoinAllProjects` | Yes (admin group) | Scoped | Scoped | Admin auto-subscribed to global admin group |

## Dashboard

| Endpoint / Action | Administrator | ProjectManager | User | Notes |
|---|---|---|---|---|
| `GET /api/dashboard/summary` | Yes (global scope) | Scoped | Scoped | Authenticated users; non-admin scoped to accessible projects/tasks |

## Known Intention Choices

- `ProjectManager` can manage projects globally.
- `ProjectManager` can manage tasks, but inside project scope.
- `User` can manage tasks, but task delete is intentionally stricter (assignee-only in handler).
- Project/member display names may include `(Inactive)` suffix when user directory reports a deactivated account.
