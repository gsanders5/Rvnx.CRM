# Rvnx CRM Design Documentation

## Project Structure
```
.
├── Rvnx.CRM.Core/              # Domain layer (business entities, interfaces, core services)
│   ├── DTOs/                   # Data Transfer Objects
│   ├── Enumerations/           # Domain enums
│   ├── Helpers/                # Static helpers (e.g. PhoneNumberNormalizer)
│   ├── Interfaces/             # Service and Repository interfaces
│   ├── Models/                 # Domain entities (organized by feature)
│   │   ├── Base/               # Base classes (BaseEntity, Person)
│   │   ├── Contact/            # Contact-related entities (Contact, ContactMethod, Relationship, Address, ContactTask, Pet)
│   │   ├── Activity/           # Activity entity
│   │   ├── Dates/              # SignificantDate, Reminder
│   │   └── Business/           # Employer, Attachment, Note
│   ├── Services/               # Pure domain logic services (e.g. FileValidation, DateCalculation)
│   ├── Validation/             # Custom ValidationAttributes (e.g. PhoneNumberAttribute)
│   └── Rvnx.CRM.Core.csproj
├── Rvnx.CRM.Infrastructure/    # Data access & Implementation layer
│   ├── Data/
│   │   └── CRMDbContext.cs     # EF Core DbContext
│   ├── Migrations/             # EF Core migrations (SQLite)
│   ├── Repositories/
│   │   └── Repository.cs       # Generic repository implementation
│   ├── Services/               # Infrastructure-dependent services (e.g. UserSync, VCard)
│   ├── ServiceCollectionExtensions.cs
│   └── Rvnx.CRM.Infrastructure.csproj
├── Rvnx.CRM.API/               # REST API layer
│   ├── Authentication/         # API Token Auth handlers
│   ├── Controllers/            # API Controllers
│   ├── Services/               # API specific services (ApiTokenCurrentUserService)
│   ├── Program.cs              # Composition Root and Swagger config
│   └── Rvnx.CRM.API.csproj
├── Rvnx.CRM.Web/               # Presentation layer
│   ├── Controllers/            # MVC Controllers (Orchestration logic)
│   ├── Services/               # UI-specific services (CurrentUserService)
│   ├── Views/                  # Razor Views
│   ├── Program.cs              # Composition Root and Service Registration
│   └── Rvnx.CRM.Web.csproj
├── Rvnx.CRM.ConsoleApp/        # Console application for scheduled tasks
│   ├── Program.cs              # Entry point
│   ├── AppHost.cs              # DI container builder
│   ├── TaskManager.cs          # Task routing and execution
│   ├── ConsoleUserService.cs   # ICurrentUserService for console context
│   └── Rvnx.CRM.ConsoleApp.csproj
└── Rvnx.CRM.Tests/             # Unit and integration tests
```

## Architecture

### Clean Architecture Principles (Actual)

- **Core**: Contains domain entities, interfaces, and pure logic. Depends on **Entity Framework Core** (for data annotations).
- **Infrastructure**: Implements Core interfaces, handles data access and external integrations. Depends on **Core** and **FolkerKinzel.VCards**.
- **API**: RESTful endpoints. Uses API token-based authentication. Depends on all other projects.
- **Web**: Presentation layer. Orchestrates user interactions. Depends on all other projects.
- **ConsoleApp**: Background task runner. Shares Core and Infrastructure with Web. Designed for cron / Task Scheduler.

### Key Patterns

- **Generic Repository Pattern**: `IRepository` handles basic CRUD for `BaseEntity` types.
- **Explicit Foreign Keys**: Entities previously using polymorphic associations (`Note`, `Reminder`, `Attachment`, `ContactMethod`, etc.) now use typed nullable foreign keys (e.g., `ContactId`) with check constraints to enforce ownership.
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

Relationships are polymorphic but primarily link Person to Person.

