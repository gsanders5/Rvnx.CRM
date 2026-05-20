using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;

namespace Rvnx.CRM.Core.Services;

public class FavoriteService(IRepository repository, ICurrentUserService currentUserService) : IFavoriteService
{
    private readonly IRepository _repository = repository;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    public async Task<bool> ToggleFavoriteAsync(Guid contactId)
    {
        Guid? userId = _currentUserService.UserId;
        if (userId == null)
        {
            return false;
        }

        int count = await _repository.CountAsync<ContactFavorite>(
            f => f.ContactId == contactId && f.UserId == userId);

        if (count > 0)
        {
            await _repository.DeleteAsync<ContactFavorite>(f => f.ContactId == contactId && f.UserId == userId);
            await _repository.SaveChangesAsync();
            return false;
        }

        ContactFavorite favorite = new()
        {
            ContactId = contactId,
            UserId = userId
        };
        await _repository.AddAsync(favorite);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<HashSet<Guid>> GetFavoriteContactIdsAsync()
    {
        Guid? userId = _currentUserService.UserId;
        if (userId == null)
        {
            return [];
        }

        List<Guid> ids = await _repository.ListProjectedAsync<ContactFavorite, Guid>(
            f => f.UserId == userId,
            f => f.ContactId);

        return [.. ids];
    }

    public async Task<List<FavoriteSidebarItemDto>> GetFavoriteSidebarItemsAsync()
    {
        Guid? userId = _currentUserService.UserId;
        if (userId == null)
        {
            return [];
        }

        List<Guid> favoriteIds = await _repository.ListProjectedAsync<ContactFavorite, Guid>(
            f => f.UserId == userId,
            f => f.ContactId);

        if (favoriteIds.Count == 0)
        {
            return [];
        }

        List<FavoriteSidebarItemDto> items = await _repository.ListProjectedByChunkedContainsAsync<Contact, FavoriteSidebarItemDto, Guid>(
            favoriteIds,
            chunk => c => chunk.Contains(c.Id) && !c.IsHidden && !c.IsDeceased,
            c => new FavoriteSidebarItemDto
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
            });

        if (items.Count == 0)
        {
            return [];
        }

        List<Guid> itemIds = [.. items.Select(i => i.Id)];
        List<(Guid ContactId, Guid AttachmentId)> profileAttachments =
            await _repository.ListProjectedByChunkedContainsAsync<Attachment, (Guid, Guid), Guid>(
                itemIds,
                chunk => a => a.ContactId != null && chunk.Contains(a.ContactId.Value)
                                                  && a.AttachmentType == AttachmentTypes.ProfileImage,
                a => new ValueTuple<Guid, Guid>(a.ContactId!.Value, a.Id));

        Dictionary<Guid, Guid> profileImageByContact = new(profileAttachments.Count);
        foreach ((Guid contactId, Guid attachmentId) in profileAttachments)
        {
            profileImageByContact.TryAdd(contactId, attachmentId);
        }

        foreach (FavoriteSidebarItemDto item in items)
        {
            if (profileImageByContact.TryGetValue(item.Id, out Guid attachmentId))
            {
                item.ProfileImageId = attachmentId;
            }
        }

        return [.. items.OrderBy(i => i.FirstName).ThenBy(i => i.LastName)];
    }
}
