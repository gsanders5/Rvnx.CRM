using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rvnx.CRM.Infrastructure.Services;

public class ImmichService : IImmichService
{
    public const string ConfigSection = "Immich";

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);
    private const string TagsAllCacheKey = "immich:tags:all";

    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ImmichService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Action<ILogger, int, string, Exception?> LogHttpFailure =
        LoggerMessage.Define<int, string>(
            LogLevel.Warning,
            new EventId(1, nameof(LogHttpFailure)),
            "Immich API call to {Path} failed with status {Status}");

    private static readonly Action<ILogger, string, Exception?> LogRequestException =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(2, nameof(LogRequestException)),
            "Immich API call to {Path} threw");

    public ImmichService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<ImmichService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _cache = cache;
        _logger = logger;
    }

    public bool IsEnabled
    {
        get
        {
            if (_httpClient.BaseAddress is null)
            {
                return false;
            }

            IConfigurationSection cfg = _configuration.GetSection(ConfigSection);
            return bool.TryParse(cfg["Enabled"], out bool enabled) && enabled;
        }
    }

    public async Task<IReadOnlyList<ImmichOptionDto>> SearchPeopleAsync(string? query, CancellationToken ct)
    {
        if (!IsEnabled || string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        string key = $"immich:people:{query.Trim().ToLowerInvariant()}";
        if (_cache.TryGetValue(key, out IReadOnlyList<ImmichOptionDto>? cached) && cached is not null)
        {
            return cached;
        }

        string url = $"search/person?name={Uri.EscapeDataString(query)}&withHidden=false";
        List<ImmichPerson>? people = await GetJsonAsync<List<ImmichPerson>>(url, ct);
        if (people is null)
        {
            return [];
        }

        List<ImmichOptionDto> results = people
            .Where(p => !string.IsNullOrWhiteSpace(p.Name))
            .Select(p => new ImmichOptionDto(p.Id, p.Name!))
            .ToList();

        _cache.Set(key, (IReadOnlyList<ImmichOptionDto>)results, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        });
        return results;
    }

    public async Task<IReadOnlyList<ImmichOptionDto>> SearchTagsAsync(string? query, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return [];
        }

        IReadOnlyList<ImmichTag> tags = await GetAllTagsAsync(ct);
        IEnumerable<ImmichTag> filtered = string.IsNullOrWhiteSpace(query)
            ? tags
            : tags.Where(t => ContainsIgnoreCase(t.Value, query) || ContainsIgnoreCase(t.Name, query));

        return filtered
            .Select(t => new ImmichOptionDto(t.Id, t.Value ?? t.Name ?? string.Empty))
            .Where(o => !string.IsNullOrWhiteSpace(o.Text))
            .ToList();
    }

    public async Task<IReadOnlyList<ImmichAssetDto>> GetAssetsAsync(Guid? personId, Guid? tagId, int maxResults, CancellationToken ct)
    {
        if (!IsEnabled || (personId is null && tagId is null) || maxResults <= 0)
        {
            return [];
        }

        List<Task<List<ImmichAsset>>> queries = [];
        if (personId.HasValue)
        {
            queries.Add(SearchAssetsAsync(new { personIds = new[] { personId.Value }, type = "IMAGE", size = maxResults }, ct));
        }
        if (tagId.HasValue)
        {
            queries.Add(SearchAssetsAsync(new { tagIds = new[] { tagId.Value }, type = "IMAGE", size = maxResults }, ct));
        }

        List<ImmichAsset>[] results = await Task.WhenAll(queries);

        HashSet<Guid> seen = [];
        List<ImmichAssetDto> output = new(maxResults);
        foreach (List<ImmichAsset> batch in results)
        {
            foreach (ImmichAsset asset in batch)
            {
                if (output.Count >= maxResults)
                {
                    return output;
                }
                if (seen.Add(asset.Id))
                {
                    output.Add(new ImmichAssetDto(asset.Id, asset.OriginalFileName ?? string.Empty, asset.Type ?? "IMAGE"));
                }
            }
        }
        return output;
    }

    public async Task<ImmichMediaPayload?> GetThumbnailAsync(Guid assetId, CancellationToken ct)
    {
        return await FetchMediaAsync($"assets/{assetId}/thumbnail?size=preview", ct);
    }

    public async Task<ImmichMediaPayload?> GetOriginalAsync(Guid assetId, CancellationToken ct)
    {
        return await FetchMediaAsync($"assets/{assetId}/original", ct);
    }

    private async Task<ImmichMediaPayload?> FetchMediaAsync(string relativeUrl, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            return null;
        }

        HttpResponseMessage? response = null;
        try
        {
            response = await _httpClient.GetAsync(relativeUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpFailure(_logger, (int)response.StatusCode, relativeUrl, null);
                response.Dispose();
                return null;
            }

            string contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            Stream content = await response.Content.ReadAsStreamAsync(ct);
            return new ImmichMediaPayload(response, content, contentType);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            LogRequestException(_logger, relativeUrl, ex);
            response?.Dispose();
            return null;
        }
    }

    private async Task<IReadOnlyList<ImmichTag>> GetAllTagsAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(TagsAllCacheKey, out IReadOnlyList<ImmichTag>? cached) && cached is not null)
        {
            return cached;
        }

        List<ImmichTag>? tags = await GetJsonAsync<List<ImmichTag>>("tags", ct);
        IReadOnlyList<ImmichTag> result = tags ?? [];

        if (tags is not null)
        {
            _cache.Set(TagsAllCacheKey, result, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl
            });
        }
        return result;
    }

    private async Task<List<ImmichAsset>> SearchAssetsAsync(object body, CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.PostAsJsonAsync("search/metadata", body, JsonOptions, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpFailure(_logger, (int)response.StatusCode, "search/metadata", null);
                return [];
            }

            ImmichSearchResponse? parsed = await response.Content.ReadFromJsonAsync<ImmichSearchResponse>(JsonOptions, ct);
            return parsed?.Assets?.Items ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            LogRequestException(_logger, "search/metadata", ex);
            return [];
        }
    }

    private async Task<T?> GetJsonAsync<T>(string relativeUrl, CancellationToken ct)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(relativeUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpFailure(_logger, (int)response.StatusCode, relativeUrl, null);
                return default;
            }
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            LogRequestException(_logger, relativeUrl, ex);
            return default;
        }
    }

    private static bool ContainsIgnoreCase(string? haystack, string needle)
    {
        return !string.IsNullOrEmpty(haystack) && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private sealed record ImmichPerson(Guid Id, string? Name);

    private sealed record ImmichTag(Guid Id, string? Name, string? Value);

    private sealed class ImmichSearchResponse
    {
        [JsonPropertyName("assets")]
        public ImmichAssetBucket? Assets { get; set; }
    }

    private sealed class ImmichAssetBucket
    {
        [JsonPropertyName("items")]
        public List<ImmichAsset>? Items { get; set; }
    }

    private sealed class ImmichAsset
    {
        [JsonPropertyName("id")]
        public Guid Id { get; set; }

        [JsonPropertyName("originalFileName")]
        public string? OriginalFileName { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
}
