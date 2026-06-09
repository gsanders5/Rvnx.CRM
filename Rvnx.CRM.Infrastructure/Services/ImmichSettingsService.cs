using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Rvnx.CRM.Core.DTOs.Immich;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Infrastructure.Services;

public class ImmichSettingsService(IRepository repository, IMemoryCache cache, ICurrentUserService currentUserService, IConfiguration configuration) : IImmichSettingsService
{
    private static readonly TimeSpan ConnectionCacheTtl = TimeSpan.FromMinutes(5);

    private readonly IRepository _repository = repository;
    private readonly IMemoryCache _cache = cache;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly IConfiguration _configuration = configuration;

    // Server-wide master switch. Absent config defaults to enabled so existing deployments
    // keep working after upgrade; an administrator sets "Immich:Enabled": false to turn the
    // whole integration off, leaving per-group enable/disable and API keys to each group.
    public bool ServerEnabled => _configuration.GetValue("Immich:Enabled", true);

    public async Task<ImmichSettingsDto?> GetSettingsAsync(CancellationToken ct = default)
    {
        GroupImmichSettings? settings = await GetCurrentAsync(tracked: false, ct);
        return settings == null
            ? null
            : new ImmichSettingsDto
            {
                Enabled = settings.Enabled,
                BaseUrl = settings.BaseUrl,
                ApiKeyHint = MaskApiKey(settings.ApiKey),
            };
    }

    // Cached (including the "not configured" case) because every proxied thumbnail request
    // needs the connection; SaveAsync/DeleteAsync invalidate on change.
    public async Task<ImmichConnectionDto?> GetConnectionAsync(CancellationToken ct = default)
    {
        // When the server-wide flag is off the integration is unavailable to every group,
        // so short-circuit before touching the cache or database.
        if (!ServerEnabled)
        {
            return null;
        }

        string cacheKey = ImmichCacheKeys.Connection(_currentUserService.GroupId);
        if (_cache.TryGetValue(cacheKey, out ImmichConnectionDto? cached))
        {
            return cached;
        }

        GroupImmichSettings? settings = await GetCurrentAsync(tracked: false, ct);
        ImmichConnectionDto? connection = settings == null
            ? null
            : new ImmichConnectionDto(settings.GroupId, settings.Enabled, settings.BaseUrl, settings.ApiKey);

        _cache.Set(cacheKey, connection, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ConnectionCacheTtl
        });
        return connection;
    }

    public async Task<ImmichSettingsOperationResult> SaveAsync(bool enabled, string baseUrl, string? apiKey, CancellationToken ct = default)
    {
        baseUrl = baseUrl?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return ImmichSettingsOperationResult.Failure("Server URL is required.");
        }
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? parsed)
            || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
        {
            return ImmichSettingsOperationResult.Failure("Server URL must be an absolute http(s) URL, e.g. https://immich.example.com/api.");
        }

        GroupImmichSettings? settings = await GetCurrentAsync(tracked: true, ct);
        if (settings == null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                return ImmichSettingsOperationResult.Failure("An API key is required when connecting Immich for the first time.");
            }

            settings = new GroupImmichSettings
            {
                Id = Guid.NewGuid(),
                Enabled = enabled,
                BaseUrl = baseUrl,
                ApiKey = apiKey.Trim(),
            };
            await _repository.AddAsync(settings, ct);
        }
        else
        {
            settings.Enabled = enabled;
            settings.BaseUrl = baseUrl;
            if (!string.IsNullOrWhiteSpace(apiKey))
            {
                settings.ApiKey = apiKey.Trim();
            }
            await _repository.UpdateAsync(settings, ct);
        }

        await _repository.SaveChangesAsync(ct);
        InvalidateLookupCaches(settings.GroupId);
        return ImmichSettingsOperationResult.Ok();
    }

    public async Task<ImmichSettingsOperationResult> DeleteAsync(CancellationToken ct = default)
    {
        GroupImmichSettings? settings = await GetCurrentAsync(tracked: true, ct);
        if (settings == null)
        {
            return ImmichSettingsOperationResult.NotFound();
        }

        Guid? groupId = settings.GroupId;
        await _repository.DeleteAsync(settings, ct);
        await _repository.SaveChangesAsync(ct);
        InvalidateLookupCaches(groupId);
        return ImmichSettingsOperationResult.Ok();
    }

    // The repository's global query filter scopes the query to the current group, and the unique
    // GroupId index guarantees at most one row per group.
    private async Task<GroupImmichSettings?> GetCurrentAsync(bool tracked, CancellationToken ct)
    {
        List<GroupImmichSettings> rows = tracked
            ? await _repository.ListAsync<GroupImmichSettings>(s => true, ct)
            : await _repository.ListAsNoTrackingAsync<GroupImmichSettings>(s => true, ct);
        return rows.FirstOrDefault();
    }

    // Settings changes may point at a different Immich server; drop the cached connection
    // and people/tag lookups.
    private void InvalidateLookupCaches(Guid? groupId)
    {
        _cache.Remove(ImmichCacheKeys.Connection(groupId));
        _cache.Remove(ImmichCacheKeys.People(groupId));
        _cache.Remove(ImmichCacheKeys.Tags(groupId));
    }

    private static string MaskApiKey(string apiKey)
    {
        return apiKey.Length > 4 ? $"••••{apiKey[^4..]}" : "••••";
    }
}
