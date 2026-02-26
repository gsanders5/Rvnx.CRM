using Microsoft.EntityFrameworkCore;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Infrastructure.Services;

public class SignificantDateService(IRepository repository) : ISignificantDateService
{
    private readonly IRepository _repository = repository;

    public async Task<OperationResult> CreateAsync(SignificantDateDto dto)
    {
        if (!await IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
        {
            if (await IsBirthdayAlreadySetAsync(dto.EntityId))
            {
                // This message will be used to add a model error in the controller
                return OperationResult.Failure("A birthday is already set for this contact.");
            }

            dto.EventFrequency = TimeSpan.FromDays(365);
        }

        SignificantDate importantDate = new()
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            Date = dto.Date,
            ContactId = dto.EntityId,
            RemindMe = dto.RemindMe,
            EventFrequency = dto.EventFrequency
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
            if (importantDate == null || !await IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
            {
                return OperationResult.Failure("Significant date not found.");
            }

            if (string.Equals(dto.Title, SignificantDateTitles.Birthday, StringComparison.OrdinalIgnoreCase))
            {
                if (await IsBirthdayAlreadySetAsync(dto.EntityId, dto.Id))
                {
                    return OperationResult.Failure("A birthday is already set for this contact.");
                }

                dto.EventFrequency = TimeSpan.FromDays(365);
            }

            importantDate.Title = dto.Title;
            importantDate.Description = dto.Description;
            importantDate.Date = dto.Date;
            importantDate.RemindMe = dto.RemindMe;
            importantDate.EventFrequency = dto.EventFrequency;

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
        SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
        if (importantDate == null || !await IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
        {
            return null;
        }
        return importantDate.ToDto();
    }

    public async Task<SignificantDate?> GetByIdAsync(Guid id)
    {
        SignificantDate? importantDate = await _repository.GetByIdAsync<SignificantDate>(id);
        if (importantDate == null || !await IsValidContactAsync(importantDate.ContactId ?? Guid.Empty))
        {
            return null;
        }
        return importantDate;
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

    private async Task<bool> IsValidContactAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return false;
        }

        return await _repository.CountAsync<Contact>(c => c.Id == id && !c.IsPartial) > 0;
    }
}
