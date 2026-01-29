# CMMS - Computerized Maintenance Management System

A full-stack maintenance management system built with ASP.NET Core 8.0, React 18, TypeScript, and SQL Server 2022.

## Project Structure

```
├── src/
│   ├── CMMS.API/              # ASP.NET Core Web API
│   ├── CMMS.Core/             # Domain models, interfaces
│   ├── CMMS.Infrastructure/   # Data access (EF Core + SQL Server)
│   └── CMMS.Shared/           # DTOs, validators
├── tests/
│   ├── CMMS.Tests.Unit/       # Unit tests
│   └── CMMS.Tests.Integration/ # Integration tests
├── frontend/                   # React TypeScript app
├── scripts/                    # SQL migration scripts
└── CMMS.sln
```

## Prerequisites

- .NET 8.0 SDK
- Node.js 18+ and npm
- SQL Server 2022 (or SQL Server Express)
- Visual Studio 2022 or VS Code

## Getting Started

### 1. Database Setup

1. Create a database named `CMMS` in SQL Server
2. Run the SQL scripts in order:
   ```
   scripts/001_InitialSchema.sql
   scripts/002_SeedData.sql
   scripts/003_CreateIndexes.sql
   ```

### 2. Backend Configuration

1. Update `src/CMMS.API/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=YOUR_SERVER;Database=CMMS;User Id=YOUR_USER;Password=YOUR_PASSWORD;TrustServerCertificate=True"
     },
     "JwtSettings": {
       "Secret": "YOUR_SECRET_KEY_MUST_BE_AT_LEAST_32_CHARACTERS_LONG"
     }
   }
   ```

2. Build and run the API:
   ```bash
   cd src/CMMS.API
   dotnet restore
   dotnet run
   ```

   The API will be available at `https://localhost:5001` (or `http://localhost:5000`)

### 3. Frontend Setup

1. Install dependencies:
   ```bash
   cd frontend
   npm install
   ```

2. Create `.env` file:
   ```
   REACT_APP_API_URL=http://localhost:5000/api/v1
   ```

3. Start the development server:
   ```bash
   npm start
   ```

   The app will be available at `http://localhost:3000`

## Default Login

- **Username:** admin
- **Password:** Admin@123

## API Endpoints

### Authentication
- `POST /api/v1/auth/login` - Login
- `POST /api/v1/auth/refresh` - Refresh token
- `POST /api/v1/auth/logout` - Logout
- `POST /api/v1/auth/register` - Register user (Admin only)
- `POST /api/v1/auth/change-password` - Change password
- `GET /api/v1/auth/me` - Get current user

### Assets
- `GET /api/v1/assets` - List assets (paginated, filterable)
- `GET /api/v1/assets/{id}` - Get asset by ID
- `POST /api/v1/assets` - Create asset
- `PUT /api/v1/assets/{id}` - Update asset
- `DELETE /api/v1/assets/{id}` - Soft delete asset

### Asset Categories
- `GET /api/v1/assetcategories` - List categories
- `GET /api/v1/assetcategories/{id}` - Get category by ID
- `POST /api/v1/assetcategories` - Create category
- `PUT /api/v1/assetcategories/{id}` - Update category
- `DELETE /api/v1/assetcategories/{id}` - Delete category

### Asset Locations
- `GET /api/v1/assetlocations` - List locations
- `GET /api/v1/assetlocations/tree` - Get location hierarchy
- `GET /api/v1/assetlocations/{id}` - Get location by ID
- `POST /api/v1/assetlocations` - Create location
- `PUT /api/v1/assetlocations/{id}` - Update location
- `DELETE /api/v1/assetlocations/{id}` - Delete location

### Health Check
- `GET /api/health` - API health status

## Features

### Phase 1 (Current)
- JWT authentication with refresh tokens
- Role-based access control (RBAC)
- Asset CRUD operations
- Asset categories (hierarchical)
- Asset locations (hierarchical)
- Audit logging
- Pagination and filtering
- Swagger API documentation

### Default Roles
- **Administrator** - Full system access
- **Manager** - Manage assets, view reports
- **Technician** - Execute work, update assets
- **Viewer** - Read-only access

## Technology Stack

### Backend
- ASP.NET Core 8.0
- Entity Framework Core 8.0
- SQL Server 2022
- JWT Authentication
- FluentValidation
- Serilog
- Swagger/OpenAPI

### Frontend
- React 18
- TypeScript
- Material-UI (MUI) v5
- Redux Toolkit
- React Query (TanStack Query)
- React Router v6
- React Hook Form
- Axios

## Development

### Running Tests
```bash
# Unit tests
dotnet test tests/CMMS.Tests.Unit

# Integration tests
dotnet test tests/CMMS.Tests.Integration
```

### Building for Production
```bash
# Backend
dotnet publish src/CMMS.API -c Release -o ./publish

# Frontend
cd frontend
npm run build
```

## Deployment

### IIS Deployment
1. Install .NET 8.0 Hosting Bundle on Windows Server
2. Publish the API to a folder
3. Create an IIS website pointing to the publish folder
4. Configure the application pool for "No Managed Code"
5. Deploy the frontend build to a separate IIS site or subdirectory

## License

Proprietary - All rights reserved
