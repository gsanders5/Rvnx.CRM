# Rvnx CRM System

**WARNING: This project is currently unstable. Updates may result in data loss.**

A modern CRM system built with **ASP.NET Core 8.0** using clean architecture principles and **Entity Framework Core 9**.

For technical details, architecture, and design documentation, please see [DESIGN.md](DESIGN.md).

## Features

- **Person Management**: Full CRUD operations for contacts, including profile images (stored as `AttachmentContent`).
- **Relationships**: Polymorphic relationship system supporting **Partial Contacts** (contacts that exist primarily as names without full profiles).
- **Import/Export**: vCard (.vcf) import and export functionality via `FolkerKinzel.VCards`.
- **Authentication & Isolation**: Supports OpenID Connect authentication. Implements user isolation (multi-tenancy) via `ICurrentUserService` and EF Core global query filters.
- **Labels**: Categorize contacts with custom labels.
- **Attachments**: Upload and manage files related to contacts.
- **Notes & Reminders**: Add notes and set reminders for contacts with recurring date logic.
- **Significant Dates**: Track birthdays and other important dates with calendar-aware recurrence.
- **Self Contact**: Manage your own profile as a contact within the system.

## Project Structure

- **Rvnx.CRM.Core**: Domain layer containing entities, interfaces, DTOs, and pure business logic services (e.g., `DateCalculationService`, `FileValidationService`).
- **Rvnx.CRM.Infrastructure**: Data access layer with EF Core DbContext, migrations, and repository implementations.
- **Rvnx.CRM.Web**: ASP.NET Core MVC presentation layer, controllers, views, and service registration.
- **Rvnx.CRM.Tests**: Unit and integration tests (using `xUnit`, `Moq`, `Microsoft.AspNetCore.Mvc.Testing`, and `Sqlite` integration tests).

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Database Setup

The project is configured to use **SQLite** by default for development.

#### 1. Initial Setup

If you are setting up the project for the first time:

```bash
# Apply migrations to create the database
dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```
This command creates the database file `rvnx-crm.db` in `Rvnx.CRM.Web/` and applies all existing migrations.

### Configuration

Configuration is handled in `appsettings.json`. By default, authentication is disabled (`"Enabled": false`). To enable OIDC authentication, configure the `Authentication` section with your provider details (Authority, ClientId, ClientSecret).

### Run the Application

```bash
dotnet run --project Rvnx.CRM.Web
```

## Running Tests

To run the full test suite (Unit and Integration):

```bash
dotnet test
```
