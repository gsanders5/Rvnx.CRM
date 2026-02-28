# DATE-MIGRATION-CHECKLIST.md

## Design Summary
Three-layer separation: **SignificantDate** (what/when) → **ReminderOffset** (notify N days before) → **ReminderLog** (delivery record per occurrence).

---

## Phase 1: Teardown
- [ ] Delete any existing `Reminder.cs` entity under `Rvnx.CRM.Core/Models/`
- [ ] Delete any `RemindableEntity.cs` base class under `Rvnx.CRM.Core/Models/Base/`
- [ ] Remove `RemindMe`, `ReminderSent`, `EventFrequency` properties from `SignificantDate.cs`
- [ ] Remove any `ReminderRepository.cs`, `IReminderRepository.cs`
- [ ] Remove any `ReminderDto.cs`, `ReminderViewModel.cs`
- [ ] Remove `DbSet<Reminder>` from `CRMDbContext.cs`

---

## Phase 2: Core — Enumerations
- [ ] Create `Rvnx.CRM.Core/Enumerations/RecurrenceType.cs`
  - Values: `None = 0`, `Annual = 1`, `Monthly = 2`, `Custom = 3`

---

## Phase 3: Core — Entities (`Rvnx.CRM.Core/Models/Dates/`)
- [ ] Replace `SignificantDate.cs` — fields: `ContactId`, `Title`, `Description`, `EventDate (DateOnly)`, `RecurrenceType`, `CustomIntervalDays (int?)`, `IsActive`; nav: `Contact`, `ICollection<ReminderOffset>`
- [ ] Create `ReminderOffset.cs` — fields: `SignificantDateId`, `DaysBeforeEvent (int)`, `IsActive`; nav: `SignificantDate`, `ICollection<ReminderLog>`
- [ ] Create `ReminderLog.cs` — fields: `ReminderOffsetId`, `OccurrenceDate (DateOnly)`, `ScheduledFor (DateOnly)`, `SentAt (DateTime?)`, `Success (bool)`, `EmailAddress`, `ErrorMessage`; nav: `ReminderOffset`
- [ ] Add `ICollection<SignificantDate>` nav property to `Contact.cs`

---

## Phase 4: Infrastructure — DbContext (`CRMDbContext.cs`)
- [ ] Add `DbSet<SignificantDate>`, `DbSet<ReminderOffset>`, `DbSet<ReminderLog>`
- [ ] Configure cascade deletes: Contact → SignificantDate → ReminderOffset → ReminderLog
- [ ] Add unique index on `ReminderLog (ReminderOffsetId, OccurrenceDate)` — prevents duplicate sends

---

## Phase 5: Core — Services
- [ ] Update `DateCalculationService.cs` — add `GetNextOccurrence(SignificantDate, DateOnly fromDate)` and `GetScheduledForDate(SignificantDate, ReminderOffset, DateOnly fromDate)`
  - Annual: use month+day, handle Feb 29 → Feb 28 in non-leap years
  - Monthly: clamp day to end of month
  - Custom: calculate interval from EventDate using `CustomIntervalDays`
  - None: return `EventDate` as-is
- [ ] Create `Rvnx.CRM.Core/Interfaces/ISignificantDateService.cs`
  - Methods: `GetByContactAsync`, `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `AddReminderOffsetAsync`, `DeleteReminderOffsetAsync`
- [ ] Create `Rvnx.CRM.Core/Services/SignificantDateService.cs` implementing above interface
- [ ] Create `Rvnx.CRM.Core/Interfaces/IReminderNotificationService.cs`
  - Method: `SendDueRemindersAsync(DateOnly forDate)`
- [ ] Create `Rvnx.CRM.Infrastructure/Services/ReminderNotificationService.cs`
  - Use `IgnoreQueryFilters()` to access all tenants
  - Only send if `ScheduledFor == forDate` and no successful log exists for `(ReminderOffsetId, OccurrenceDate)`
  - Always write a `ReminderLog` — `Success = false` rows are retried on next run
- [ ] Register both services in their respective `ServiceCollectionExtensions.cs`

---

## Phase 6: Core — DTOs (`Rvnx.CRM.Core/DTOs/Dates/`)
- [ ] `SignificantDateDto.cs` — includes computed `NextOccurrence (DateOnly?)` and list of `ReminderOffsetDto`
- [ ] `ReminderOffsetDto.cs` — includes computed `ScheduledFor (DateOnly?)`
- [ ] `CreateSignificantDateRequest.cs` — includes `List<int> ReminderOffsetDays` for seeding offsets on creation
- [ ] `UpdateSignificantDateRequest.cs`

---

## Phase 7: Console App (`Rvnx.CRM.ConsoleApp/TaskManager.cs`)
- [ ] Add `"SEND-DATE-REMINDERS"` case to task switch
- [ ] Add `RunSendDateRemindersAsync` method calling `IReminderNotificationService.SendDueRemindersAsync(DateOnly.FromDateTime(DateTime.Today))`

---

## Phase 8: Web — Controller (`Rvnx.CRM.Web/Controllers/SignificantDatesController.cs`)
- [ ] `GET  Index(Guid contactId)`
- [ ] `GET  Create(Guid contactId)` — pre-populate `ReminderOffsetDays = [0, 7, 30]`
- [ ] `POST Create(CreateSignificantDateRequest)`
- [ ] `GET  Edit(Guid id)`
- [ ] `POST Edit(Guid id, UpdateSignificantDateRequest)`
- [ ] `POST Delete(Guid id, Guid contactId)`
- [ ] `POST AddOffset(Guid significantDateId, int daysBeforeEvent, Guid contactId)`
- [ ] `POST DeleteOffset(Guid offsetId, Guid significantDateId, Guid contactId)`

---

## Phase 9: Web — Views (`Rvnx.CRM.Web/Views/SignificantDates/`)
- [ ] `Index.cshtml` — table showing Title, EventDate, RecurrenceType, NextOccurrence, active offsets summary, IsActive badge
- [ ] `Create.cshtml` — form with checkboxes for 30/7/0 day offsets; JS toggle to show `CustomIntervalDays` field when `RecurrenceType == Custom`
- [ ] `Edit.cshtml` — same form fields plus inline offset management (add by entering days, delete individual offsets)

---

## Phase 10: Tests (`Rvnx.CRM.Tests/DateCalculationServiceTests.cs`)
- [x] Annual: same-year occurrence when date not yet passed
- [x] Annual: rolls to next year when date has passed
- [x] Annual: Feb 29 birthday → Feb 28 in non-leap year
- [x] Annual: Feb 29 birthday → Feb 29 in leap year
- [x] None: always returns fixed `EventDate`
- [x] Monthly: advances to next month when day has passed
- [x] Monthly: clamps to end of month (e.g. Jan 31 → Feb 28)
- [x] Custom: next occurrence lands on a valid interval boundary
- [x] `GetScheduledForDate`: returns `nextOccurrence - DaysBeforeEvent`

---

## Phase 11: Migrations
- [x] Delete all files in `Rvnx.CRM.Infrastructure/Migrations/`
- [x] Delete the SQLite database file if it exists
- [x] Run: `dotnet ef migrations add InitialCreate --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web`
- [x] Run: `dotnet ef database update --project Rvnx.CRM.Infrastructure --startup-project Rvnx.CRM.Web`
- [x] Verify migration file contains the unique index `IX_ReminderLogs_OffsetId_OccurrenceDate`
