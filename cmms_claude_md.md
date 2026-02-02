# CMMS Project Configuration

## Project Overview

On-premises Computerized Maintenance Management System (CMMS) built for enterprise deployment. This system provides comprehensive maintenance operations management with work order tracking, preventive maintenance scheduling, asset management, inventory control, and reporting capabilities.

## Technology Stack

### Database
- **Microsoft SQL Server+** (Primary choice for robust features, JSON support, and enterprise reliability)
- Reasons: ACID compliance, advanced indexing, partitioning support, free licensing, excellent performance for OLTP workloads

### Backend
- **ASP.NET Core 8.0** (C#)
- RESTful API architecture
- Entity Framework Core for ORM
- JWT authentication with role-based access control (RBAC)

### Frontend
- **React 18+** with TypeScript
- Material-UI (MUI) component library
- Redux Toolkit for state management
- React Query for server state management

### Infrastructure
- Nginx reverse proxy
- Redis for caching and session management
- Windows Server 2022 or Linux (Ubuntu 22.04 LTS) for production hosting

## Database Architecture

### Core Schema Design Principles
1. **Normalization**: Third normal form (3NF) minimum
2. **Audit Trails**: All tables include created_at, created_by, modified_at, modified_by
3. **Soft Deletes**: Use is_deleted flag rather than hard deletes
4. **Indexing Strategy**: Index all foreign keys, frequently queried fields, and date ranges
5. **Partitioning**: Implement table partitioning for work_orders and maintenance_logs by date ranges

### Primary Tables

**Assets Management**
- `assets`: Equipment/asset registry with specifications, locations, criticality ratings
- `asset_categories`: Hierarchical categorization system
- `asset_locations`: Physical location hierarchy (site → building → floor → room)
- `asset_documents`: Manuals, warranties, compliance certificates
- `asset_relationships`: Parent-child asset relationships

**Work Order System**
- `work_orders`: Main work order tracking table
- `work_order_tasks`: Individual tasks within work orders
- `work_order_labor`: Time tracking for technicians
- `work_order_parts`: Parts/materials consumed
- `work_order_comments`: Communication thread
- `work_order_attachments`: Photos, diagrams, documentation

**Preventive Maintenance**
- `pm_schedules`: Recurring maintenance definitions
- `pm_triggers`: Time-based, meter-based, or condition-based triggers
- `pm_templates`: Standard task lists and procedures
- `pm_history`: Execution history and compliance tracking

**Inventory Management**
- `inventory_items`: Parts and supplies catalog
- `inventory_locations`: Warehouse/storage locations
- `inventory_transactions`: Stock movements (receipts, issues, transfers, adjustments)
- `inventory_reservations`: Parts allocated to work orders
- `purchase_orders`: Procurement tracking
- `vendors`: Supplier information

**Personnel & Resources**
- `users`: System users with authentication credentials
- `roles`: RBAC role definitions
- `permissions`: Granular permission matrix
- `technicians`: Maintenance staff profiles with skills/certifications
- `teams`: Work groups and assignments
- `shifts`: Scheduling and availability

**Reporting & Analytics**
- `downtime_logs`: Equipment downtime tracking
- `meter_readings`: Usage counters (hours, cycles, mileage)
- `cost_centers`: Financial allocation tracking
- `kpi_snapshots`: Historical performance metrics

## Coding Standards

### C# Backend Standards
1. **Naming Conventions**:
   - PascalCase for classes, methods, properties
   - camelCase for local variables and parameters
   - Prefix interfaces with `I` (e.g., `IWorkOrderService`)
   - Suffix repositories with `Repository` (e.g., `AssetRepository`)

2. **Project Structure**:
   ```
   /src
     /CMMS.API          # Web API controllers and middleware
     /CMMS.Core         # Domain models, interfaces, business logic
     /CMMS.Infrastructure # Data access, external services
     /CMMS.Shared       # Common utilities, DTOs, constants
   /tests
     /CMMS.Tests.Unit
     /CMMS.Tests.Integration
   ```

3. **Design Patterns**:
   - Repository pattern for data access
   - Unit of Work for transaction management
   - Dependency Injection throughout
   - CQRS pattern for complex queries vs commands
   - Factory pattern for entity creation

4. **Error Handling**:
   - Custom exception types for domain-specific errors
   - Global exception middleware for API responses
   - Structured logging with Serilog
   - Return standardized API response format

5. **Validation**:
   - FluentValidation for request validation
   - Business rule validation in service layer
   - Database constraints for data integrity

### TypeScript Frontend Standards
1. **Component Structure**:
   - Functional components with hooks
   - Separate presentational and container components
   - Custom hooks for reusable logic
   - One component per file

2. **File Naming**:
   - PascalCase for component files (e.g., `AssetDetails.tsx`)
   - camelCase for utilities and hooks (e.g., `useWorkOrders.ts`)
   - kebab-case for CSS modules (e.g., `asset-card.module.css`)

3. **State Management**:
   - Redux Toolkit slices for global state
   - React Query for server state
   - Local state with useState for component-specific data
   - Context API for theme and authentication state

4. **Type Safety**:
   - Strict TypeScript configuration
   - Define interfaces for all API responses
   - Avoid `any` type; use `unknown` when necessary
   - Generate API types from backend DTOs

### SQL Standards
1. **Naming Conventions**:
   - snake_case for all database objects
   - Plural table names (e.g., `work_orders`)
   - Descriptive column names avoiding abbreviations
   - Prefix foreign keys with table name (e.g., `asset_id`)

2. **Schema Organization**:
   - Use schemas to separate functional areas: `core`, `inventory`, `maintenance`, `reporting`
   - Create views for complex frequently-used queries
   - Stored procedures for complex business logic operations
   - Functions for reusable calculations

3. **Performance**:
   - Create indexes on foreign keys and frequently queried columns
   - Use covering indexes for read-heavy queries
   - Implement table partitioning for large historical tables
   - Regular VACUUM and ANALYZE operations

## Security Requirements

### Authentication & Authorization
- JWT tokens with 60-minute expiration
- Refresh token mechanism (7-day expiration)
- Password complexity requirements (12+ chars, mixed case, numbers, symbols)
- Account lockout after 5 failed attempts (30-minute lockout)
- Multi-factor authentication (TOTP) for admin roles

### Data Protection
- Encrypt sensitive data at rest (AES-256)
- TLS 1.3 for all network communication
- Parameterized queries to prevent SQL injection
- Input sanitization and validation on all endpoints
- CORS policy restricting to authorized domains

### Audit & Compliance
- Log all authentication attempts
- Track all data modifications with user and timestamp
- Maintain 7-year audit history for compliance
- Generate audit reports for regulatory review
- Implement data retention policies

## API Design Standards

### RESTful Conventions
- Use HTTP verbs correctly (GET, POST, PUT, PATCH, DELETE)
- Versioned endpoints: `/api/v1/assets`
- Consistent resource naming (plural nouns)
- Return appropriate HTTP status codes
- Pagination for list endpoints (default 50 items, max 200)

### Request/Response Format
```json
{
  "success": true,
  "data": { ... },
  "message": "Operation completed successfully",
  "timestamp": "2026-01-22T10:30:00Z"
}
```

### Error Response Format
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid input data",
    "details": [
      {
        "field": "asset_id",
        "message": "Asset ID is required"
      }
    ]
  },
  "timestamp": "2026-01-22T10:30:00Z"
}
```

## Performance Requirements

### Response Time Targets
- API endpoints: < 200ms (p95)
- Complex reports: < 2 seconds (p95)
- Dashboard load: < 1 second
- Search operations: < 500ms

### Scalability Targets
- Support 500+ concurrent users
- Handle 10,000+ active assets
- Process 1,000+ work orders per day
- Maintain 5+ years of historical data

### Optimization Strategies
- Implement Redis caching for frequently accessed data
- Use database connection pooling
- Lazy loading for related entities
- Background jobs for heavy operations (reports, notifications)
- CDN for static assets

## Testing Requirements

### Unit Testing
- Minimum 80% code coverage for business logic
- xUnit for .NET tests
- Jest for TypeScript tests
- Mock external dependencies
- Test all validation rules

### Integration Testing
- Test database operations with test containers
- API endpoint testing with WebApplicationFactory
- Test authentication and authorization flows

### End-to-End Testing
- Playwright for UI automation
- Critical user workflows (create work order, schedule PM, manage inventory)
- Cross-browser compatibility (Chrome, Firefox, Edge)

## Development Workflow

### Version Control
- Git with feature branch workflow
- Branch naming: `feature/`, `bugfix/`, `hotfix/`
- Meaningful commit messages following conventional commits
- Pull requests require code review and passing tests
- Squash commits before merging to main

### CI/CD Pipeline
- Automated testing on every commit
- Code quality checks (SonarQube or similar)
- Build Docker images on successful tests
- Deploy to staging environment automatically
- Manual approval for production deployment

### Environment Configuration
- Staging: Mirror production with anonymized data
- Production: High availability configuration with load balancing

## Reporting & Analytics

### Standard Reports
- Asset downtime analysis by category/location
- Work order completion metrics (backlog, completion rate, MTTR)
- Preventive maintenance compliance rates
- Inventory turnover and stock levels
- Cost tracking by asset, department, or cost center
- Technician productivity and utilization

### Dashboard KPIs
- Active work orders by status
- Overdue preventive maintenance tasks
- Critical assets requiring attention
- Parts below reorder point
- Monthly maintenance cost trends

## Mobile Considerations

### Responsive Design
- Mobile-first CSS approach
- Touch-friendly UI elements (minimum 44px touch targets)
- Offline capability for work order updates
- Progressive Web App (PWA) for mobile installation

## Documentation Requirements

### Code Documentation
- XML documentation comments for all public APIs
- README files in each project directory
- Architecture decision records (ADRs) for major decisions
- Database schema diagrams (ERD)

### User Documentation
- Administrator guide for system configuration
- User guide for common workflows
- API documentation (Swagger/OpenAPI)
- Video tutorials for key features

## Deployment Architecture

### Production Environment
- Primary database server (Microsoft SQL Server)
- Application server (ASP.NET Core API)
- Web server (Nginx serving React frontend)
- Redis server (caching and sessions)
- Backup server (automated daily backups with 30-day retention)

### High Availability Options
- Microsoft SQL streaming replication (primary + standby)
- Load balancer for API servers (if scaling horizontally)
- Database connection pooling (PgBouncer)
- Automated failover procedures

### Monitoring & Alerting
- Application Performance Monitoring (APM)
- Database query performance monitoring
- Server resource monitoring (CPU, RAM, disk I/O)
- Error rate and response time alerts
- Scheduled backup verification

## Initial Development Phases

### Phase 1: Foundation (Day 1-2)
- Database schema design and creation
- Authentication and authorization system
- Basic API framework and middleware
- Frontend project structure and routing
- Asset management CRUD operations

### Phase 2: Core Features (Day 3-8)
- Work order management system
- Preventive maintenance scheduling
- Inventory management
- User and team management
- Basic reporting

### Phase 3: Advanced Features (Day 9 - 14)
- Dashboard and analytics
- Mobile responsiveness
- Email notifications
- Document management
- Advanced reporting and exports

### Phase 4: Polish & Deployment (Day 15 - 21)
- Performance optimization
- Security hardening
- User acceptance testing
- Documentation completion
- Production deployment

## Claude Code Behavior

When working on this project:

1. **Always use Microsoft SQL-specific syntax** for database operations
2. **Follow the established project structure** exactly as defined
3. **Generate migrations** for all database schema changes using Entity Framework Core
4. **Implement comprehensive error handling** for all operations
5. **Write unit tests** for new business logic automatically
6. **Use strongly-typed DTOs** for all API requests and responses
7. **Apply RBAC checks** on all endpoints that modify data
8. **Log significant operations** using structured logging
9. **Validate all inputs** using FluentValidation before processing
10. **Update API documentation** (Swagger annotations) when modifying endpoints
11. **Create database indexes** for new frequently-queried columns
12. **Implement soft deletes** rather than hard deletes for data integrity

## Questions to Ask Before Implementation

When receiving a task, Claude should clarify:

1. **Which module/area** does this affect? (Assets, Work Orders, Inventory, PM, etc.)
2. **Should this be accessible via API, UI, or both?**
3. **What permission level** is required to perform this action?
4. **Are there any specific business rules or validation requirements?**
5. **Should this operation be logged/audited?**
6. **Are there any performance considerations** (large datasets, complex queries)?
7. **Does this require any background processing** (scheduled jobs, async operations)?

## Success Criteria

The CMMS system will be considered successful when it:

1. Reduces average work order response time by 30%
2. Achieves 95%+ preventive maintenance schedule compliance
3. Provides real-time visibility into asset status and availability
4. Reduces inventory carrying costs through optimized stock levels
5. Generates comprehensive reports meeting regulatory requirements
6. Maintains 99.5%+ system uptime during business hours
7. Receives positive feedback from maintenance technicians and management