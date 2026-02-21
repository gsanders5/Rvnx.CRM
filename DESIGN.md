# Rvnx CRM Design Documentation

> **Cartographer Note (Updated Feb 2026):** This document has been updated to reflect the _actual_ state of the architecture, as opposed to the purely idealized state. Several deviations from strict Clean Architecture have been documented below, specifically regarding service placement and controller responsibilities.

## Project Structure

```
.
├── Rvnx.CRM.Core/              # Domain layer (business entities, interfaces, core services)
│   ├── DTOs/                   # Data Transfer Objects (organized by scope: Business, Common, Contact, Dashboard, Pet)
│   ├── Enumerations/           # Domain enums
│   ├── Interfaces/             # Service and Repository interfaces
│   ├── Models/                 # Domain entities (organized by feature)
│   │   ├── Base/               # Base classes (BaseEntity, Person)
│   │   ├── Contact/            # Contact-related entities (Contact, ContactMethod)
│   │   ├── Dates/              # SignificantDate, Reminder
│   │   └── Business/           # Employer, etc.
│   ├── Services/               # Pure domain logic services (e.g. FileValidation, DateCalculation)
│   └── Rvnx.CRM.Core.csproj
├── Rvnx.CRM.Infrastructure/    # Data access & Implementation layer
│   ├── Data/
│   │   └── CRMDbContext.cs     # EF Core DbContext
│   ├── Migrations/             # EF Core migrations
│   ├── Repositories/
│   │   └── Repository.cs       # Generic repository implementation
│   ├── Services/               # Infrastructure services and (currently) Domain Orchestration services
│   ├── ServiceCollectionExtensions.cs
│   └── Rvnx.CRM.Infrastructure.csproj
├── Rvnx.CRM.Shared/            # Shared components (Currently empty/reserved)
│   └── Rvnx.CRM.Shared.csproj
├── Rvnx.CRM.Tests/             # Unit and integration tests (Flat structure)
│   └── Rvnx.CRM.Tests.csproj
└── Rvnx.CRM.Web/               # Presentation layer
    ├── Controllers/            # MVC Controllers (Orchestration and some business logic)
    ├── Services/               # UI-specific services (CurrentUserService)
    ├── Views/                  # Razor Views
    ├── Program.cs              # Composition Root and Service Registration
    └── Rvnx.CRM.Web.csproj
```

## Architecture

### Clean Architecture Principles (Actual)

- **Core**: Contains domain entities, interfaces, and pure logic. Depends on **Entity Framework Core** (for data annotations).
- **Infrastructure**: Implements Core interfaces, handles data access and external integrations. Depends on **Core** and **FolkerKinzel.VCards**.
  - _Current Deviation:_ Several business orchestration services (e.g., `ContactManagementService`, `DashboardService`) are currently located here instead of Core.
- **Shared**: Currently empty. Intended for cross-cutting types.
- **Web**: Presentation layer. Orchestrates user interactions. Depends on all other projects.
  - _Current Deviation:_ Controllers frequently bypass application services and interact directly with `IRepository` for CRUD operations.

### Key Patterns

- **Generic Repository Pattern**: `IRepository` handles basic CRUD for `BaseEntity` types.
- **Polymorphic Relationships**: Entities like `Note`, `Reminder`, `Attachment`, `Pet`, `ContactMethod` are linked to parent entities via `EntityId` + `EntityType` (discriminator). This mapping logic is currently handled manually within Web controllers (e.g., `RelationshipsController`).
- **User Isolation**: `CRMDbContext` applies global query filters to restrict access to data based on the current user (`ICurrentUserService`).
- **Service Delegation**: _Intended_ pattern is for controllers to delegate business logic to specialized services. In reality, this is mixed; some controllers delegate to services (like `ContactImportService`), while others handle logic and data access directly.

## Technology Stack

- **.NET 8.0**
- **ASP.NET Core MVC** for web interface
- **Entity Framework Core 8.0** for data access
- **SQLite / SQL Server** for database
- **MDB Bootstrap 5** for UI styling
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
Special care is needed for polymorphic entities. Currently, controllers execute this manually:

```csharp
// Fetch related entities manually
var notes = await _repository.ListAsync<Note>(n => n.EntityId == parentId && n.EntityType == EntityTypes.Person);
```

## Service Layer

- **Core Services**: Pure logic implementations (e.g., `FileValidationService`, `DateCalculationService`, `RelationshipTypeService`).
- **Infrastructure Services**:
  - _Genuine Infrastructure:_ Implementations requiring external dependencies (`UserSynchronizationService`, `VCardService`).
  - _Domain Orchestrators (Misplaced):_ Services that orchestrate business logic using `IRepository` but don't strictly require infrastructure dependencies (`ContactImportService`, `ContactExportService`, `ContactManagementService`, `ContactReadService`, `SelfContactService`, `DashboardService`).
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

## Known Deviations & Limitations

1. **Core Dependency on EF Core**: Pragmatic choice to use Data Annotations (`[Key]`, `[Table]`) directly on domain models.
2. **Service Registration**: Split between `Program.cs` (Web) and `ServiceCollectionExtensions.cs` (Infrastructure).
3. **Thick Controllers / Repository Leakage**: Controllers frequently inherit from `RepositoryController` and use `_repository` directly to perform data access and entity mapping, bypassing the service layer.
4. **Service Placement**: Many services that orchestrate domain logic (depending purely on `IRepository`) are placed in Infrastructure rather than Core.
5. **Testing Structure**: The `Rvnx.CRM.Tests` project does not currently mirror the project structure of the software, making it difficult to navigate coverage. Inconsistent use of logging across Web layers.
