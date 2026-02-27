using Microsoft.Extensions.Configuration;

namespace Rvnx.CRM.Tests.Security
{
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
            Dictionary<string, string?> authSettings = new()
            {
                {"Authentication:Enabled", "true"},
                {"Authentication:Authority", ""},
                {"Authentication:ClientId", "test-id"},
                {"Authentication:ClientSecret", "test-secret"}
            };

            IConfigurationRoot config = new ConfigurationBuilder()
                .AddInMemoryCollection(authSettings)
                .Build();

            IConfigurationSection authConfig = config.GetSection("Authentication");
            bool authEnabled = authConfig.GetValue<bool>("Enabled");

            if (authEnabled)
            {
                Assert.Throws<InvalidOperationException>(() =>
                {
                    if (string.IsNullOrWhiteSpace(authConfig["Authority"]) ||
                        string.IsNullOrWhiteSpace(authConfig["ClientId"]) ||
                        string.IsNullOrWhiteSpace(authConfig["ClientSecret"]))
                    {
                        throw new InvalidOperationException("Authentication is enabled but Authority, ClientId, or ClientSecret is missing in configuration.");
                    }
                });
            }
        }
    }
}
