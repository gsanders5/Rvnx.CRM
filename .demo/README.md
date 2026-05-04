# `.demo/` — Anonymized demo data and documentation screenshots

This directory contains the assets used by the documentation screenshot pipeline.

## Contents

- `db/rvnx-crm-demo.db` — Anonymized SQLite database. All rows have `GroupId = NULL` so the global query filter (`e.GroupId == _currentUserService.GroupId`) matches when authentication is disabled. Seeded with literary characters (Sherlock Holmes / Watson, Dracula / Van Helsing, etc.) — no real PII.
- `screenshots/` — Generated images consumed by the root [`README.md`](../README.md). Run the docs-build pipeline to refresh.
- `appsettings.Local.json` — Reference settings for browsing the demo DB manually (copy into `Rvnx.CRM.Web/` and `dotnet run --project Rvnx.CRM.Web`). The docs-build harness does not read this file — it passes equivalent overrides as CLI args instead.

## Refreshing screenshots

```bash
dotnet run --project Rvnx.CRM.DocsBuild
```

The harness boots `Rvnx.CRM.Web` against a *temporary* copy of `db/rvnx-crm-demo.db` (so EF migrations do not mutate the source), drives Microsoft.Playwright through each `<!-- SCREENSHOT: ... -->` directive in [`README.md`](../README.md), and writes captured PNGs back to `screenshots/`.

## Anonymization notes

The demo DB was prepared with:

- `Users.Email`, `Users.SubjectId`, `Users.DisplayName` replaced with placeholders (`demo@example.com` / `demo-subject` / `Demo User`).
- `UserGroups.Name` set to `Demo Group`.
- All `ApiTokens` rows deleted (never ship token hashes, even though they are not reversible).
- All `ContactFavorite` rows deleted (per-user state has no meaning in single-user mode).
- `GroupId` and `UserId` columns set to `NULL` on every BaseEntity table.

If new tables are added that contain PII, repeat the anonymization steps above before re-snapshotting the demo DB.
