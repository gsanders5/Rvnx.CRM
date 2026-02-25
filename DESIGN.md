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
- **Web**: Presentation layer. Orchestrates user interactions. Depends on all other projects.

### Key Patterns

- **Generic Repository Pattern**: `IRepository` handles basic CRUD for `BaseEntity` types.
- **Explicit Foreign Keys**: Entities previously using polymorphic associations (`Note`, `Reminder`, `Attachment`, `ContactMethod`, etc.) now use typed nullable foreign keys (e.g., `ContactId`, `CompanyId`) with check constraints to enforce single ownership. `Pet` uses a required `ContactId`.
- **User Isolation**: `CRMDbContext` applies global query filters to restrict access to data based on the current user (`ICurrentUserService`).
- **Service Delegation**: Controllers delegate business logic to specialized services (e.g., `IContactManagementService`, `IContactImportService`).

### Data Loading (The "Stitching" Pattern)

For complex views (like the Contact Index), the application avoids complex SQL joins or massive Cartesian products. Instead, it uses a "stitching" pattern:

1.  Fetch the main entities (e.g., `Contact`) based on the filter.
2.  Extract the list of IDs.
3.  Execute separate, efficient queries to fetch related lightweight data (e.g., `Attachment` for profile images, `ContactLabel` for tags) using `Contains` on the ID list.
4.  Map the related data to the DTOs in memory using Dictionaries.

This is implemented in `ContactReadService.GetIndexDataAsync`.

### Partial Contacts

The system supports "Partial Contacts" — contacts that exist primarily as a name in a relationship (e.g., "John's Wife") but don't have a full profile yet.

-   **Implementation**: They are standard `Contact` entities in the database.
-   **Differentiation**: Defined by the `IsPartial` boolean flag on the `Contact` entity.
-   **Relationships**: `Relationship.RelatedEntityId` points to the partial contact's ID just like any other contact.
-   **Promotion**: A partial contact can be "promoted" to a full contact, which simply toggles the `IsPartial` flag to `false`.

### Relationship Direction

Relationships are polymorphic but managed via `RelationshipService`.

-   **Storage**: Stored as `EntityId` (Source) -> `RelatedEntityId` (Target).
-   **Selection**: The UI sends a string like `{TypeId}_Fwd` or `{TypeId}_Rev`.
-   **Parsing**: `RelationshipService` parses this string. if `Rev` (Reverse) is selected, the service swaps the `EntityId` and `RelatedEntityId` before saving, ensuring the relationship is stored in the semantic direction intended by the user.

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

The `[NotMapped]` collections on `Person` (like `Relationships`, `RelatedTo`) are populated manually by services (e.g., `ContactReadService`) when needed for display, rather than being managed automatically by EF Core navigation properties.

### Contact

Concrete entity inheriting `Person`.

- Stores `Employers` via navigation property.
- Links to `ContactMethod`, `SignificantDate`, `Pet`, `Note`, etc. via standard EF Core navigation properties with Cascade Delete.
- Contains the `IsPartial` flag.

### Relationship

Polymorphic entity linking two entities.

-   **Inheritance**: Inherits `PolymorphicEntity` (`EntityId`, `EntityType`).
-   **Target**: `RelatedEntityId` (Points to the target entity, usually another `Contact`).
-   **Navigation**: `Person` and `RelatedPerson` are `[NotMapped]` and populated manually by `ContactReadService`.

## Repository Usage

The generic repository provides type-safe CRUD operations.
Data loading is now simplified with standard Include support:

```csharp
// Fetch related entities via Include or typed FK
var contacts = await _repository.ListAsync<Contact>(c => c.Id == id, default, nameof(Contact.Notes));
var notes = await _repository.ListAsync<Note>(n => n.ContactId == parentId);
```

## Service Layer

- **Core Services**: Pure logic implementations and domain orchestrations.
  - `FileValidationService`
  - `ContactManagementService`
  - `ContactReadService`
  - `SelfContactService`
  - `DashboardService`
  - `RelationshipService`
- **Infrastructure Services**: Implementations requiring external dependencies or DB access.
  - `UserSynchronizationService`
  - `VCardService`
  - `ContactImportService`
  - `ContactExportService`
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
2. **Service Registration**: Now utilizing `ServiceCollectionExtensions` across `Core` and `Infrastructure` layers to let each layer manage its own registrations.
