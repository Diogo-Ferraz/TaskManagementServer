# API Contract Guide

This guide centralizes SPA-facing API behavior across both services:
- `TaskManagement.Api`
- `TaskManagement.Auth` (admin user-management APIs under `api/*`)

Use this as the single contract reference for filters, pagination, patch semantics, and error format.

## Pagination

List endpoints support pagination:
- `GET /api/projects`
- `GET /api/taskitems`
- `GET /api/activity`
- `GET /api/users` (Auth service, admin-only)

Defaults and caps:
- Projects: `page=1`, `pageSize=50`, max `200`
- TaskItems: `page=1`, `pageSize=50`, max `500`
- Activity: `page=1`, `pageSize=50`, max `200`
- Users: `page=1`, `pageSize=25`, max `100`

Legacy compatibility:
- `GET /api/taskitems` and `GET /api/activity` support `limit` (first page only, capped).
- `GET /api/users` supports `skip/take` in addition to `page/pageSize`.

## Filters

### Tasks (`GET /api/taskitems`)

Supported query params:
- `projectId`
- `assignedUserId`
- `updatedByUserId`
- `status`
- `unassignedOnly`
- `search` (title/description contains)
- `lastModifiedFrom` (inclusive)
- `lastModifiedTo` (inclusive)
- `page`, `pageSize`, `limit`

Example:

```http
GET /api/taskitems?projectId=...&status=InProgress&updatedByUserId=user-123&search=api&lastModifiedFrom=2026-02-01T00:00:00Z&page=1&pageSize=20
```

### Users (Auth: `GET /api/users`)

Supported query params:
- `search` (displayName/email/userName contains)
- `isActive`
- `role`
- `page`, `pageSize`, `skip`, `take`

## Patch Semantics

Patch endpoints:
- `PATCH /api/projects/{id}`
- `PATCH /api/taskitems/{id}`

Behavior:
- Field omitted from JSON: unchanged
- Field included with value: updated
- Field included with `null`: cleared (for nullable fields)

### Project patch example

```json
{
  "name": "Platform API",
  "description": null
}
```

Result:
- `name` updated
- `description` cleared

### Task patch example

```json
{
  "status": "Done",
  "dueDate": null,
  "assignedUserId": null
}
```

Result:
- `status` updated
- `dueDate` cleared
- assignment cleared

## Auth Admin User Management

Endpoints:
- `GET /api/users`
- `GET /api/users/{id}/details`
- `PATCH /api/users/{id}/status`

Status change payload:

```json
{
  "isActive": false
}
```

Safety rules:
- Admin cannot deactivate own account.
- Last active administrator cannot be deactivated.

## Activity Event Types

`GET /api/activity` and SignalR `activity-created` can return:
- `ProjectCreated`
- `ProjectRenamed`
- `ProjectDeleted`
- `TaskCreated`
- `TaskStatusChanged`
- `TaskRenamed`
- `TaskDeleted`
- `TaskAssigneeChanged`
- `TaskDueDateChanged`

Additional payload fields:
- Rename/assignee/due-date events: `oldValue`, `newValue`
- Status transitions: `oldStatus`, `newStatus`

## Dashboard

Endpoint:
- `GET /api/dashboard/summary`

Behavior:
- Returns aggregated counters for current user scope.
- Administrator sees global scope.

## Error Contract (RFC 7807)

Both services use `application/problem+json` for API errors.

Base shape:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/resource"
}
```

Validation errors include `errors` (ValidationProblemDetails):

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/users/123/status",
  "errors": {
    "fieldName": [
      "Example validation message."
    ]
  }
}
```

Expected error codes:
- `400` validation/business rule
- `401` unauthenticated
- `403` unauthorized
- `404` not found
- `429` rate limit exceeded
- `500` unexpected error
