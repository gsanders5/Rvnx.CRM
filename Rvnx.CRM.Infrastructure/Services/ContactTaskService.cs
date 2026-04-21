using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Infrastructure.Services;

public class ContactTaskService(IRepository repository) : IContactTaskService
{
    private readonly IRepository _repository = repository;

    public async Task<List<ContactTaskDto>> GetByContactAsync(Guid contactId)
    {
        List<ContactTask> tasks = await _repository.ListAsync<ContactTask>(
            t => t.ContactId == contactId
        );
        return [.. tasks.Select(t => t.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(ContactTaskFormDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.EntityId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        ContactTask task = dto.ToEntity();
        await _repository.AddAsync(task);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(task.ContactId ?? Guid.Empty, EntityType.Person);
    }

    public async Task<OperationResult> UpdateAsync(Guid id, ContactTaskFormDto dto)
    {
        try
        {
            ContactTask? existing = await _repository.GetByIdAsync<ContactTask>(id);
            if (existing == null || !await _repository.IsValidContactAsync(existing.ContactId ?? Guid.Empty))
            {
                return OperationResult.NotFound("Task not found.");
            }

            existing.UpdateEntity(dto);
            await _repository.UpdateAsync(existing);
            await _repository.SaveChangesAsync();

            return OperationResult.Ok(existing.ContactId ?? Guid.Empty, EntityType.Person);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<ContactTask>(dto.Id ?? Guid.Empty))
            {
                return OperationResult.NotFound("Task not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        List<Guid?> contactIds = await _repository.ListProjectedAsync<ContactTask, Guid?>(
            t => t.Id == id,
            t => t.ContactId);

        if (contactIds.Count > 0)
        {
            Guid entityId = contactIds.FirstOrDefault() ?? Guid.Empty;
            await _repository.DeleteAsync<ContactTask>(t => t.Id == id);
            await _repository.SaveChangesAsync();
            return OperationResult.Ok(entityId, EntityType.Person);
        }

        return OperationResult.NotFound("Task not found.");
    }

    public async Task<ContactTaskFormDto?> GetFormAsync(Guid id)
    {
        ContactTask? task = await _repository.GetByIdAsync<ContactTask>(id);
        return task == null || !await _repository.IsValidContactAsync(task.ContactId ?? Guid.Empty)
            ? null
            : new ContactTaskFormDto
            {
                Id = task.Id,
                EntityId = task.ContactId ?? Guid.Empty,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                IsCompleted = task.IsCompleted
            };
    }

    public async Task<ContactTaskFormDto?> GetFormForCreateAsync(Guid entityId)
    {
        return !await _repository.IsValidContactAsync(entityId)
            ? null
            : new ContactTaskFormDto
            {
                EntityId = entityId,
                DueDate = DateOnly.FromDateTime(DateTime.Today)
            };
    }

    public async Task<ContactTask?> GetByIdAsync(Guid id)
    {
        ContactTask? task = await _repository.GetByIdAsync<ContactTask>(id);
        return task == null || !await _repository.IsValidContactAsync(task.ContactId ?? Guid.Empty) ? null : task;
    }

    public async Task<OperationResult> ToggleCompleteAsync(Guid id)
    {
        ContactTask? task = await _repository.GetByIdAsync<ContactTask>(id);
        if (task == null || !await _repository.IsValidContactAsync(task.ContactId ?? Guid.Empty))
        {
            return OperationResult.NotFound("Task not found.");
        }

        task.IsCompleted = !task.IsCompleted;
        task.CompletedDate = task.IsCompleted ? DateTime.UtcNow : null;

        await _repository.UpdateAsync(task);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(task.ContactId ?? Guid.Empty, EntityType.Person);
    }

    public async Task<List<CalendarEventDto>> GetCalendarEventsAsync()
    {
        List<(Guid Id, string Title, DateOnly DueDate, Guid? ContactId)> tasks =
            await _repository.ListProjectedAsync<ContactTask, (Guid, string, DateOnly, Guid?)>(
                t => !t.IsCompleted && t.ContactId.HasValue,
                t => new ValueTuple<Guid, string, DateOnly, Guid?>(t.Id, t.Title, t.DueDate, t.ContactId));

        if (tasks.Count == 0)
        {
            return [];
        }

        List<Guid> contactIds = [.. tasks.Where(t => t.ContactId.HasValue).Select(t => t.ContactId!.Value).Distinct()];

        List<(Guid Id, string FirstName)> contacts =
            await _repository.ListProjectedByChunkedContainsAsync<Contact, (Guid, string), Guid>(
                contactIds,
                chunk => c => chunk.Contains(c.Id),
                c => new ValueTuple<Guid, string>(c.Id, c.FirstName));

        Dictionary<Guid, string> contactNames = contacts.ToDictionary(c => c.Id, c => c.FirstName);

        return tasks
            .Where(t => t.ContactId.HasValue && contactNames.ContainsKey(t.ContactId.Value))
            .Select(t => new CalendarEventDto
            {
                Title = $"{contactNames[t.ContactId!.Value]}: {t.Title}",
                Start = t.DueDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                Color = CalendarColors.Task,
                AllDay = true,
                ContactId = t.ContactId!.Value
            })
            .ToList();
    }
}