-   **Storage**: Stored as `EntityId` (Source) -> `RelatedEntityId` (Target).
-   **API**: The `CreateRelationshipRequest` DTO accepts a typed `RelationshipDirection` enum (`Forward` or `Reverse`) alongside a `RelationshipTypeId` GUID. The API controller converts this to the internal `{TypeId}_Fwd` / `{TypeId}_Rev` string before passing it to the service.
-   **Web UI**: The UI sends the combined string directly as a `<select>` value.
-   **Parsing**: `RelationshipService` parses the combined string. If `Rev` (Reverse) is selected, the service swaps `EntityId` and `RelatedEntityId` before saving, ensuring the relationship is stored in the semantic direction intended by the caller.

## Console Application

The `Rvnx.CRM.ConsoleApp` project provides a command-line interface for running scheduled tasks via cron / Task Scheduler.

### Architecture

The console app follows a three-class pattern:

| File | Responsibility |
|------|----------------|
| `Program.cs` | Entry point with try/catch/finally error handling |
| `AppHost.cs` | Builds the DI container, registers services, runs migrations |
| `TaskManager.cs` | Parses arguments, routes to task methods, handles logging |

### Service Registration

The console app reuses `AddCoreServices()` and `AddInfrastructure()` from the shared projects. It provides its own `ICurrentUserService` implementation:
```csharp
internal sealed class ConsoleUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public Guid? GroupId => null;
    public string? UserName => "System";
    public bool IsAuthenticated => false;
}
```

### Bypassing Query Filters

Since `ConsoleUserService` returns `null` for `GroupId`, tasks that need to access all data across user groups should use `IgnoreQueryFilters()`:
```csharp
int count = await context.Contacts
    .IgnoreQueryFilters()
    .CountAsync(c => !c.IsPartial);
```

### Adding New Tasks

1. Add a case to the switch in `TaskManager.ExecuteTaskAsync`:
```csharp
bool success = taskName switch
{
    "COUNT-CONTACTS" => await RunCountContactsAsync(services),
    "NEW-TASK" => await RunNewTaskAsync(services),
    _ => false
};
```

2. Add the task method:
```csharp
private static async Task<bool> RunNewTaskAsync(IServiceProvider services)
{
    var myService = services.GetRequiredService<IMyService>();
    await myService.DoWorkAsync();
    return true;
}
```

3. Schedule in cron or Task Scheduler with the task name as the argument.

## API Application

The `Rvnx.CRM.API` project provides RESTful endpoints to interact with the CRM system programmatically.

### Architecture

The API application delegates logic to the shared `Core` services via DI, just like the `Web` and `ConsoleApp` projects.

### Authentication & Authorization

- **API Token Authentication**: The API is protected using bearer tokens. It uses a custom `ApiTokenAuthenticationHandler` for the `"Bearer"` scheme.
- **User Context**: It resolves the current user context using `ApiTokenCurrentUserService`, which implements `ICurrentUserService` by extracting the user ID from the validated API token.
- **Token Delivery**: Normal endpoints accept the token via the `Authorization: Bearer crm_…` header; the iCal feed endpoint additionally accepts `?token=crm_…` as a query parameter so calendar clients without custom-header support can subscribe.

### Endpoints

| Controller | Endpoints |
|---|---|
| Contacts | List, Get, Create, Update, Delete, SetPhoto, UnsetPhoto |
| Activities | ListByContact, Create, Update, Delete |
| Addresses | ListByContact, Create, Update, Delete |
| Attachments | ListByContact, Upload, Download, Thumbnail, Delete |
| Calendar | Events (significant dates + tasks, fetched concurrently) |
| CalendarFeed | `GET feed.ics` (query-param token, returns RFC 5545 VCALENDAR) |
| ContactMethods | ListByContact, Create, Update, Delete |
| ContactTasks | ListByContact, Create, Update, Delete, ToggleComplete |
| Facts | ListByContact, Create, Update, Delete |
| Favorites | List, Toggle |
| Labels | List, Create, Update, Delete, Associate, Disassociate |
| Merge | Merge |
| Notes | ListByContact, Create, Update, Delete |
| Pets | ListByContact, Create, Update, Delete |
| Relationships | ListByContact, Create, Update, Delete |
| SignificantDates | ListByContact, Create, Update, Delete |

