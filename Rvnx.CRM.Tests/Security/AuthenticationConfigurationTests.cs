using Microsoft.Extensions.Configuration;

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
}