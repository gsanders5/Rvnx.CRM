using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Core.Services;

internal static class ContactUpdateHelper
{
    public static async Task UpdateOrAddContactMethodAsync(IRepository repository, Guid contactId, ContactMethodType type, string? newValue, ContactMethod? existingMethod)
    {
        if (!string.IsNullOrEmpty(newValue))
        {
            if (existingMethod != null)
            {
                if (existingMethod.Value != newValue)
                {
                    existingMethod.Value = newValue;
                    await repository.UpdateAsync(existingMethod);
                }
            }
            else
            {
                await repository.AddAsync(new ContactMethod
                {
                    Id = Guid.NewGuid(),
                    ContactId = contactId,
                    Type = type,
                    Value = newValue,
                    Label = ContactMethodLabels.Primary
                });
            }
        }
        else if (existingMethod != null)
        {
            await repository.DeleteAsync<ContactMethod>(existingMethod.Id);
        }
    }

    public static async Task UpdateOrAddBirthdayAsync(IRepository repository, Guid contactId, DateTime? newDate, SignificantDate? existingDate, bool remindOnBirthday)
    {
        if (newDate.HasValue)
        {
            DateOnly newDateOnly = DateOnly.FromDateTime(newDate.Value);
            SignificantDate targetDate = existingDate!;

            if (existingDate != null)
            {
                if (existingDate.EventDate != newDateOnly)
                {
                    existingDate.EventDate = newDateOnly;
                    await repository.UpdateAsync(existingDate);
                }
            }
            else
            {
                targetDate = new SignificantDate
                {
                    Id = Guid.NewGuid(),
                    ContactId = contactId,
                    Title = SignificantDateTitles.Birthday,
                    EventDate = newDateOnly,
                    Description = "Birthday",
                    RecurrenceType = RecurrenceType.Annual,
                    IsActive = true
                };
                await repository.AddAsync(targetDate);
            }

            // Sync ReminderOffset based on remindOnBirthday
            List<ReminderOffset> offsets = await repository.ListAsync<ReminderOffset>(o => o.SignificantDateId == targetDate.Id && o.DaysBeforeEvent == 0);
            ReminderOffset? offset = offsets.FirstOrDefault();

            await SyncReminderOffsetAsync(repository, targetDate.Id, offset, remindOnBirthday);
        }
        else if (existingDate != null)
        {
            await repository.DeleteAsync<SignificantDate>(existingDate.Id);
        }
    }

    private static async Task SyncReminderOffsetAsync(IRepository repository, Guid targetDateId, ReminderOffset? offset, bool remindOnBirthday)
    {
        if (remindOnBirthday)
        {
            if (offset == null)
            {
                await repository.AddAsync(new ReminderOffset
                {
                    Id = Guid.NewGuid(),
                    SignificantDateId = targetDateId,
                    DaysBeforeEvent = 0,
                    IsActive = true
                });
            }
            else if (!offset.IsActive)
            {
                offset.IsActive = true;
                await repository.UpdateAsync(offset);
            }
        }
        else if (offset?.IsActive == true)
        {
            offset.IsActive = false;
            await repository.UpdateAsync(offset);
        }
    }
}
