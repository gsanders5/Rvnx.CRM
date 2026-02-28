using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using System.Security.Claims;

namespace Rvnx.CRM.Core.Services;

public class SelfContactService(IRepository repository, ICurrentUserService currentUserService, IUserSynchronizationService userSynchronizationService) : ISelfContactService
{
    private readonly IRepository _repository = repository;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IUserSynchronizationService _userSynchronizationService = userSynchronizationService;

    public async Task<Guid?> GetSelfContactIdAsync(ClaimsPrincipal user)
    {
        await _userSynchronizationService.SyncUserAsync(user);
        Guid? userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return null;
        }

        Rvnx.CRM.Core.Models.User? userEntity = await GetUserAsync(userId.Value);
        return userEntity?.SelfContactId;
    }

    public async Task<ContactFormDto?> GetSelfContactFormAsync(ClaimsPrincipal user)
    {
        await _userSynchronizationService.SyncUserAsync(user);
        Guid? userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return null;
        }

        Rvnx.CRM.Core.Models.User? userEntity = await GetUserAsync(userId.Value);
        if (userEntity == null)
        {
            return null;
        }

        ContactFormDto dto = new()
        {
            Email = userEntity.Email
        };

        string userName = _currentUserService.UserName ?? string.Empty;
        if (!string.IsNullOrEmpty(userName))
        {
            int firstSpaceIndex = userName.IndexOf(' ');
            if (firstSpaceIndex > 0)
            {
                dto.FirstName = userName[..firstSpaceIndex];
                dto.LastName = userName[(firstSpaceIndex + 1)..];
            }
            else
            {
                dto.FirstName = userName;
            }
        }

        return dto;
    }

    public async Task<ContactOperationResult> CreateSelfContactAsync(ClaimsPrincipal user, ContactFormDto contactDto)
    {
        Guid? userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return ContactOperationResult.Failure("User not authenticated.");
        }

        Rvnx.CRM.Core.Models.User? userEntity = await GetUserAsync(userId.Value);
        if (userEntity == null)
        {
            return ContactOperationResult.Failure("User entity not found.");
        }

        if (userEntity.SelfContactId.HasValue)
        {
            return ContactOperationResult.Ok(userEntity.SelfContactId.Value);
        }

        Contact contact = contactDto.ToEntity();
        await _repository.AddAsync(contact);

        userEntity.SelfContactId = contact.Id;
        await _repository.UpdateAsync(userEntity);

        await _repository.SaveChangesAsync();

        await UpdateOrAddContactMethod(contact.Id, ContactMethodType.Email, contactDto.Email, null);
        await UpdateOrAddContactMethod(contact.Id, ContactMethodType.Phone, contactDto.Phone, null);
        await UpdateOrAddBirthday(contact.Id, contactDto.Birthday, null, contactDto.RemindOnBirthday);

        await _repository.SaveChangesAsync();

        return ContactOperationResult.Ok(contact.Id);
    }

    private async Task<Rvnx.CRM.Core.Models.User?> GetUserAsync(Guid userId)
    {
        Rvnx.CRM.Core.Models.User? user = await _repository.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId);
        return user ?? (await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SubjectId == userId.ToString())).FirstOrDefault();
    }

    private async Task UpdateOrAddContactMethod(Guid contactId, ContactMethodType type, string? newValue, ContactMethod? existingMethod)
    {
        if (!string.IsNullOrEmpty(newValue))
        {
            if (existingMethod != null)
            {
                if (existingMethod.Value != newValue)
                {
                    existingMethod.Value = newValue;
                    await _repository.UpdateAsync(existingMethod);
                }
            }
            else
            {
                await _repository.AddAsync(new ContactMethod
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
            await _repository.DeleteAsync<ContactMethod>(existingMethod.Id);
        }
    }

    private async Task UpdateOrAddBirthday(Guid contactId, DateTime? newDate, SignificantDate? existingDate, bool remindOnBirthday)
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
                    await _repository.UpdateAsync(existingDate);
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
                await _repository.AddAsync(targetDate);
            }

            // Sync ReminderOffset based on remindOnBirthday
            var offsets = await _repository.ListAsync<ReminderOffset>(o => o.SignificantDateId == targetDate.Id && o.DaysBeforeEvent == 0);
            var offset = offsets.FirstOrDefault();

            if (remindOnBirthday)
            {
                if (offset == null)
                {
                    await _repository.AddAsync(new ReminderOffset
                    {
                        Id = Guid.NewGuid(),
                        SignificantDateId = targetDate.Id,
                        DaysBeforeEvent = 0,
                        IsActive = true
                    });
                }
                else if (!offset.IsActive)
                {
                    offset.IsActive = true;
                    await _repository.UpdateAsync(offset);
                }
            }
            else
            {
                if (offset != null)
                {
                    if (offset.IsActive)
                    {
                        offset.IsActive = false;
                        await _repository.UpdateAsync(offset);
                    }
                }
            }
        }
        else if (existingDate != null)
        {
            await _repository.DeleteAsync<SignificantDate>(existingDate.Id);
        }
    }
}
