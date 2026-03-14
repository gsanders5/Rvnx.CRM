using Microsoft.AspNetCore.Mvc.Testing;
using Rvnx.CRM.Web;

namespace Rvnx.CRM.Tests.Security;

public class SecurityHeadersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SecurityHeadersTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResponseShouldContainXContentTypeOptionsHeader()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options header is missing from the response.");

        IEnumerable<string> values = response.Headers.GetValues("X-Content-Type-Options");
        Assert.Contains("nosniff", values);
    }
}