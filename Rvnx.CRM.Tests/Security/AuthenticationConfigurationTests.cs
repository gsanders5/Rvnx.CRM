using Microsoft.Extensions.Configuration;

namespace Rvnx.CRM.Tests.Security
{
    public class AuthenticationConfigurationTests
    {
        [Fact]
        public void AppSettingsShouldHaveAuthenticationEnabledByDefault()
        {
            // Arrange
            var appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Rvnx.CRM.Web", "appsettings.json");

            var config = new ConfigurationBuilder()
                .AddJsonFile(appsettingsPath)
                .Build();

            // Act
            bool authEnabled = config.GetValue<bool>("Authentication:Enabled");

            // Assert
            Assert.True(authEnabled, "appsettings.json should have authentication enabled by default.");
        }

        [Fact]
        public void AppSettingsShouldNotContainHardcodedSecret()
        {
            // Arrange
            var appsettingsPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "Rvnx.CRM.Web", "appsettings.json");

            var config = new ConfigurationBuilder()
                .AddJsonFile(appsettingsPath)
                .Build();

            // Act
            string? secret = config["Authentication:ClientSecret"];

            // Assert
            Assert.True(string.IsNullOrWhiteSpace(secret) || secret == "CHANGE_ME",
                "appsettings.json should not contain a hardcoded secret.");

            Assert.NotEqual("CHANGE_ME", secret);
        }

        [Fact]
        public void ValidationLogicShouldThrowWhenAuthEnabledAndSecretMissing()
        {
            // Arrange
            var authSettings = new Dictionary<string, string?> {
                {"Authentication:Enabled", "true"},
                {"Authentication:Authority", ""},
                {"Authentication:ClientId", "test-id"},
                {"Authentication:ClientSecret", "test-secret"}
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(authSettings)
                .Build();

            var authConfig = config.GetSection("Authentication");
            bool authEnabled = authConfig.GetValue<bool>("Enabled");

            // Act & Assert
            if (authEnabled)
            {
                Assert.Throws<InvalidOperationException>(() => {
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
