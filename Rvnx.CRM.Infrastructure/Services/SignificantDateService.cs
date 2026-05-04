using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;

namespace Rvnx.CRM.Infrastructure.Services;

public class SignificantDateService(IRepository repository) : ISignificantDateService
{
    private readonly IRepository _repository = repository;

    public async Task<List<SignificantDateDto>> GetByContactAsync(Guid contactId)
    {
        List<SignificantDate> dates = await _repository.ListAsync<SignificantDate>(
            d => d.ContactId == contactId,
            default,
            nameof(SignificantDate.ReminderOffsets)
        );
        return [.. dates.Select(d => d.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(SignificantDateDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.ContactId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
        {
            if (await IsBirthdayAlreadySetAsync(dto.ContactId))
            {
                return OperationResult.Conflict("A birthday is already set for this contact.");
            }

            dto.RecurrenceType = Core.Enumerations.RecurrenceType.Annual;
        }

        SignificantDate importantDate = new()
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            EventDate = dto.EventDate,
            ContactId = dto.ContactId,
            RecurrenceType = dto.RecurrenceType,
            CustomIntervalDays = dto.CustomIntervalDays,
            IsActive = dto.IsActive,
            ReminderOffsets = dto.ReminderOffsets.Select(ro => new ReminderOffset
            {
                Id = Guid.NewGuid(),
                DaysBeforeEvent = ro.DaysBeforeEvent,
                IsActive = ro.IsActive
            }).ToList()
        };

        await _repository.AddAsync(importantDate);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(dto.ContactId);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, SignificantDateDto dto)
    {
        try
        {
            SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
            if (importantDate == null || !await _repository.IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
            {
                return OperationResult.NotFound("Significant date not found.");
            }

            if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
            {
                if (await IsBirthdayAlreadySetAsync(dto.ContactId, dto.Id))
                {
                    return OperationResult.Conflict("A birthday is already set for this contact.");
                }

                dto.RecurrenceType = Core.Enumerations.RecurrenceType.Annual;
            }

            importantDate.Title = dto.Title;
            importantDate.Description = dto.Description;
            importantDate.EventDate = dto.EventDate;
            importantDate.RecurrenceType = dto.RecurrenceType;
            importantDate.CustomIntervalDays = dto.CustomIntervalDays;
            importantDate.IsActive = dto.IsActive;

            await _repository.UpdateAsync(importantDate);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(dto.ContactId);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<SignificantDate>(dto.Id))
            {
                return OperationResult.NotFound("Significant date not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> AddReminderOffsetAsync(Guid significantDateId, int daysBeforeEvent)
    {
        SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(significantDateId);
        if (importantDate == null || !await _repository.IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
        {
            return OperationResult.NotFound("Significant date not found.");
        }

        ReminderOffset offset = new()
        {
            Id = Guid.NewGuid(),
            SignificantDateId = significantDateId,
            DaysBeforeEvent = daysBeforeEvent,
            IsActive = true
        };

        await _repository.AddAsync(offset);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(importantDate.ContactId ?? Guid.Empty);
    }

    public async Task<OperationResult> DeleteReminderOffsetAsync(Guid offsetId)
    {
        List<Guid> significantDateIds = await _repository.ListProjectedAsync<ReminderOffset, Guid>(
            ro => ro.Id == offsetId,
            ro => ro.SignificantDateId);

        if (significantDateIds.Count == 0)
        {
            return OperationResult.NotFound("Reminder offset not found.");
        }

        Guid significantDateId = significantDateIds.FirstOrDefault();
        List<Guid?> contactIds = await _repository.ListProjectedAsync<SignificantDate, Guid?>(
            sd => sd.Id == significantDateId,
            sd => sd.ContactId);

        if (contactIds.Count == 0)
        {
            return OperationResult.NotFound("Significant date not found.");
        }

        await _repository.DeleteAsync<ReminderOffset>(ro => ro.Id == offsetId);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(contactIds.FirstOrDefault() ?? Guid.Empty);
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        List<Guid?> contactIds = await _repository.ListProjectedAsync<SignificantDate, Guid?>(
            sd => sd.Id == id,
            sd => sd.ContactId);

        if (contactIds.Count > 0)
        {
            Guid entityId = contactIds.FirstOrDefault() ?? Guid.Empty;

            await _repository.DeleteAsync<SignificantDate>(sd => sd.Id == id);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(entityId);
        }
        return OperationResult.NotFound("Significant date not found.");
    }

    public async Task<SignificantDateDto?> GetDtoAsync(Guid id)
    {
        List<SignificantDate> dates = await _repository.ListAsync<SignificantDate>(
            d => d.Id == id,
            default,
            nameof(SignificantDate.ReminderOffsets)
        );
        SignificantDate? importantDate = dates.FirstOrDefault();
        return importantDate == null || !await _repository.IsValidContactAsync(importantDate.ContactId ?? Guid.Empty) ? null : importantDate.ToDto();
    }

    public async Task<SignificantDate?> GetByIdAsync(Guid id)
    {
        SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
        return importantDate == null || !await _repository.IsValidContactAsync(importantDate.ContactId ?? Guid.Empty) ? null : importantDate;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1862:Use the 'StringComparison' method overloads to perform case-insensitive string comparisons", Justification = "EF Core cannot translate string.Equals with StringComparison. .ToLower() is used for SQLite-compatible translatable case-insensitivity.")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1304:Specify CultureInfo", Justification = "EF Core cannot translate .ToLower(CultureInfo).")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1311:Specify a culture or use an invariant version", Justification = "EF Core cannot translate .ToLower(CultureInfo) or .ToLowerInvariant().")]
    private async Task<bool> IsBirthdayAlreadySetAsync(Guid contactId, Guid? excludeId = null)
    {
        return (await _repository.CountAsync<SignificantDate>(d =>
                   d.ContactId == contactId &&
                   d.Id != (excludeId ?? Guid.Empty) &&
                   d.Title != null &&
                   d.Title.ToLower() == SignificantDateTitles.Birthday.ToLower())) >
               0;
    }

    public async Task<List<CalendarEventDto>> GetCalendarEventsAsync()
    {
        List<SignificantDate> dates = await _repository.ListAsNoTrackingAsync<SignificantDate>(sd => sd.ContactId.HasValue && sd.IsActive);

        if (dates.Count == 0)
        {
            return [];
        }

        List<Guid> contactIds = [.. dates.Select(d => d.ContactId!.Value).Distinct()];

        List<(Guid Id, string FirstName, string? LastName)> contacts =
            await _repository.ListProjectedByChunkedContainsAsync<Contact, (Guid, string, string?), Guid>(contactIds,
                chunk => c => chunk.Contains(c.Id) && !c.IsPartial && !c.IsDeceased,
                c => new ValueTuple<Guid, string, string?>(c.Id, c.FirstName, c.LastName));

        Dictionary<Guid, string> contactNames = contacts.ToDictionary(c => c.Id, c => $"{c.FirstName} {c.LastName}".Trim());
        DateOnly today = DateOnly.FromDateTime(DateTime.Today);
        List<CalendarEventDto> events = [];

        foreach (SignificantDate date in dates)
        {
            if (!contactNames.TryGetValue(date.ContactId!.Value, out string? contactName))
            {
                continue;
            }

            bool isBirthday = string.Equals(date.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase);
            DateOnly nextOccurrence = DateCalculationService.GetNextOccurrence(date, today);
            string firstName = contactName.Split(' ')[0];
            string color = isBirthday ? CalendarColors.Birthday : CalendarColors.SignificantDate;
            string title = $"{firstName}'s {date.Title}";
            Guid contactId = date.ContactId!.Value;

            events.Add(CreateEvent(title, nextOccurrence, color, contactId));

            DateOnly? currentYearOccurrence = DateCalculationService.GetCurrentYearOccurrence(date, today, nextOccurrence);
            if (currentYearOccurrence.HasValue)
            {
                events.Add(CreateEvent(title, currentYearOccurrence.Value, color, contactId));
            }
        }

        return events;
    }

    private static CalendarEventDto CreateEvent(string title, DateOnly date, string color, Guid contactId)
    {
        return new()
        {
            Title = title,
            Start = date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
            Color = color,
            AllDay = true,
            ContactId = contactId
        };
    }
}
