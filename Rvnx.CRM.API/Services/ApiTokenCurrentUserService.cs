using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Services;

public class ApiTokenCurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ApiTokenCurrentUserService> _logger;

    private ApiToken? _resolvedToken;
    private bool _hasAttemptedResolution;

    public ApiTokenCurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ILogger<ApiTokenCurrentUserService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Guid? UserId
    {
        get
        {
            EnsureTokenResolved();
            return _resolvedToken?.UserId;
        }
    }

    public Guid? GroupId
    {
        get
        {
            EnsureTokenResolved();
            return _resolvedToken?.GroupId;
        }
    }

    public string? UserName
    {
        get
        {
            EnsureTokenResolved();
            return _resolvedToken != null ? _resolvedToken.Name : "System";
        }
    }

    public bool IsAuthenticated
    {
        get
        {
            EnsureTokenResolved();
            return _resolvedToken != null;
        }
    }

    public Task<bool> IsAdministratorAsync(Guid userId)
    {
        return Task.FromResult(false); // API tokens don't currently support admin access
    }

    private void EnsureTokenResolved()
    {
        if (_hasAttemptedResolution)
        {
            return;
        }

        _hasAttemptedResolution = true;

        HttpContext? context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return;
        }

        string? authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string rawToken = authHeader.Substring("Bearer ".Length).Trim();

        try
        {
            // Resolve using the service
            using IServiceScope scope = _serviceProvider.CreateScope();
            IApiTokenService tokenService = scope.ServiceProvider.GetRequiredService<IApiTokenService>();

            // Note: ResolveTokenAsync is async but we are in synchronous property getters.
            // Using GetAwaiter().GetResult() is generally frowned upon but necessary here
            // since ICurrentUserService properties are synchronous.
            // The AuthHandler should ideally pre-warm this or we accept the sync-over-async here.
            _resolvedToken = tokenService.ResolveTokenAsync(rawToken).GetAwaiter().GetResult();

            if (_resolvedToken != null)
            {
                // Update LastUsedAt (fire and forget to not block)
                _resolvedToken.LastUsedAt = DateTime.UtcNow;
                IRepository repo = scope.ServiceProvider.GetRequiredService<IRepository>();
                repo.UpdateAsync(_resolvedToken).GetAwaiter().GetResult();
                repo.SaveChangesAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving API token.");
            _resolvedToken = null;
        }
    }
}