### Documentation

The API uses Swagger/OpenAPI to document its endpoints and available operations.

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
Guid? GroupId
```

### Person

Abstract base class for contact entities:
```csharp
[Required, MaxLength(100)] string FirstName
[MaxLength(100)] string? LastName
[MaxLength(100)] string? Nickname
[MaxLength(100)] string? JobTitle
[MaxLength(200)] string? Company
string FullName (computed property)
bool IsHidden
Guid? ProfileImageId [NotMapped]
// Has many collections for related entities (some virtual, some [NotMapped])
```

The `[NotMapped]` collections on `Person` (like `Relationships`, `RelatedTo`) are populated manually by services (e.g., `ContactReadService`) when needed for display, rather than being managed automatically by EF Core navigation properties.

### Contact

Concrete entity inheriting `Person`.

- **IsPartial**: Boolean flag indicating if the contact is a full profile or a placeholder.
- **Pronouns**: String (max 100).
- **Gender**: String (max 100).
- **Religion**: String (max 100).
- **Pets**: Managed via `Pet` entity with `ContactId` FK.
- **Employers**: Managed via `Employer` entity with `EmployeeId` FK.
- **Labels**: Managed via `ContactLabel` join entity.
- **Addresses**: Managed via `Address` entity with `ContactId` FK. Supports Line1, Line2, City, State, Zip, Country, AddressType.
- **ContactTasks**: Managed via `ContactTask` entity with `ContactId` FK. Supports Title, Description (Markdown), DueDate, IsCompleted, CompletedDate.
- **Activities**: Managed via `Activity` entity. Supports many-to-many contact association, Title, Description (Markdown), ActivityDate, ActivityType, Location.

### Relationship

Polymorphic entity linking two entities.

-   **Inheritance**: Inherits `PolymorphicEntity` (`EntityId`, `EntityType`).
-   **Target**: `RelatedEntityId` (Points to the target entity, usually another `Contact`).
-   **Navigation**: `Person` and `RelatedPerson` are `[NotMapped]` and populated manually by `ContactReadService`.

### Attachment & Content

Attachments are split into two tables to optimize performance (loading metadata without heavy blobs).

-   **Attachment**: Stores metadata (`FileName`, `ContentType`, `AttachmentType`) and links to a `ContactId`.
-   **AttachmentContent**: Stores the actual file bytes (`byte[] Content`) and links back to `Attachment` via 1:1 relationship (`AttachmentId`).

## Service Layer

- **Core Services**: Pure logic implementations and domain orchestrations.
  - `DateCalculationService`: Handles recurrence logic (e.g., birthdays, reminders), explicitly handling leap years and calendar vs. timespan frequency. Provides `GetNextOccurrence` and `GetCurrentYearOccurrence` for calendar event generation.
  - `FileValidationService`: Validates file signatures (magic numbers) and extensions for uploads.
  - `ContactManagementService`: Handles CUD operations for contacts, including cascading deletes and orphan cleanup for partial contacts.
  - `RelationshipService`: Manages creation, direction parsing, and promotion of partial contacts.

- **Infrastructure Services**: Implementations requiring external dependencies or DB access.
  - `UserSynchronizationService`: Syncs OIDC user claims to the local `User` table.
  - `VCardService`: Uses `FolkerKinzel.VCards` to parse and generate VCF files.
  - `ContactTaskService`: CRUD for per-contact tasks/follow-ups with completion toggling and calendar event generation.
  - `ActivityService`: CRUD for activities with multi-contact association.
  - `AddressService`: CRUD for contact addresses.
  - `FavoriteService`: Toggle and query favorite contacts.
  - `SignificantDateService`: Manages significant dates and generates calendar events for both current-year and next-year occurrences.
  - `CalendarFeedService`: Generates an RFC 5545 iCalendar (.ics) feed aggregating significant dates and incomplete tasks; used by the subscribable calendar endpoint. Uses Ical.Net and builds deterministic per-event UIDs (`{type}-{contactId}-{yyyyMMdd}-{titleHash}@rvnx-crm`) so subscribed clients dedupe on refresh while still keeping multiple same-day events distinct.
  - `CsvExportService`: Exports all contacts (plus flattened emails, phones, first address, and birthday) as an RFC 4180 CSV. Column definitions are exposed as a reusable list so a future CSV-import can map the same headers in reverse.
  - `ReminderNotificationService`: Run from the console app; scans active `ReminderOffset`s, computes the next occurrence via `DateCalculationService`, and emails the owning group's users via MailKit SMTP (StartTls) when the offset fires on `forDate`. Writes a `ReminderLog` row per occurrence so successful sends are not duplicated on re-runs, and caches group recipients per invocation to avoid N+1 user lookups.
  - `ThumbnailService`: On-demand JPEG thumbnail generator for image attachments using ImageSharp (`ResizeMode.Max`, quality 75). Clamps requested dimensions, falls back to a 200px default, and caches results in `IMemoryCache` keyed by `(AttachmentId, MaxWidth, MaxHeight)` for 24 hours. Returns `null` for non-image content types or on decode failure (logged as a warning).
  - `MergeService`: Merges a secondary contact into a primary. Scalar fields fall back to the primary's value (or the secondary's if the primary is blank). Child records (attachments, notes, contact methods, significant dates, facts, relationships, pets) are reassigned to the primary, de-duplicated by natural key, and orphans are deleted. Profile-photo conflicts are resolved by downgrading the secondary's profile photos to general attachments. The whole operation runs in a single `CRMDbContext` transaction when the provider is relational.
  - `DebugDataService`: Populates and clears sample data for local/debug environments. `SeedTestDataAsync` uses `FakeDataGenerator` to create contacts plus related addresses, contact methods, and significant dates. `ResetDatabaseAsync` clears all contacts and their dependent entities. `AddRandomRelationshipsAsync` wires up random relationships between existing contacts (skipping self-links and duplicates).

- **Constants**:
  - `CalendarColors`: Centralized color constants for calendar event types (Birthday, SignificantDate, Task).

- **Helpers**:
  - `PhoneNumberNormalizer` (`Rvnx.CRM.Core/Helpers/`): Wraps Google's libphonenumber (`PhoneNumbers` package). `TryNormalize` parses a user-entered number and returns the canonical E.164 form (with `;ext=` suffix when an extension is present), falling back to an error message on invalid input. `NormalizeOrThrow` is the write-path helper (only normalizes `ContactMethodType.Phone`; throws `ValidationException` on failure). `FormatForDisplay` renders stored E.164 values in national format when the country code matches `DefaultRegion` ("US"), otherwise international, and `FormatForTelUri` produces an RFC 3966 `tel:` URI.
  - `PhoneNumberAttribute` (`Rvnx.CRM.Core/Validation/`): `ValidationAttribute` that defers to `PhoneNumberNormalizer.TryNormalize` so DTOs and view models enforce phone validity with the same rules used on write. Returns the shared `InvalidPhoneMessage` and attaches the member name to the validation result so clients can surface field-level errors.

## Database Configuration

### Global Query Filters

- Automatically applied to all `BaseEntity` types in `CRMDbContext`.
- Filter: `e => e.GroupId == _currentUserService.GroupId`.
- Ensures users only see their own data (or data belonging to their group).
- Console app can bypass with `IgnoreQueryFilters()` for cross-tenant operations.

### Audit Trail

- Automatically populated in `CRMDbContext.SaveChanges()`.
- Sets `CreatedBy`, `LastChangedBy`, `CreatedDate`, `LastChangedDate`.
- Sets `UserId` and `GroupId` on creation if null.
- Console app uses "System" as the username.

## Known Deviations from Pure Architecture

1. **Core Dependency on EF Core**: Pragmatic choice to use Data Annotations (`[Key]`, `[Table]`) directly on domain models.
2. **Service Registration**: Now utilizing `ServiceCollectionExtensions` across `Core` and `Infrastructure` layers to let each layer manage its own registrations.