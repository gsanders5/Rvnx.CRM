using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Infrastructure.Services;
using System.Net;
using System.Text;

namespace Rvnx.CRM.Tests.Services;

public class ImmichServiceTests
{
    private const string BaseUrl = "https://immich.example.com/api/";

    private static IConfiguration BuildConfig(bool enabled)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Immich:Enabled"] = enabled ? "true" : "false",
                ["Immich:BaseUrl"] = BaseUrl,
                ["Immich:ApiKey"] = "test-api-key"
            })
            .Build();
    }

    private static (ImmichService Service, Mock<HttpMessageHandler> Handler, HttpClient Client) CreateService(
        bool enabled = true, bool setBaseAddress = true)
    {
        Mock<HttpMessageHandler> handler = new();
        HttpClient client = new(handler.Object);
        if (setBaseAddress)
        {
            client.BaseAddress = new Uri(BaseUrl);
            client.DefaultRequestHeaders.Add("x-api-key", "test-api-key");
        }
        ImmichService service = new(client, BuildConfig(enabled), new MemoryCache(new MemoryCacheOptions()), NullLogger<ImmichService>.Instance);
        return (service, handler, client);
    }

    private static void SetupJson(Mock<HttpMessageHandler> handler, string pathSuffix, HttpMethod method, string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.Method == method && r.RequestUri != null && r.RequestUri.AbsoluteUri.EndsWith(pathSuffix, StringComparison.Ordinal)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }

    [Fact]
    public void IsEnabledReturnsFalseWhenConfigDisabled()
    {
        (ImmichService service, _, HttpClient client) = CreateService(enabled: false);
        using (client)
        {
            Assert.False(service.IsEnabled);
        }
    }

    [Fact]
    public void IsEnabledReturnsFalseWhenBaseAddressMissing()
    {
        (ImmichService service, _, HttpClient client) = CreateService(enabled: true, setBaseAddress: false);
        using (client)
        {
            Assert.False(service.IsEnabled);
        }
    }

    [Fact]
    public async Task GetAllPeopleAsyncReturnsEmptyWhenDisabled()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService(enabled: false);
        using (client)
        {
            IReadOnlyList<ImmichOptionDto> result = await service.GetAllPeopleAsync(CancellationToken.None);

            Assert.Empty(result);
            handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }

    [Fact]
    public async Task GetAllPeopleAsyncSendsApiKeyHeader()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            HttpRequestMessage? captured = null;
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => captured = req)
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"people\":[]}", Encoding.UTF8, "application/json")
                });

            await service.GetAllPeopleAsync(CancellationToken.None);

            Assert.NotNull(captured);
            Assert.True(captured!.Headers.Contains("x-api-key"));
        }
    }

    [Fact]
    public async Task GetAllPeopleAsyncMapsResponseAndSortsByName()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();
            string json = $"{{\"people\":[{{\"id\":\"{id1}\",\"name\":\"Zebra\"}},{{\"id\":\"{id2}\",\"name\":\"Alice\"}}],\"hasNextPage\":false}}";
            SetupJson(handler, "/people?withHidden=false&size=1000", HttpMethod.Get, json);

            IReadOnlyList<ImmichOptionDto> result = await service.GetAllPeopleAsync(CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal("Alice", result[0].Text);
            Assert.Equal("Zebra", result[1].Text);
        }
    }

    [Fact]
    public async Task GetAllTagsAsyncReturnsAllMapped()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            Guid id1 = Guid.NewGuid();
            Guid id2 = Guid.NewGuid();
            string json = $"[{{\"id\":\"{id1}\",\"name\":\"Bob\",\"value\":\"Bob\"}},{{\"id\":\"{id2}\",\"name\":\"Alice\",\"value\":\"Alice\"}}]";
            SetupJson(handler, "/tags", HttpMethod.Get, json);

            IReadOnlyList<ImmichOptionDto> result = await service.GetAllTagsAsync(CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Equal("Alice", result[0].Text);
            Assert.Equal("Bob", result[1].Text);
        }
    }

    [Fact]
    public async Task GetAssetsAsyncReturnsEmptyWhenBothIdsNull()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            IReadOnlyList<ImmichAssetDto> result = await service.GetAssetsAsync(null, null, 10, CancellationToken.None);

            Assert.Empty(result);
            handler.Protected().Verify("SendAsync", Times.Never(), ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>());
        }
    }

    [Fact]
    public async Task GetAssetsAsyncDedupesAcrossCalls()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            Guid sharedId = Guid.NewGuid();
            Guid uniqueId = Guid.NewGuid();
            string body1 = $"{{\"assets\":{{\"items\":[{{\"id\":\"{sharedId}\",\"originalFileName\":\"a.jpg\",\"type\":\"IMAGE\"}},{{\"id\":\"{uniqueId}\",\"originalFileName\":\"b.jpg\",\"type\":\"IMAGE\"}}]}}}}";
            string body2 = $"{{\"assets\":{{\"items\":[{{\"id\":\"{sharedId}\",\"originalFileName\":\"a.jpg\",\"type\":\"IMAGE\"}}]}}}}";

            int callCount = 0;
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() =>
                {
                    string content = Interlocked.Increment(ref callCount) == 1 ? body1 : body2;
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(content, Encoding.UTF8, "application/json")
                    };
                });

            IReadOnlyList<ImmichAssetDto> result = await service.GetAssetsAsync(Guid.NewGuid(), Guid.NewGuid(), 24, CancellationToken.None);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.Id == sharedId);
            Assert.Contains(result, r => r.Id == uniqueId);
        }
    }

    [Fact]
    public async Task GetAssetsAsyncRespectsMaxResultsCap()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            List<string> items = [];
            for (int i = 0; i < 30; i++)
            {
                items.Add($"{{\"id\":\"{Guid.NewGuid()}\",\"originalFileName\":\"img{i}.jpg\",\"type\":\"IMAGE\"}}");
            }
            string body = $"{{\"assets\":{{\"items\":[{string.Join(",", items)}]}}}}";
            SetupJson(handler, "/search/metadata", HttpMethod.Post, body);

            IReadOnlyList<ImmichAssetDto> result = await service.GetAssetsAsync(Guid.NewGuid(), null, 24, CancellationToken.None);

            Assert.Equal(24, result.Count);
        }
    }

    [Fact]
    public async Task GetAllPeopleAsyncReturnsEmptyOnUnauthorized()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            SetupJson(handler, "withHidden=false&size=1000", HttpMethod.Get, "{}", HttpStatusCode.Unauthorized);

            IReadOnlyList<ImmichOptionDto> result = await service.GetAllPeopleAsync(CancellationToken.None);

            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task GetAllPeopleAsyncReturnsEmptyOnTimeout()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            IReadOnlyList<ImmichOptionDto> result = await service.GetAllPeopleAsync(CancellationToken.None);

            Assert.Empty(result);
        }
    }

    [Fact]
    public async Task GetThumbnailAsyncReturnsContentTypeFromResponse()
    {
        (ImmichService service, Mock<HttpMessageHandler> handler, HttpClient client) = CreateService();
        using (client)
        {
            byte[] bytes = Encoding.UTF8.GetBytes("fake-jpeg-bytes");
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(r => r.RequestUri != null && r.RequestUri.AbsoluteUri.Contains("/thumbnail", StringComparison.Ordinal)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new ByteArrayContent(bytes) { Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/webp") } }
                });

            ImmichMediaPayload? result = await service.GetThumbnailAsync(Guid.NewGuid(), CancellationToken.None);

            Assert.NotNull(result);
            Assert.Equal("image/webp", result!.ContentType);
            result.Response.Dispose();
        }
    }
}
