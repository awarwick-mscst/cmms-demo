# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CMMS (Computerized Maintenance Management System) - a full-stack web application with ASP.NET Core 8.0 backend, React 18 TypeScript frontend, and SQL Server database.

## Common Commands

### Backend
```bash
cd src/CMMS.API && dotnet run              # Run API (http://fragbox:5000, https://fragbox:5001)
dotnet build                                # Build all projects
dotnet test tests/CMMS.Tests.Unit           # Run unit tests
dotnet test tests/CMMS.Tests.Integration    # Run integration tests
dotnet publish src/CMMS.API -c Release -o ./publish  # Production build
```

### Frontend
```bash
cd frontend && npm start      # Dev server (http://fragbox:3000)
cd frontend && npm run build  # Production build
cd frontend && npm test       # Run tests
```

### Database
```bash
# Run migrations via sqlcmd against FRAGBOX\SQLEXPRESS, database CMMS
sqlcmd -S "FRAGBOX\SQLEXPRESS" -d CMMS -E -i "migrations/filename.sql"
```

## Architecture

### Backend (Clean Architecture)
```
src/
├── CMMS.API/           # Controllers, middleware, Program.cs (DI setup)
├── CMMS.Core/          # Entities, interfaces (no external dependencies)
├── CMMS.Infrastructure/# EF Core DbContext, repositories, service implementations
└── CMMS.Shared/        # DTOs, FluentValidation validators
```

**Key patterns:**
- Repository pattern with Unit of Work (`IUnitOfWork` coordinates repositories)
- All entities inherit from `BaseEntity` (Id, CreatedAt, UpdatedAt, IsDeleted, etc.)
- Soft deletes via `IsDeleted` flag with global query filters in EF configurations
- Service interfaces defined in Core, implementations in Infrastructure
- Entity configurations in `Infrastructure/Data/Configurations/`

### Frontend
```
frontend/src/
├── pages/          # Route components organized by domain
├── components/     # Reusable UI components by domain (/common, /assets, /inventory, etc.)
├── services/       # API client functions (one per domain)
├── store/          # Redux Toolkit slices
├── hooks/          # Custom React hooks
└── types/          # TypeScript interfaces
```

**Key patterns:**
- TanStack React Query for server state (queries use `['entity', id]` key pattern)
- Redux Toolkit for auth state only
- Material-UI (MUI) components throughout
- Services return `ApiResponse<T>` wrapper with `data`, `success`, `errors` fields

### Database Schemas
- `core` - Users, roles, permissions, audit logs, attachments
- `assets` - Assets, categories, locations
- `inventory` - Parts, suppliers, storage locations, stock tracking
- `maintenance` - Work orders, tasks, templates, preventive maintenance
- `admin` - Label templates and printers

## Key Integration Points

**Authentication:** JWT tokens with refresh tokens. Auth state in Redux, tokens in localStorage. Optional LDAP integration configurable in appsettings.json.

**File Attachments:** Stored in `wwwroot/uploads/{entityType}/{entityId}/`. Managed via `AttachmentService` and `FileStorageService`.

**Label Printing:** Supports ZPL (Zebra) and EPL formats. Templates stored in database, printers configured via admin UI.

## Configuration

- `src/CMMS.API/appsettings.json` - Connection string, JWT settings, LDAP, CORS origins
- `frontend/.env` - `REACT_APP_API_URL` pointing to API base URL

## Default Credentials

- Username: `admin`
- Password: `Admin@123`
