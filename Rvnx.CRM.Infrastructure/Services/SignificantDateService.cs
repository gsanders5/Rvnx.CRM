using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;

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
        return dates.Select(d => d.ToDto()).ToList();
    }

    public async Task<OperationResult> CreateAsync(SignificantDateDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
        {
            if (await IsBirthdayAlreadySetAsync(dto.EntityId))
            {
                return OperationResult.Failure("A birthday is already set for this contact.");
            }

            dto.RecurrenceType = Core.Enumerations.RecurrenceType.Annual;
        }

        SignificantDate importantDate = new()
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            EventDate = dto.EventDate,
            ContactId = dto.EntityId,
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

        return OperationResult.Ok(dto.EntityId, dto.EntityType);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, SignificantDateDto dto)
    {
        try
        {
            SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
            if (importantDate == null || !await _repository.IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
            {
                return OperationResult.Failure("Significant date not found.");
            }

            if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
            {
                if (await IsBirthdayAlreadySetAsync(dto.EntityId, dto.Id))
                {
                    return OperationResult.Failure("A birthday is already set for this contact.");
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

            return OperationResult.Ok(dto.EntityId, dto.EntityType);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _repository.ExistsAsync<SignificantDate>(dto.Id))
            {
                return OperationResult.Failure("Significant date not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> AddReminderOffsetAsync(Guid significantDateId, int daysBeforeEvent)
    {
        SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(significantDateId);
        if (importantDate == null || !await _repository.IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
        {
            return OperationResult.Failure("Significant date not found.");
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

        return OperationResult.Ok(importantDate.ContactId ?? Guid.Empty, EntityTypes.Person);
    }

    public async Task<OperationResult> DeleteReminderOffsetAsync(Guid offsetId)
    {
        ReminderOffset? offset = await _repository.GetByIdAsync<ReminderOffset>(offsetId);
        if (offset == null)
        {
            return OperationResult.Failure("Reminder offset not found.");
        }

        SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(offset.SignificantDateId);
        if (importantDate == null)
        {
            return OperationResult.Failure("Significant date not found.");
        }

        await _repository.DeleteAsync<ReminderOffset>(offsetId);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(importantDate.ContactId ?? Guid.Empty, EntityTypes.Person);
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
        if (importantDate != null)
        {
            Guid entityId = importantDate.ContactId ?? Guid.Empty;
            string entityType = EntityTypes.Person;

            await _repository.DeleteAsync<SignificantDate>(id);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Significant date not found.");
    }

    public async Task<SignificantDateDto?> GetDtoAsync(Guid id)
    {
        var dates = await _repository.ListAsync<SignificantDate>(
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
}
