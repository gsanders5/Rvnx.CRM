using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    private readonly IFavoriteService _favoriteService = favoriteService;

    [HttpGet]
    public async Task<IActionResult> List()
    {
        HashSet<Guid> favoriteIds = await _favoriteService.GetFavoriteContactIdsAsync();
        return Ok(favoriteIds);
    }

    [HttpPost("{contactId}")]
    public async Task<IActionResult> Toggle(Guid contactId)
    {
        bool isFavorited = await _favoriteService.ToggleFavoriteAsync(contactId);
        return Ok(new { IsFavorited = isFavorited });
    }
}
