# Rvnx CRM

A self-hosted personal CRM — keep track of the people in your life without giving your data to anyone else.

Built with ASP.NET Core and SQLite. No subscriptions, no cloud sync, no third-party services required.

![Demo](.screenshots/demo.gif)

---

## Features

### Contacts
- Full contact profiles: name, company, job title, pronouns, gender, birthday, religion
- Profile photos, labels, and favorites
- Hide contacts without deleting them
- Merge duplicates
- Import via vCard (.vcf)
- Export to **CSV** (round-trippable — a future CSV import will reuse the same schema), single **vCard** (.vcf), or a **bulk vCard zip** of all contacts
- **Partial Contacts** — lightweight placeholders for people in relationships who don't need a full profile yet

### Per-Contact Detail Panels

![Contact Detail](.screenshots/contact-detail.png)

Each contact has dedicated sections for:
- **Contact Info** — emails, phone numbers (normalized to E.164 on save, formatted for display), websites
- **Addresses** — structured postal addresses with type
- **Quick Facts** — freeform key/value pairs for memorable details
- **Relationships** — family, friends, colleagues; links to full or partial contacts
- **Pets** — companion animals with support for multiple owners
- **Important Dates** — birthdays, anniversaries, and custom milestones with recurrence
- **Notes** — long-form Markdown notes
- **Activities** — logged meetings, calls, and events; activities can link to multiple contacts at once, with a one-click **QuickLog** for fast entry
- **Tasks / Follow-ups** — per-contact to-do items with due dates
- **Attachments** — photos, documents, or any file
- **Social Media** — linked social accounts

### Calendar

![Calendar](.screenshots/calendar.png)

Monthly calendar view of upcoming significant dates and incomplete tasks, with list view for compact browsing.

### Network Graph
Interactive visualization of contact relationships on the dashboard. Node color reflects gender (blue = male, pink = female, purple = non-binary, grey = unset).

### Organization
- **Labels** — create and apply custom labels to any contact
- **Favorites** — star contacts for quick access

### REST API

![Swagger UI](.screenshots/swagger.png)

Full API coverage for all resources: contacts, activities, addresses, tasks, favorites, labels, notes, facts, significant dates, pets, attachments, relationships, and calendar events.

- Bearer token authentication (`crm_` prefix tokens, created via the console app)
- String-based enums — all enum fields accept human-readable strings (`"Annual"`, `"Forward"`)
- Swagger/OpenAPI docs at `/swagger`
- Partial update support via JSON Merge Patch (`PATCH`) on all resources
- **iCal subscription feed** at `/api/calendar/feed.ics?token=<api-token>` — subscribe from Google / Apple / Outlook Calendar to see significant dates and tasks. Token is passed as a query parameter so calendar clients that don't support custom headers can subscribe.

### Authentication
- OpenID Connect (OIDC) — tested with [Authentik](https://goauthentik.io/)
- Per-user data isolation; users only see their own contacts

---

## Setup

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download)

### 1. Configure each project

Each runnable project (`Web`, `API`, `ConsoleApp`) reads an `appsettings.Local.json` that is not committed to source control. Create one in each project directory you plan to run.

**Minimum — no authentication:**
```json
{
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/absolute/path/to/rvnx-crm.db"
  }
}
```

**With OIDC authentication (Web only):**
```json
{
  "Authentication": {
    "Enabled": true,
    "Authority": "https://your-sso-provider/application/o/your-app/",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc",
    "ResponseType": "code",
    "Scopes": "openid profile email"
  },
  "DatabaseProvider": "SQLite",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=/absolute/path/to/rvnx-crm.db"
  }
}
```

> Use an absolute path in the connection string.

### 2. Create the database

```bash
cd Rvnx.CRM.ConsoleApp
dotnet run -- COUNT-CONTACTS
```

This auto-applies EF Core migrations and prints the contact count (0 on a fresh database).

### 3. Run the web app

```bash
dotnet run --project Rvnx.CRM.Web
# → http://localhost:5215
```

Log in via your OIDC provider, or browse freely if auth is disabled. Your account is created on first login.

### 4. Create an API token (optional)

```bash
cd Rvnx.CRM.ConsoleApp
dotnet run -- ADD-API-TOKEN your@email.com my-token-name
# Prints: Raw Token: crm_xxxxxxxxxxxx  (shown once — save it)
```

### 5. Run the API (optional)

```bash
dotnet run --project Rvnx.CRM.API
# → http://localhost:5212
# Swagger UI → http://localhost:5212/swagger
```

```bash
curl -H "Authorization: Bearer crm_xxxxxxxxxxxx" http://localhost:5212/api/contacts
```

---

## Console Commands

| Command | Description |
|---|---|
| `COUNT-CONTACTS` | Print count of non-partial contacts |
| `SEND-DATE-REMINDERS` | Send email reminders for upcoming significant dates |
| `LIST-USERS` | List all users |
| `PROMOTE-USER <email>` | Grant administrator rights |
| `DEMOTE-USER <email>` | Revoke administrator rights |
| `ADD-API-TOKEN <email> <name>` | Create a new API token |
| `REVOKE-API-TOKEN <email> <name>` | Revoke an API token by name |
| `MERGE-USERS <email1> <email2> [--confirm]` | Merge two user accounts |

---

## Running Tests

```bash
dotnet test
```

---

For architecture and technical design details, see [DESIGN.md](DESIGN.md).

## License

Source available under a custom license. Non-commercial use, personal projects, and self-hosting are free. Commercial use requires a separate written agreement. See [LICENSE](LICENSE) and [THIRD-PARTY-LICENSES.md](THIRD-PARTY-LICENSES.md) for details.
