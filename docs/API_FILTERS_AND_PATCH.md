# API Filters And Patch Guide

This guide summarizes the SPA-facing query filters, pagination rules, and partial update behavior.

## Pagination

List endpoints support pagination with `page` and `pageSize`:
- `GET /api/projects`
- `GET /api/taskitems`
- `GET /api/activity`

Defaults and caps:
- Projects: default `page=1`, `pageSize=50`, max `200`
- TaskItems: default `page=1`, `pageSize=50`, max `500`
- Activity: default `page=1`, `pageSize=50`, max `200`

Legacy compatibility:
- `GET /api/taskitems` and `GET /api/activity` still support `limit` (mapped to first page with capped size).

## Task Filters

`GET /api/taskitems` supports:
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

## Patch Semantics

Patch endpoints:
- `PATCH /api/projects/{id}`
- `PATCH /api/taskitems/{id}`

Behavior:
- Field omitted from JSON: unchanged.
- Field included with value: updated.
- Field included with `null`: cleared for nullable fields.

### Project Patch Payload

```json
{
  "name": "Platform API",
  "description": null
}
```

Result:
- `name` updated
- `description` cleared

### Task Patch Payload

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

## Dashboard

Dashboard summary endpoint:
- `GET /api/dashboard/summary`

Returns aggregated counters for the current user scope (administrator sees global scope).
