# Rvnx CRM System

**WARNING: This project is currently unstable. Updates may result in data loss.**

A modern CRM system built with ASP.NET Core 8.0 using clean architecture principles and Entity Framework Core.

For technical details, architecture, and design documentation, please see [DESIGN.md](DESIGN.md).

## Features

- **Person Management**: Full CRUD operations for contacts, including profile images.
- **Relationships**: Polymorphic relationship system supporting partial contacts (names without full profiles).
- **Import/Export**: vCard (.vcf) import and export functionality.
- **Authentication**: User isolation (multi-tenancy) via `ICurrentUserService` and global query filters.
- **Labels**: Categorize contacts with custom labels.
- **Attachments**: Upload and manage files related to contacts.
- **Notes & Reminders**: Add notes and set reminders for contacts.
- **Significant Dates**: Track birthdays and other important dates.
- **Self Contact**: Manage your own profile as a contact within the system.

## Project Structure

- **Rvnx.CRM.Core**: Domain layer containing entities, interfaces, DTOs, and pure business logic services.
- **Rvnx.CRM.Infrastructure**: Data access layer with EF Core DbContext, migrations, and repository implementations.
- **Rvnx.CRM.Web**: ASP.NET Core MVC presentation layer, controllers, and views.
- **Rvnx.CRM.Tests**: Unit and integration tests.

## Setup Instructions

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or VS Code

### Database Setup

The project is configured to use SQLite by default for development.

#### 1. Initial Setup

If you are setting up the project for the first time:

```bash
# Apply migrations to create the database
dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```
This command creates the database file `rvnx-crm.db` in `Rvnx.CRM.Web/` and applies all existing migrations.

### Run the Application

```bash
dotnet run --project Rvnx.CRM.Web
```

## Running Tests

To run the test suite:

```bash
dotnet test
```
