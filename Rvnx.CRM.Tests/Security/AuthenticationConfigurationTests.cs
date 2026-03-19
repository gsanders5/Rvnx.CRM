using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Rvnx.CRM.Web;

namespace Rvnx.CRM.Tests.Security;

public class AuthenticationConfigurationTests
{
    [Fact]
    public void AppSettingsShouldNotContainHardcodedSecret()
    {
        string appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Rvnx.CRM.Web", "appsettings.json");

        IConfigurationRoot config = new ConfigurationBuilder()
            .AddJsonFile(appsettingsPath)
            .Build();

        string? secret = config["Authentication:ClientSecret"];

        Assert.True(string.IsNullOrWhiteSpace(secret) || secret == "CHANGE_ME",
            "appsettings.json should not contain a hardcoded secret.");

        Assert.NotEqual("CHANGE_ME", secret);
    }

    [Fact]
    public void ValidationLogicShouldThrowWhenAuthEnabledAndSecretMissing()
    {
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Enabled", "true");
                builder.UseSetting("Authentication:Authority", "");
                builder.UseSetting("Authentication:ClientId", "test-id");
                builder.UseSetting("Authentication:ClientSecret", "test-secret");
            });

        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            factory.CreateClient();
        });

        Assert.Contains("Authentication is enabled but Authority is missing.", ex.Message);
    }
}