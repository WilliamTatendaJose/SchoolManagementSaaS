# School Management System (SMS) SaaS

A multi-tenant School Management System built with ASP.NET Core (.NET 8), following Clean Architecture principles.

## ??? Architecture

The solution follows **Clean Architecture** with the following layers:

```
src/
??? SMS.Domain/           # Domain entities, enums, and business logic
??? SMS.Application/      # Application services, CQRS handlers, interfaces
??? SMS.Infrastructure/   # Data access, external services, authentication
??? SMS.API/              # REST API endpoints, middleware
tests/
??? SMS.Domain.Tests/     # Domain unit tests
??? SMS.Application.Tests/# Application layer tests
```

## ?? Technology Stack

- **Backend**: ASP.NET Core (.NET 8)
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Storage**: AWS S3
- **Logging**: Serilog
- **Validation**: FluentValidation
- **CQRS**: MediatR
- **Testing**: xUnit, FluentAssertions, Moq

## ?? Features

### MVP (Phase 1)
- ? Multi-tenant architecture with tenant isolation
- ? Student management
- ? Guardian/Parent management
- ? Attendance tracking with SMS notifications
- ? Fee management and payments
- ? Role-based access control (RBAC)
- ? JWT authentication

### Phase 2 (Planned)
- ?? Learning Management System (LMS)
- ?? Analytics and dashboards
- ?? Transport management
- ?? Asset management
- ?? Library management

## ?? Prerequisites

- .NET 9 SDK
- PostgreSQL 14+
- AWS Account (for S3 storage)

## ?? Configuration

Update `appsettings.json` with your settings:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=sms_db;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "SMS.API",
    "Audience": "SMS.Client"
  },
  "AWS": {
    "S3": {
      "BucketName": "your-bucket-name"
    }
  }
}
```

## ?? Running the Application

```bash
# Restore packages
dotnet restore

# Apply database migrations
cd src/SMS.Infrastructure
dotnet ef database update --startup-project ../SMS.API

# Run the API
cd ../SMS.API
dotnet run
```

The API will be available at `https://localhost:5001` with Swagger UI at the root URL.

## ?? Running Tests

```bash
dotnet test
```

## ?? API Documentation

Once running, access Swagger UI at the root URL for interactive API documentation.

### Key Endpoints

| Endpoint | Description |
|----------|-------------|
| `POST /api/auth/login` | Authenticate and get JWT token |
| `GET /api/students` | List students (paginated) |
| `POST /api/students` | Create a new student |
| `POST /api/attendance` | Mark attendance |
| `GET /api/finance/invoices` | List invoices |
| `POST /api/finance/payments` | Record a payment |

## ?? Multi-Tenancy

The system uses a shared database with tenant isolation via `TenantId`:

- Each request is scoped to a tenant based on JWT claims
- Global query filters ensure data isolation
- Tenant context is set via middleware

## ??? Domain Model

### Core Entities
- **Tenant** - School/organization
- **User** - System users with roles
- **Student** - Student records
- **Guardian** - Parent/guardian information
- **Class** - Grade levels
- **Subject** - Academic subjects
- **Attendance** - Daily/period attendance
- **Invoice/Payment** - Financial transactions

## ?? Project Structure

```
SMS.Domain/
??? Common/          # Base entities, interfaces
??? Entities/        # Domain entities
??? Enums/           # Enumerations

SMS.Application/
??? Common/          # Behaviors, models
??? Features/        # CQRS commands/queries by feature
??? Interfaces/      # Service interfaces

SMS.Infrastructure/
??? Persistence/     # DbContext, configurations
??? Services/        # Service implementations

SMS.API/
??? Controllers/     # API endpoints
??? *.cs             # Middleware, DI configuration
```

## ?? Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Open a Pull Request

## ?? License

This project is licensed under the MIT License.
