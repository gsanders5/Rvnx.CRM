using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers
{
    public class AccountControllerTests
    {
        [Fact]
        public void Login_ShouldDefaultToRoot_WhenReturnUrlIsExternal()
        {
            // Arrange
            AccountController controller = new();
            string externalUrl = "https://evil.com/login-callback";

            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns((string url) => !string.IsNullOrEmpty(url) && url.StartsWith("/"));

            controller.Url = urlHelperMock.Object;

            // Act
            IActionResult result = controller.Login(externalUrl);

            // Assert
            ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void Login_ShouldUseReturnUrl_WhenValidLocalUrl()
        {
            // Arrange
            AccountController controller = new();
            string localUrl = "/Home/Index";

            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns((string url) => !string.IsNullOrEmpty(url) && url.StartsWith("/"));

            controller.Url = urlHelperMock.Object;

            // Act
            IActionResult result = controller.Login(localUrl);

            // Assert
            ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
            Assert.Equal(localUrl, challengeResult.Properties!.RedirectUri);
        }
    }
}
