using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Activity;

namespace Rvnx.CRM.Infrastructure.Services;

public class ActivityService(IRepository repository, ISelfContactService selfContactService) : IActivityService
{
    private readonly IRepository _repository = repository;
    private readonly ISelfContactService _selfContactService = selfContactService;

    public async Task<List<ActivityDto>> GetByContactAsync(Guid contactId)
    {
        List<ActivityContact> activityContacts = await _repository.ListAsync<ActivityContact>(
            ac => ac.ContactId == contactId,
            default,
            nameof(ActivityContact.Activity)
        );
        return [.. activityContacts.Select(ac => ac.Activity.ToDto())];
    }

    public async Task<OperationResult> CreateAsync(ActivityFormDto dto)
    {
        if (!await _repository.IsValidContactAsync(dto.ContactId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        // Activities are present-tense — refuse to log against a deceased primary contact.
        if (!await _repository.IsLivingContactAsync(dto.ContactId))
        {
            return OperationResult.Failure("Cannot log an activity for a deceased contact.");
        }

        List<Guid> contactIds = dto.ContactIds.Count > 0 ? dto.ContactIds : [dto.ContactId];
        if (!contactIds.Contains(dto.ContactId))
        {
            contactIds.Add(dto.ContactId);
        }

        Activity activity = dto.ToEntity();
        await _repository.AddAsync(activity);

        List<ActivityContact> activityContacts = contactIds.Select(contactId => new ActivityContact
        {
            ActivityId = activity.Id,
            ContactId = contactId
        }).ToList();
        await _repository.AddRangeAsync(activityContacts);

        await _repository.SaveChangesAsync();

        return OperationResult.Ok(dto.ContactId);
    }

    public async Task<OperationResult> QuickLogAsync(Guid contactId, string activityType)
    {
        if (!ActivityTypeSuggestions.QuickLog.Any(q => q.Type == activityType))
        {
            return OperationResult.Failure("Invalid activity type.");
        }

        if (!await _repository.IsValidContactAsync(contactId))
        {
            return OperationResult.NotFound("Contact not found.");
        }

        // Activities are present-tense ("had lunch") — refuse to log against a deceased contact.
        if (!await _repository.IsLivingContactAsync(contactId))
        {
            return OperationResult.Failure("Cannot log an activity for a deceased contact.");
        }

        ActivityFormDto dto = new()
        {
            ContactId = contactId,
            Title = activityType,
            ActivityType = activityType,
            ActivityDate = DateTime.Today,
            ContactIds = await BuildContactIdsWithSelfAsync(contactId)
        };

        return await CreateAsync(dto);
    }

    private async Task<List<Guid>> BuildContactIdsWithSelfAsync(Guid contactId)
    {
        List<Guid> contactIds = [contactId];
        Guid? selfContactId = await _selfContactService.GetSelfContactIdAsync();
        if (selfContactId.HasValue && selfContactId.Value != contactId)
        {
            contactIds.Add(selfContactId.Value);
        }
        return contactIds;
    }

    public async Task<OperationResult> UpdateAsync(Guid id, ActivityFormDto dto)
    {
        try
        {
            Activity? existing = await _repository.GetByIdWithIncludesAsync<Activity>(id, nameof(Activity.ActivityContacts));
            if (existing == null)
            {
                return OperationResult.NotFound("Activity not found.");
            }

            existing.UpdateEntity(dto);
            await _repository.UpdateAsync(existing);

            List<Guid> contactIds = dto.ContactIds.Count > 0 ? dto.ContactIds : [dto.ContactId];
            if (!contactIds.Contains(dto.ContactId))
            {
                contactIds.Add(dto.ContactId);
            }

            HashSet<Guid> desiredContactIds = [.. contactIds];
            HashSet<Guid> existingContactIds = [.. existing.ActivityContacts.Select(ac => ac.ContactId)];

            List<ActivityContact> toRemove = existing.ActivityContacts
                .Where(ac => !desiredContactIds.Contains(ac.ContactId))
                .ToList();
            if (toRemove.Count > 0)
            {
                await _repository.DeleteRangeAsync(toRemove);
            }

            List<Guid> toAdd = contactIds.Where(cid => !existingContactIds.Contains(cid)).ToList();
            if (toAdd.Count > 0)
            {
                await _repository.AddRangeAsync(toAdd.Select(contactId => new ActivityContact
                {
                    ActivityId = id,
                    ContactId = contactId
                }));
            }

            await _repository.SaveChangesAsync();

            return OperationResult.Ok(dto.ContactId);
        }
        catch (EntityConcurrencyException)
        {
            if (!await _repository.ExistsAsync<Activity>(id))
            {
                return OperationResult.NotFound("Activity not found.");
            }
            throw;
        }
    }

    public async Task<OperationResult> DeleteAsync(Guid id)
    {
        if (!await _repository.ExistsAsync<Activity>(id))
        {
            return OperationResult.NotFound("Activity not found.");
        }

        Guid contactId = (await _repository.ListProjectedAsync<ActivityContact, Guid>(
            ac => ac.ActivityId == id,
            ac => ac.ContactId)).FirstOrDefault();

        await _repository.DeleteAsync<Activity>(a => a.Id == id);
        await _repository.SaveChangesAsync();

        return OperationResult.Ok(contactId);
    }

    public async Task<ActivityFormDto?> GetFormAsync(Guid id)
    {
        Activity? activity = await _repository.GetByIdWithIncludesAsync<Activity>(id, nameof(Activity.ActivityContacts));
        if (activity == null)
        {
            return null;
        }

        List<Guid> contactIds = activity.ActivityContacts.Select(ac => ac.ContactId).ToList();
        Guid contactId = contactIds.FirstOrDefault();

        return new ActivityFormDto
        {
            Id = activity.Id,
            ContactId = contactId,
            ContactIds = contactIds,
            Title = activity.Title,
            Description = activity.Description,
            ActivityDate = activity.ActivityDate,
            ActivityType = activity.ActivityType,
            Location = activity.Location
        };
    }

    public async Task<ActivityFormDto?> GetFormForCreateAsync(Guid contactId)
    {
        // Forward-looking: refuse to render the create form for a deceased contact.
        return !await _repository.IsLivingContactAsync(contactId)
            ? null
            : new ActivityFormDto { ContactId = contactId, ContactIds = await BuildContactIdsWithSelfAsync(contactId) };
    }

    public async Task<Activity?> GetByIdAsync(Guid id)
    {
        return await _repository.GetByIdWithIncludesAsync<Activity>(id, nameof(Activity.ActivityContacts));
    }
}
