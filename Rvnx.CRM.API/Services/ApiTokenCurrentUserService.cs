using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.API.Services;

public class ApiTokenCurrentUserService : ICurrentUserService
{
    private static readonly Action<ILogger, Exception?> LogResolveTokenError =
        LoggerMessage.Define(LogLevel.Error, new EventId(1), "Error resolving API token.");

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

        if (context.Items.ContainsKey("IsResolvingApiToken"))
        {
            return;
        }

        string rawToken = authHeader["Bearer ".Length..].Trim();

        try
        {
            context.Items["IsResolvingApiToken"] = true;

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
            LogResolveTokenError(_logger, ex);
            _resolvedToken = null;
        }
        finally
        {
            context.Items.Remove("IsResolvingApiToken");
        }
    }
}