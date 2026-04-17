using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.API.Controllers;

/// <summary>
/// Manages favorite contacts for the current user.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    private readonly IFavoriteService _favoriteService = favoriteService;

    /// <summary>
    /// List the GUIDs of all contacts marked as favorites by the current user.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List()
    {
        HashSet<Guid> favoriteIds = await _favoriteService.GetFavoriteContactIdsAsync();
        return Ok(favoriteIds);
    }

    /// <summary>
    /// Toggle a contact's favorite status. If currently favorited, removes it;
    /// if not favorited, adds it. Returns the new favorite state.
    /// </summary>
    /// <param name="contactId">The contact GUID.</param>
    /// <returns>{ isFavorited: true/false }</returns>
    [HttpPost("{contactId}")]
    public async Task<IActionResult> Toggle(Guid contactId)
    {
        bool isFavorited = await _favoriteService.ToggleFavoriteAsync(contactId);
        return Ok(new { IsFavorited = isFavorited });
    }
}