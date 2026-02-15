# Rvnx CRM System

**WARNING: This project is currently unstable. Updates may result in data loss.**

A modern CRM system built with ASP.NET Core 8.0 using clean architecture principles and Entity Framework Core.

For technical details, architecture, and design documentation, please see [DESIGN.md](DESIGN.md).

## Updating the Project

To update the project to the latest version and apply changes, use the following commands:

```bash
git reset --hard HEAD
git clean -fd
git pull
dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```

## Troubleshooting Migrations

If you encounter migration errors or need to regenerate the base migration, you can reset all migrations.
**Note: This will delete all existing migration history.**

1. Delete the `Migrations` folder in `Rvnx.CRM.Infrastructure`.
2. Delete the `rvnx-crm.db` file in `Rvnx.CRM.Web` (if it exists).
3. Run the following command to create a new initial migration:

```bash
dotnet ef migrations add InitialCreate --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```

4. Apply the migration to create the database:

```bash
dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web
```

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

### Run the Application

```bash
dotnet run --project Rvnx.CRM.Web
```

## Current Features

- **Person Management**: Basic CRUD operations for contacts
- **Audit Trail**: Automatic tracking of creation/modification
- **Responsive UI**: Bootstrap-styled person listing
- **Test Data Seeding**: Sample data generation for testing
- **Database Migrations**: EF Core migration system configured

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
