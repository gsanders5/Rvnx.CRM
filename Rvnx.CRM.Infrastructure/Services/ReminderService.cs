using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Infrastructure.Services;

public class ReminderService(IRepository repository, IEntityService entityService) : IReminderService
{
    private readonly IRepository _repository = repository;
    private readonly IEntityService _entityService = entityService;

    public async Task<OperationResult> CreateAsync(ReminderFormViewModel dto)
    {
        if (!await _repository.IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.Failure("Contact not found.");
        }

        Reminder reminder = new()
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            IsCompleted = dto.IsCompleted,
            ContactId = dto.EntityId,
            RemindMe = dto.RemindMe,
            EventFrequency = dto.EventFrequency
        };

        await _repository.AddAsync(reminder);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(reminder.ContactId ?? Guid.Empty, EntityTypes.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, ReminderFormViewModel dto)
    {
        try
        {
            Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id);
            if (reminder == null || !await _repository.IsValidContactAsync(reminder.ContactId ?? Guid.Empty))
            {
                return OperationResult.Failure("Reminder not found.");
            }

            reminder.UpdateEntity(dto);

            await _repository.UpdateAsync(reminder);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(reminder.ContactId ?? Guid.Empty, EntityTypes.Person);
        }
        catch (Exception)
        {
            if (!await _repository.ExistsAsync<Reminder>(dto.Id))
            {
                return OperationResult.Failure("Reminder not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id);
        if (reminder != null)
        {
            Guid entityId = reminder.ContactId ?? Guid.Empty;
            string entityType = EntityTypes.Person;
            await _repository.DeleteAsync<Reminder>(id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, entityType);
        }
        return OperationResult.Failure("Reminder not found.");
    }

    public async Task<ReminderFormViewModel?> GetFormAsync(Guid id)
    {
        Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id);

        if (reminder == null || !await _repository.IsValidContactAsync(reminder.ContactId ?? Guid.Empty))
        {
            return null;
        }

        ReminderDto dto = reminder.ToDto();
        return new ReminderFormViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            IsCompleted = dto.IsCompleted,
            EntityId = dto.EntityId,
            EntityType = dto.EntityType,
            RemindMe = dto.RemindMe,
            ReminderSent = dto.ReminderSent,
            EventFrequency = dto.EventFrequency,
            EntityName = await _entityService.GetEntityNameAsync(dto.EntityType, dto.EntityId)
        };
    }

    public async Task<ReminderFormViewModel?> GetFormForCreateAsync(Guid entityId, string entityType)
    {
        if (!await _repository.IsValidContactAsync(entityId))
        {
            return null;
        }

        ReminderDto dto = new Reminder
        {
            ContactId = entityId,
            DueDate = DateTime.Now.AddDays(1),
            EventFrequency = TimeSpan.FromDays(365) // Default
        }.ToDto();

        return new ReminderFormViewModel
        {
            Id = dto.Id,
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
            IsCompleted = dto.IsCompleted,
            EntityId = dto.EntityId,
            EntityType = dto.EntityType,
            RemindMe = dto.RemindMe,
            ReminderSent = dto.ReminderSent,
            EventFrequency = dto.EventFrequency,
            EntityName = await _entityService.GetEntityNameAsync(entityType, entityId)
        };
    }

    public async Task<ReminderDto?> GetDtoAsync(Guid id)
    {
        Reminder? reminder = await _repository.GetByIdAsync<Reminder>(id);
        return reminder == null || !await _repository.IsValidContactAsync(reminder.ContactId ?? Guid.Empty) ? null : reminder.ToDto();
    }
}
