using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Web.Controllers;
using System.Reflection;

namespace Rvnx.CRM.Tests.Controllers
{
    public class AccountControllerTests
    {
        [Fact]
        public void LogoutShouldHaveHttpPostAndValidateAntiForgeryTokenAttributes()
        {
            // Arrange
            MethodInfo? methodInfo = typeof(AccountController).GetMethod("Logout");
            Assert.NotNull(methodInfo);

            // Act
            object? httpPostAttribute = methodInfo!.GetCustomAttributes(typeof(HttpPostAttribute), false).FirstOrDefault();
            object? validateAntiForgeryTokenAttribute = methodInfo!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).FirstOrDefault();

            // Assert
            Assert.NotNull(httpPostAttribute);
            Assert.NotNull(validateAntiForgeryTokenAttribute);
        }

        [Fact]
        public void LogoutShouldReturnSignOutResult()
        {
            // Arrange
            AccountController controller = new();

            // Act
            IActionResult result = controller.Logout();

            // Assert
            SignOutResult signOutResult = Assert.IsType<SignOutResult>(result);
            Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
            Assert.Contains(OpenIdConnectDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
            Assert.Equal("/", signOutResult.Properties!.RedirectUri);
        }

        [Fact]
        public void LoginShouldDefaultToRootWhenReturnUrlIsExternal()
        {
            // Arrange
            AccountController controller = new();
            string externalUrl = "https://evil.com/login-callback";

            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns((string url) => !string.IsNullOrEmpty(url) && url.StartsWith('/'));

            controller.Url = urlHelperMock.Object;

            // Act
            IActionResult result = controller.Login(externalUrl);

            // Assert
            ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
            Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
            Assert.Equal("/", challengeResult.Properties!.RedirectUri);
        }

        [Fact]
        public void LoginShouldUseReturnUrlWhenValidLocalUrl()
        {
            // Arrange
            AccountController controller = new();
            string localUrl = "/Home/Index";

            Mock<IUrlHelper> urlHelperMock = new();
            urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
                .Returns((string url) => !string.IsNullOrEmpty(url) && url.StartsWith('/'));

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
