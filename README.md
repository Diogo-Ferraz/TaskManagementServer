# Task Management Server

A task and project management server composed of two independent services (Auth and API) and a SQL Server database. The system implements OAuth2 and OpenID Connect using OpenIddict, exposes secured endpoints validated by roles and scopes, and follows a vertical-slice feature architecture using MediatR, FluentValidation, AutoMapper, and EF Core.

Designed for containerized development with Docker and fronted by a Caddy reverse proxy that handles TLS termination and local development certificates.

---

## System Overview

Below is the architecture diagram describing the full stack:

                         +-----------------------+
                         |       SPA Client      |
                         |  (future standalone)  |
                         +-----------+-----------+
                                     |
                                     | HTTPS
                                     v
                     +---------------+---------------+
                     |             Caddy              |
                     |  Reverse Proxy + Internal TLS  |
                     +-------+---------------+--------+
                             |               |
                    HTTPS    |               |   HTTPS
                             v               v
                 +------------------+     +-------------------+
                 |   Auth Service   |     |    API Service    |
                 |   OpenIddict +   |     |    Access Token   |
                 | ASP.NET Identity |     |    Validation     |
                 +------------------+     +-------------------+
                            |                   |
                            |                   |
                            v                   v
                       +------------------------------+
                       |       SQL Server 2022        |
                       |   Database for Auth + API    |
                       +------------------------------+

---

## Features

### Domain Entities

The system exposes three core domain objects:

- **Projects**: contain metadata, ownership, and audit fields.
- **TaskItems**: belong to projects, include assignment and status state, and also include audit fields.
- **Users**: authenticated and authorized via ASP.NET Identity.

Each entity except Users includes audit metadata:

- CreatedBy and CreatedAt
- LastUpdatedBy and LastUpdatedAt

### Security Model

Three roles govern permissions:

- **ProjectManager**: Full CRUD on Projects, can create and assign Tasks.
- **User**: CRUD on TaskItems only.
- **Administrator**: Full permissions across both domains.

Auth implements OAuth2 Authorization Code Flow with PKCE. The API enforces authorization using:

- Scopes
- Role checks
- Resource ownership validation

---

## Architecture

### Why Vertical Slice Architecture

Vertical slice architecture organizes the application by features, not layers. Each feature contains:

- Commands and queries
- Handlers
- Validators
- Mapping profiles
- Controller endpoint definitions

Benefits:

- Faster comprehension of each feature
- No mega service classes
- Fewer merge conflicts in team environments
- Clean separation of concerns

### Patterns and Libraries

- **MediatR**: dispatches commands and queries.
- **FluentValidation**: colocated validators per command.
- **AutoMapper**: DTO mapping configured per slice.
- **EF Core**: persistence with entity configurations.
- **Serilog**: structured logging and request pipeline logging.
- **Health Checks**: integrated for container diagnostics.

---

## Services

### Auth Service

- OpenIddict-powered OAuth2 and OpenID Connect server.
- ASP.NET Identity for users and roles.
- Authorization Code + PKCE, refresh tokens.
- Razor UI for login and consent.
- Issues access and refresh tokens to the SPA or future clients.

### API Service

- Vertical slice features (Projects, TaskItems).
- Validates JWTs and role-based policies.
- Enforces ownership and domain rules inside handlers.
- Includes both unit and integration tests.

### Database

- SQL Server 2022 container.
- Initialization scripts included.
- Used by both Auth and API.

---

## Reverse Proxy and HTTPS

Caddy runs as the entrypoint of the environment.

- Terminates TLS for all services.
- Issues local development certificates using `tls internal`.
- Routes to Auth and API using service names.

---

## Testing

- Auth Service: full integration test suite.
- API Service: unit and integration tests across features.

Tests validate:

- Authorization rules
- Domain logic
- Handler correctness
- Persistence

---

## Local Development

### Requirements
- Docker
- Docker Compose
- Hosts file update: map `api.localhost` and `auth.localhost` to `127.0.0.1`.

### Run

```bash
docker-compose up --build
```

Everything starts fully configured, including TLS and routing.

---

