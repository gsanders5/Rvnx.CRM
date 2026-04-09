using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;

namespace Rvnx.CRM.Web.Controllers;

public class FavoritesController(
    IFavoriteService favoriteService,
    IContactReadService contactReadService) : AuthorizedController
{
    private readonly IFavoriteService _favoriteService = favoriteService;
    private readonly IContactReadService _contactReadService = contactReadService;

    [HttpPost]
    public async Task<IActionResult> Toggle(Guid contactId)
    {
        if (!await _contactReadService.ContactExistsAsync(contactId))
        {
            return NotFound();
        }

        bool isFavorite = await _favoriteService.ToggleFavoriteAsync(contactId);
        return Json(new { isFavorite });
    }
}