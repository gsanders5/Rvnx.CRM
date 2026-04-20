using Rvnx.CRM.Core.Interfaces;
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
}