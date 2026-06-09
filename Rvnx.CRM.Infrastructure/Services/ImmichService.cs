using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Immich;
using Rvnx.CRM.Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rvnx.CRM.Infrastructure.Services;

public class ImmichService : IImmichService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(5);

    private readonly HttpClient _httpClient;
    private readonly IImmichSettingsService _settingsService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ImmichService> _logger;

    // Dedupes connection lookups within this service instance (typed HTTP clients are
    // transient); cross-instance reuse comes from the settings service's memory cache.
    private Task<ImmichConnectionDto?>? _connectionTask;

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

    // Immich /people pages at 500 by default, max 1000. Above that we log and let the user know the dropdown is truncated.
    private const int PeoplePageSize = 1000;

    private static readonly Action<ILogger, Exception?> LogPeoplePaginationOverflow =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(3, nameof(LogPeoplePaginationOverflow)),
            "Immich returned hasNextPage=true for /people; the Select2 dropdown will be truncated at " + nameof(PeoplePageSize) + " entries.");

    public ImmichService(
        HttpClient httpClient,
        IImmichSettingsService settingsService,
        IMemoryCache cache,
        ILogger<ImmichService> logger)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsEnabledAsync(CancellationToken ct)
    {
        return await GetActiveConnectionAsync() is not null;
    }

    public async Task<string?> GetWebBaseUrlAsync(CancellationToken ct)
    {
        ImmichConnectionDto? connection = await GetActiveConnectionAsync();
        if (connection is null)
        {
            return null;
        }

        string raw = connection.BaseUrl.TrimEnd('/');
        const string apiSuffix = "/api";
        return raw.EndsWith(apiSuffix, StringComparison.OrdinalIgnoreCase)
            ? raw[..^apiSuffix.Length]
            : raw;
    }

    public async Task<IReadOnlyList<ImmichOptionDto>> GetAllPeopleAsync(CancellationToken ct)
    {
        ImmichConnectionDto? connection = await GetActiveConnectionAsync();
        if (connection is null)
        {
            return [];
        }

        string cacheKey = ImmichCacheKeys.People(connection.GroupId);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ImmichOptionDto>? cached) && cached is not null)
        {
            return cached;
        }

        ImmichPeopleResponse? response = await GetJsonAsync<ImmichPeopleResponse>(connection, $"people?withHidden=false&size={PeoplePageSize}", ct);
        if (response is null)
        {
            return [];
        }

        if (response.HasNextPage == true)
        {
            LogPeoplePaginationOverflow(_logger, null);
        }

        IReadOnlyList<ImmichOptionDto> result = response.People is null
            ? []
            : response.People
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .Select(p => new ImmichOptionDto(p.Id, p.Name!))
                .OrderBy(o => o.Text, StringComparer.OrdinalIgnoreCase)
                .ToList();

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        });
        return result;
    }

    public async Task<IReadOnlyList<ImmichOptionDto>> GetAllTagsAsync(CancellationToken ct)
    {
        ImmichConnectionDto? connection = await GetActiveConnectionAsync();
        if (connection is null)
        {
            return [];
        }

        string cacheKey = ImmichCacheKeys.Tags(connection.GroupId);
        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<ImmichOptionDto>? cached) && cached is not null)
        {
            return cached;
        }

        List<ImmichTag>? tags = await GetJsonAsync<List<ImmichTag>>(connection, "tags", ct);
        if (tags is null)
        {
            return [];
        }

        IReadOnlyList<ImmichOptionDto> result = tags
            .Select(t => new ImmichOptionDto(t.Id, t.Value ?? t.Name ?? string.Empty))
            .Where(o => !string.IsNullOrWhiteSpace(o.Text))
            .OrderBy(o => o.Text, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl
        });
        return result;
    }

    public async Task<IReadOnlyList<ImmichAssetDto>> GetAssetsAsync(Guid? personId, Guid? tagId, int maxResults, CancellationToken ct)
    {
        ImmichConnectionDto? connection = await GetActiveConnectionAsync();
        if (connection is null || (personId is null && tagId is null) || maxResults <= 0)
        {
            return [];
        }

        List<Task<List<ImmichAsset>>> queries = [];
        if (personId.HasValue)
        {
            queries.Add(SearchAssetsAsync(connection, new { personIds = new[] { personId.Value }, type = "IMAGE", size = maxResults }, ct));
        }
        if (tagId.HasValue)
        {
            queries.Add(SearchAssetsAsync(connection, new { tagIds = new[] { tagId.Value }, type = "IMAGE", size = maxResults }, ct));
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

    // Returns the group's connection settings only when the integration is usable
    // (enabled and carrying a valid absolute base URL); null otherwise.
    private async Task<ImmichConnectionDto?> GetActiveConnectionAsync()
    {
        // The lazily-shared task deliberately ignores per-call cancellation tokens so one
        // cancelled caller can't poison the cached lookup for the rest of the request.
        _connectionTask ??= _settingsService.GetConnectionAsync(CancellationToken.None);
        ImmichConnectionDto? connection = await _connectionTask;

        if (connection is null
            || !connection.Enabled
            || !Uri.TryCreate(NormalizeBaseUrl(connection.BaseUrl), UriKind.Absolute, out _))
        {
            return null;
        }
        return connection;
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        return baseUrl.Trim().TrimEnd('/') + "/";
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, ImmichConnectionDto connection, string relativeUrl)
    {
        // The API key is attached per request (not on the shared HttpClient) because each
        // group can point at a different Immich server with different credentials.
        HttpRequestMessage request = new(method, new Uri(new Uri(NormalizeBaseUrl(connection.BaseUrl)), relativeUrl));
        if (!string.IsNullOrWhiteSpace(connection.ApiKey))
        {
            request.Headers.Add("x-api-key", connection.ApiKey);
        }
        return request;
    }

    private async Task<ImmichMediaPayload?> FetchMediaAsync(string relativeUrl, CancellationToken ct)
    {
        ImmichConnectionDto? connection = await GetActiveConnectionAsync();
        if (connection is null)
        {
            return null;
        }

        HttpResponseMessage? response = null;
        try
        {
            using HttpRequestMessage request = CreateRequest(HttpMethod.Get, connection, relativeUrl);
            response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
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

    private async Task<List<ImmichAsset>> SearchAssetsAsync(ImmichConnectionDto connection, object body, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(HttpMethod.Post, connection, "search/metadata");
            request.Content = JsonContent.Create(body, options: JsonOptions);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
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

    private async Task<T?> GetJsonAsync<T>(ImmichConnectionDto connection, string relativeUrl, CancellationToken ct)
    {
        try
        {
            using HttpRequestMessage request = CreateRequest(HttpMethod.Get, connection, relativeUrl);
            using HttpResponseMessage response = await _httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                LogHttpFailure(_logger, (int)response.StatusCode, relativeUrl, null);
                return default;
            }
            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            LogRequestException(_logger, relativeUrl, ex);
            return default;
        }
    }

    private sealed record ImmichPerson(Guid Id, string? Name);

    private sealed class ImmichPeopleResponse
    {
        [JsonPropertyName("people")]
        public List<ImmichPerson>? People { get; set; }

        [JsonPropertyName("hasNextPage")]
        public bool? HasNextPage { get; set; }
    }

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
