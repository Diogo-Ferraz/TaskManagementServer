# TaskManagementServer

# Docker Setup for Task Management Application

This document describes the Docker configuration for the Task Management Application, which follows the vertical slice architecture and uses .NET 8 with SQL Server.

## Overview

The application is containerized using Docker and consists of three main components:

1. **API Service**: Handles CRUD operations for Projects and Tasks
2. **Auth Service**: Manages authentication and authorization using OpenIddict and .NET Identity
3. **SQL Server**: Database for both services

## Prerequisites

- Docker and Docker Compose installed
- .NET 8 SDK (for local development)
- HTTPS Certificate for development (see below)

## Services Configuration

### SQL Server
- Uses the official Microsoft SQL Server 2022 image
- Stores data in a persistent volume
- Runs initialization scripts automatically on first startup

### Auth Service
- Built from the Auth project Dockerfile
- Handles user authentication and authorization
- Exposes OpenID Connect endpoints

### API Service
- Built from the API project Dockerfile
- Provides CRUD operations for Projects and Tasks
- Communicates with the Auth service for validation

## Development Certificates

For local development with HTTPS, you need to generate a development certificate:

```bash
dotnet dev-certs https -ep ${HOME}/.aspnet/https/aspnetapp.pfx -p mypassword123
dotnet dev-certs https --trust
```

Make sure the password matches the one in your .env file.

## Environment Variables

The project uses environment variables for configuration. These are defined in:
- .env file (for sensitive information)
- docker-compose.override.yml (for development-specific settings)

## Building and Running

```bash
# Build and start the containers
docker-compose up -d

# View logs
docker-compose logs -f

# Stop containers
docker-compose down

# Rebuild containers after changes
docker-compose up -d --build
```

## Accessing the Services

- API Service: https://localhost:7140
- Auth Service: https://localhost:7134
- Swagger UI: https://localhost:7140/swagger

## Database Migrations

Migrations are automatically applied when the services start. If you need to run migrations manually:

```bash
# Connect to the API container
docker-compose exec api-service bash

# Run migrations
dotnet ef database update
```

## Production Deployment

For production, you should:
1. Use proper SSL certificates instead of development ones
2. Store secrets in a secure vault or environment variables
3. Consider using orchestration tools like Kubernetes