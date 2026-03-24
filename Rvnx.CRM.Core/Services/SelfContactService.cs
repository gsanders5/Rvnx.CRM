using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
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

        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repository, contact.Id, ContactMethodType.Email, contactDto.Email, null);
        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repository, contact.Id, ContactMethodType.Phone, contactDto.Phone, null);
        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repository, contact.Id, contactDto.Birthday, null, contactDto.RemindOnBirthday, contactDto.BirthdayYearUnknown);

        await _repository.SaveChangesAsync();

        return ContactOperationResult.Ok(contact.Id);
    }

    private async Task<Rvnx.CRM.Core.Models.User?> GetUserAsync(Guid userId)
    {
        Rvnx.CRM.Core.Models.User? user = await _repository.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId);
        return user ?? (await _repository.ListAsync<Rvnx.CRM.Core.Models.User>(u => u.SubjectId == userId.ToString())).FirstOrDefault();
    }

}