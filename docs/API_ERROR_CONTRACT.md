# API Error Contract

This project standardizes error responses around RFC 7807 `ProblemDetails` (`application/problem+json`) across both services:
- `TaskManagement.Api`
- `TaskManagement.Auth` (for `api/*` endpoints)

## Base Shape

All error responses should follow:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation Error",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "instance": "/api/resource"
}
```

Common fields:
- `type`: URI describing the error category.
- `title`: Short human-readable summary.
- `status`: HTTP status code.
- `detail`: Specific message for this occurrence.
- `instance`: Request path that produced the error.

## Validation Errors (400)

Validation uses `ValidationProblemDetails` and includes an `errors` object:

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

## Expected Status Codes

Typical endpoint-level errors:
- `400`: request validation or business rule violation
- `401`: missing/invalid authentication
- `403`: authenticated but not authorized
- `404`: entity/resource not found
- `429`: rate limit exceeded (auth/admin-sensitive endpoints)
- `500`: unexpected server error

## Swagger Coverage

Swagger examples are configured to show standardized `ProblemDetails` examples for common error codes:
- API service: via operation filter in Swagger config.
- Auth service: via operation filter in Swagger config (for `api/*` documentation).

This keeps SPA error handling predictable and consistent across services.
