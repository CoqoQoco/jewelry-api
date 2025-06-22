# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Architecture

This is a **Jewelry Management System API** built with .NET 8 and Entity Framework Core using PostgreSQL. The project follows a clean architecture pattern with distinct layers:

### Layer Structure
- **Jewelry.Api**: Web API controllers and middleware (main entry point)
- **Jewelry.Service**: Business logic and service implementations
- **Jewelry.Data**: Entity Framework models and database context
- **jewelry.Model**: DTOs, requests, responses, and constants
- **DynamicLinqCore**: Custom LINQ extensions for dynamic queries

### Key Features
- **Authentication**: JWT-based authentication with user status validation
- **Production Planning**: Complex jewelry production workflow management
- **Stock Management**: Inventory tracking for gems, molds, and finished products
- **Worker Management**: Production worker tracking and wages
- **Receipt System**: Production receipts and material handling
- **Cost Tracking**: Gold cost calculations and BOM management

## Development Commands

### Database Operations
```bash
# Generate database models from existing PostgreSQL database
cd Jewelry.Data
dotnet ef dbcontext scaffold 'Server=localhost;Port=5432;Database=postgres;User Id=postgres;Password=winsun24;Trust Server Certificate=true;' Npgsql.EntityFrameworkCore.PostgreSQL -o Models/Jewelry --context-dir Context/ --context JewelryContext -f --no-pluralize --no-onconfiguring
```

### Running the Application
```bash
# Development (from Jewelry.Api directory)
dotnet run --project Jewelry.Api

# Docker
cd Jewelry.Api  # Root directory with docker-compose.yml
docker-compose down
docker-compose build
docker-compose up -d
```

### Build and Test
```bash
# Build entire solution
dotnet build Jewelry.Api.sln

# Run from specific project
dotnet run --project Jewelry.Api/Jewelry.Api.csproj
```

## Configuration Notes

### Database Connection
- **Development**: Uses PostgreSQL on localhost:5432
- **Docker**: Uses `host.docker.internal:5432` to connect to host database
- Connection string configured in `appsettings.json`

### API Endpoints
- **Development**: http://localhost:7000, https://localhost:7001
- **Docker**: Port 2001 mapped to container port 80
- **Swagger**: Available at `/swagger` in development

### Authentication
- JWT tokens required for most endpoints
- User status validation occurs on each request
- CORS configured for frontend at ports 5173, 7000, 7001

## Architecture Patterns

### Service Registration
All services are registered in `InfrastructureServiceRegistration.cs` using dependency injection pattern.

### Controller Base Class
Controllers extend `ApiControllerBase` for consistent model state handling.

### Middleware Pipeline
1. `AuthenticationMiddleware` - Custom authentication logic
2. `ExceptionMiddleware` - Global exception handling
3. Standard ASP.NET Core middleware

### Database Context
- `JewelryContext` provides access to all entities
- Models are generated from existing database schema
- Uses PostgreSQL with Npgsql provider

## Key Business Domains

### Production Planning (`TbtProductionPlan*`)
Complex workflow involving design, casting, cutting, gems, and finishing stages.

### Mold Management (`TbtProductMold*`)
Tracking of jewelry molds through various production stages.

### Stock Management (`TbtStock*`)
Inventory management for gems, products, and materials.

### Worker Management (`TbmWorker`, `TbtUser`)
Production worker assignments and wage calculations.

## Development Guidelines

### Entity Framework
- Models are auto-generated - avoid manual edits
- Use migrations for schema changes in development
- Database-first approach with scaffold commands

### API Patterns
- Request/Response DTOs in `jewelry.Model`
- Service layer for business logic
- Repository pattern through Entity Framework

### File Storage
Images stored in `Images/` subdirectories organized by feature (Mold, Stock, etc.)