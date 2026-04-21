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

    [Fact]
    public async Task ResponseShouldContainXFrameOptionsHeader()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options header is missing from the response.");

        IEnumerable<string> values = response.Headers.GetValues("X-Frame-Options");
        string firstValue = values.First();
        Assert.True(firstValue is "DENY" or "SAMEORIGIN", "X-Frame-Options should be DENY or SAMEORIGIN");
    }

    [Fact]
    public async Task ResponseShouldContainContentSecurityPolicyHeader()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.True(response.Headers.Contains("Content-Security-Policy"),
            "Content-Security-Policy header is missing from the response.");

        IEnumerable<string> values = response.Headers.GetValues("Content-Security-Policy");
        Assert.False(string.IsNullOrEmpty(values.First()));
    }

    [Fact]
    public async Task ApiEndpointShouldContainXContentTypeOptionsHeader()
    {
        HttpClient client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        HttpResponseMessage response = await client.GetAsync("/api/contacts");

        Assert.True(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options header is missing from the API response.");

        IEnumerable<string> values = response.Headers.GetValues("X-Content-Type-Options");
        Assert.Contains("nosniff", values);
    }
}
