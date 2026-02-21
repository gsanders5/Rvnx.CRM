# Rvnx CRM Design Documentation

## Project Structure

```
.
├── Rvnx.CRM.Core/              # Domain layer (business entities, interfaces, core services)
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Enumerations/           # Domain enums
│   ├── Interfaces/             # Service and Repository interfaces
│   ├── Models/                 # Domain entities (organized by feature)
│   │   ├── Base/               # Base classes (BaseEntity, Person)
│   │   ├── Contact/            # Contact-related entities (Contact, ContactMethod)
│   │   ├── Dates/              # SignificantDate, Reminder
│   │   └── Business/           # Employer, etc.
│   ├── Services/               # Pure domain logic services (e.g. FileValidation)
│   └── Rvnx.CRM.Core.csproj
├── Rvnx.CRM.Infrastructure/    # Data access & Implementation layer
│   ├── Data/
│   │   └── CRMDbContext.cs     # EF Core DbContext
│   ├── Migrations/             # EF Core migrations
│   ├── Repositories/
│   │   └── Repository.cs       # Generic repository implementation
│   ├── Services/               # Infrastructure-dependent services (e.g. UserSync, VCard)
│   ├── ServiceCollectionExtensions.cs
│   └── Rvnx.CRM.Infrastructure.csproj
├── Rvnx.CRM.Shared/            # Shared components (Currently empty/reserved)
│   └── Rvnx.CRM.Shared.csproj
└── Rvnx.CRM.Web/               # Presentation layer
    ├── Controllers/            # MVC Controllers (Orchestration logic)
    ├── Services/               # UI-specific services (CurrentUserService)
    ├── Views/                  # Razor Views
    ├── Program.cs              # Composition Root and Service Registration
    └── Rvnx.CRM.Web.csproj
```

## Architecture

### Clean Architecture Principles (Actual)

- **Core**: Contains domain entities, interfaces, and pure logic. Depends on **Entity Framework Core** (for data annotations).
- **Infrastructure**: Implements Core interfaces, handles data access and external integrations. Depends on **Core** and **FolkerKinzel.VCards**.
- **Shared**: Currently unused (placeholder).
- **Web**: Presentation layer. Orchestrates user interactions. Depends on all other projects.

### Key Patterns

- **Generic Repository Pattern**: `IRepository` handles basic CRUD for `BaseEntity` types.
- **Polymorphic Relationships**: Entities like `Note`, `Reminder`, `Attachment`, `Pet`, `ContactMethod` are linked to parent entities via `EntityId` + `EntityType` (discriminator), often managed manually in controllers.
- **User Isolation**: `CRMDbContext` applies global query filters to restrict access to data based on the current user (`ICurrentUserService`).
- **Service Delegation**: Controllers delegate business logic to specialized services (e.g., `IContactManagementService`, `IContactImportService`).

## Technology Stack

- **.NET 8.0**
- **ASP.NET Core MVC** for web interface
- **Entity Framework Core 8.0** for data access
- **SQLite** for development database
- **MDB Bootstrap 5** (MDBootstrap) for UI styling
- **FolkerKinzel.VCards** for vCard import/export

## Core Entities

### BaseEntity

Abstract base class for all domain entities with built-in audit trail and ownership:

```csharp
[Key, Required] Guid Id
[Required, MaxLength(256)] string CreatedBy
[Required, MaxLength(256)] string LastChangedBy
[Required] DateTime CreatedDate
[Required] DateTime LastChangedDate
[MaxLength(450)] Guid? UserId // Owner for isolation
```

### Person

Abstract base class for contact entities:

```csharp
[Required, MaxLength(100)] string FirstName
[MaxLength(100)] string? LastName
string FullName (computed property)
// Has many [NotMapped] collections for related entities
```

### Contact

Concrete entity inheriting `Person`.

- Stores `Employers` via navigation property.
- Links to `ContactMethod`, `SignificantDate`, `Relationship` via polymorphic association.

## Repository Usage

The generic repository provides type-safe CRUD operations.
Special care is needed for polymorphic entities:

```csharp
// Fetch related entities manually
var notes = await _repository.ListAsync<Note>(n => n.EntityId == parentId && n.EntityType == EntityTypes.Person);
```

## Service Layer

- **Core Services**: Pure logic implementations (e.g., `FileValidationService`).
- **Infrastructure Services**: Implementations requiring external dependencies or DB access.
  - `UserSynchronizationService`
  - `VCardService`
  - `ContactImportService`
  - `ContactExportService`
  - `ContactManagementService`
  - `ContactReadService`
  - `SelfContactService`
- **Web Services**: Implementations requiring HTTP Context (e.g., `CurrentUserService`).

## Database Configuration

### Global Query Filters

- Automatically applied to all `BaseEntity` types in `CRMDbContext`.
- Filter: `e => e.UserId == _currentUserService.UserId`.
- Ensures users only see their own data.

### Audit Trail

- Automatically populated in `CRMDbContext.SaveChanges()`.
- Sets `CreatedBy`, `LastChangedBy`, `CreatedDate`, `LastChangedDate`.
- Sets `UserId` on creation if null.

## Known Deviations from Pure Architecture

1. **Core Dependency on EF Core**: Pragmatic choice to use Data Annotations (`[Key]`, `[Table]`) directly on domain models.
2. **Service Registration**: Split between `Program.cs` (Web) and `ServiceCollectionExtensions.cs` (Infrastructure).
