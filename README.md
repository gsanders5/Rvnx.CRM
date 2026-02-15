# Rvnx CRM System

A modern CRM system built with ASP.NET Core 8.0 using clean architecture principles and Entity Framework Core.

## Project Structure

```
.
├── Rvnx.CRM.Core/              # Domain layer (business entities, interfaces)
│   ├── Interfaces/
│   │   └── IRepository.cs      # Generic repository interface
│   ├── Models/
│   │   ├── CRMBaseEntity.cs    # Base entity with audit trail
│   │   └── Person.cs           # Person entity
│   └── Rvnx.CRM.Core.csproj
├── Rvnx.CRM.Infrastructure/    # Data access layer
│   ├── Data/
│   │   └── CRMDbContext.cs     # EF Core DbContext
│   ├── Migrations/             # EF Core migrations
│   ├── Repositories/
│   │   └── Repository.cs       # Generic repository implementation
│   ├── ServiceCollectionExtensions.cs
│   └── Rvnx.CRM.Infrastructure.csproj
├── Rvnx.CRM.Shared/           # Shared models/DTOs (future use)
│   └── Rvnx.CRM.Shared.csproj
└── Rvnx.CRM.Web/              # Presentation layer
    ├── Controllers/
    │   └── HomeController.cs   # Main controller with repository demo
    ├── Views/
    │   └── Home/
    │       └── Index.cshtml    # People listing view
    ├── Program.cs              # Application startup
    ├── appsettings.json        # Configuration
    └── Rvnx.CRM.Web.csproj
```

## Architecture

### Clean Architecture Principles
- **Core**: Contains business entities and interfaces. No dependencies on other projects.
- **Infrastructure**: Implements Core interfaces, handles data access. Depends on Core.
- **Shared**: DTOs and view models. Depends on Core.
- **Web**: Presentation layer. Depends on all other projects.

### Key Patterns
- **Generic Repository Pattern**: Single `IRepository` interface works with any `CRMBaseEntity`
- **Dependency Injection**: All dependencies managed through built-in DI container
- **Data Annotations**: Entity configuration done directly on model classes
- **Automatic Audit Trail**: CreatedBy, LastChangedBy, CreatedDate, LastChangedDate handled automatically

## Technology Stack

- **.NET 8.0**
- **ASP.NET Core MVC** for web interface
- **Entity Framework Core 8.0** for data access
- **SQLite** for development database (configurable for SQL Server/PostgreSQL)
- **Bootstrap 5** for UI styling

## Database Configuration

Currently configured for SQLite with easy switching to other databases:

```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=rvnx-crm.db"
  }
}
```

### Supported Database Providers
- **SQLite** (current): Development/testing
- **SQL Server** (future): Production deployment
- **PostgreSQL** (future): Alternative production option

To switch databases:
1. Update `appsettings.json` DatabaseProvider
2. Add appropriate EF Core provider package
3. Create new migration

## Core Entities

### CRMBaseEntity
Abstract base class for all domain entities with built-in audit trail:

```csharp
[Key, Required] Guid Id
[Required, MaxLength(256)] string CreatedBy
[Required, MaxLength(256)] string LastChangedBy  
[Required] DateTime CreatedDate
[Required] DateTime LastChangedDate
```

### Person
CRM contact entity extending CRMBaseEntity:

```csharp
[Required, MaxLength(100)] string FirstName
[Required, MaxLength(100)] string LastName
[Required, MaxLength(256)] string Email (unique index)
[MaxLength(20)] string? PhoneNumber
[MaxLength(100)] string? JobTitle
[MaxLength(200)] string? Company
string FullName (computed property)
```

## Repository Usage

The generic repository provides type-safe CRUD operations:

```csharp
// Inject repository in controller
public HomeController(IRepository repository) { ... }

// Usage examples
var people = await _repository.ListAsync<Person>();
var person = await _repository.GetByIdAsync<Person>(id);
var count = await _repository.CountAsync<Person>();
await _repository.AddAsync(newPerson);
await _repository.UpdateAsync(person);
await _repository.DeleteAsync<Person>(id);
await _repository.SaveChangesAsync();
```

## Current Features

- **Person Management**: Basic CRUD operations for contacts
- **Audit Trail**: Automatic tracking of creation/modification
- **Responsive UI**: Bootstrap-styled person listing
- **Test Data Seeding**: Sample data generation for testing
- **Database Migrations**: EF Core migration system configured

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Database Setup

The project uses SQLite by default. The database file `rvnx-crm.db` will be created in the `Rvnx.CRM.Web` directory.

#### 1. Initial Setup (No existing database)

If you are setting up the project for the first time or do not have the `rvnx-crm.db` file:

```bash
# Apply migrations to create the database
dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```
This command creates the database file `rvnx-crm.db` in `Rvnx.CRM.Web/` and applies all existing migrations.

#### 2. Update Existing Database

If you already have a database and want to apply the latest changes (migrations):

```bash
# Update the database to the latest migration
dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```

#### 3. Reset Database (Start from scratch)

If you want to completely wipe the database and start over:

1.  Delete the `rvnx-crm.db` file located in the `Rvnx.CRM.Web/` directory.
2.  Run the update command again:

```bash
dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```

### Run the Application

```bash
dotnet run --project Rvnx.CRM.Web
```

## Test the Setup
1. Navigate to home page (usually `http://localhost:5000` or `https://localhost:5001`)
2. Click "Add Test Data" to populate sample people
3. View people listing with audit information

## Development Notes

### Package References
- **Core**: No external dependencies
- **Infrastructure**: EF Core + SQLite provider
- **Shared**: References Core
- **Web**: References all projects + EF Core Design tools

### Data Annotations Used
```csharp
[Key], [Required], [MaxLength(n)]
[Index(nameof(Email), IsUnique = true)]
```

### Audit Trail Implementation
- Automatically populated in `CRMDbContext.UpdateAuditFields()`
- Triggered on `SaveChanges()` and `SaveChangesAsync()`
- Currently uses "Environment.Username" placeholder for user identification

## Next Steps / TODOs

### Immediate
- [ ] Implement user authentication for proper audit trail
- [ ] Add Person CRUD operations (Create, Edit, Delete views)
- [ ] Add form validation and error handling

### Architecture Expansion  
- [ ] Add Company entity
- [ ] Add Deal/Opportunity entity
- [ ] Add Task/Activity tracking
- [ ] Implement relationships between entities

### Production Readiness
- [ ] Switch to SQL Server/PostgreSQL for production
- [ ] Add logging and error handling
- [ ] Implement search and filtering
- [ ] Add pagination for large datasets
- [ ] Add API controllers for mobile/SPA consumption

### Advanced Features
- [ ] Import/Export functionality
- [ ] Email integration
- [ ] Reporting and analytics
- [ ] Role-based security

## File Locations

### Key Configuration Files
- `appsettings.json`: Database configuration
- `Program.cs`: Dependency injection setup
- `ServiceCollectionExtensions.cs`: Infrastructure service registration

### Database Files
- `rvnx-crm.db`: SQLite database file (created automatically in `Rvnx.CRM.Web/`)
- `Migrations/`: EF Core migration files (in `Rvnx.CRM.Infrastructure/`)

## Database Schema

Current schema includes:
- **People** table with audit trail columns
- **Unique index** on Email column
- **GUID primary keys** for all entities

The generic repository pattern and base entity ensure consistent schema patterns for future entities.
