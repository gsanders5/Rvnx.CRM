using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Web.Controllers.Base;
using System.ComponentModel.DataAnnotations;

namespace Rvnx.CRM.Web.Controllers;

public class ApiTokenCreateViewModel
{
    [Required]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    public DateTime? ExpiresAt { get; set; }
}

public class ApiTokensController(IApiTokenService apiTokenService, ICurrentUserService currentUserService) : AuthorizedController
{
    private readonly IApiTokenService _apiTokenService = apiTokenService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var tokens = await _apiTokenService.ListTokensAsync(userId.Value);
        return View(tokens);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new ApiTokenCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ApiTokenCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var userId = _currentUserService.UserId;
        var groupId = _currentUserService.GroupId;

        if (userId == null || groupId == null)
        {
            return Unauthorized();
        }

        var (token, rawToken) = await _apiTokenService.CreateTokenAsync(userId.Value, groupId.Value, model.Name, model.ExpiresAt);

        TempData["RawToken"] = rawToken;
        TempData["TokenName"] = token.Name;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid id)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var success = await _apiTokenService.RevokeTokenAsync(id, userId.Value);

        if (!success)
        {
            return NotFound();
        }

        return RedirectToAction(nameof(Index));
    }
}
