using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Web.Controllers;
using System.Reflection;

namespace Rvnx.CRM.Tests.Controllers;

public class AccountControllerTests
{
    [Fact]
    public void LogoutShouldHaveHttpPostAndValidateAntiForgeryTokenAttributes()
    {
        MethodInfo? methodInfo = typeof(AccountController).GetMethod("Logout");
        Assert.NotNull(methodInfo);

        object? httpPostAttribute = methodInfo!.GetCustomAttributes(typeof(HttpPostAttribute), false).FirstOrDefault();
        object? validateAntiForgeryTokenAttribute = methodInfo!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), false).FirstOrDefault();

        Assert.NotNull(httpPostAttribute);
        Assert.NotNull(validateAntiForgeryTokenAttribute);
    }

    [Fact]
    public void LogoutShouldReturnSignOutResult()
    {
        AccountController controller = new();

        IActionResult result = controller.Logout();

        SignOutResult signOutResult = Assert.IsType<SignOutResult>(result);
        Assert.Contains(CookieAuthenticationDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
        Assert.Contains(OpenIdConnectDefaults.AuthenticationScheme, signOutResult.AuthenticationSchemes);
        Assert.Equal("/", signOutResult.Properties!.RedirectUri);
    }

    [Fact]
    public void LoginShouldDefaultToRootWhenReturnUrlIsExternal()
    {
        AccountController controller = new();
        string externalUrl = "https://evil.com/login-callback";

        Mock<IUrlHelper> urlHelperMock = new();
        urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
            .Returns(false);

        controller.Url = urlHelperMock.Object;

        IActionResult result = controller.Login(externalUrl);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
        Assert.Equal("/", challengeResult.Properties!.RedirectUri);
    }

    [Fact]
    public void LoginShouldUseReturnUrlWhenValidLocalUrlInSafelist()
    {
        AccountController controller = new();
        string localUrl = "/Home/Index";

        Mock<IUrlHelper> urlHelperMock = new();
        urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
            .Returns(true);

        controller.Url = urlHelperMock.Object;

        IActionResult result = controller.Login(localUrl);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
        Assert.Equal(localUrl, challengeResult.Properties!.RedirectUri);
    }

    [Fact]
    public void LoginShouldDefaultToRootWhenLocalUrlNotInSafelist()
    {
        AccountController controller = new();
        string localUrl = "/UnauthorizedPath/Secret";

        Mock<IUrlHelper> urlHelperMock = new();
        urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
            .Returns(true);

        controller.Url = urlHelperMock.Object;

        IActionResult result = controller.Login(localUrl);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(OpenIdConnectDefaults.AuthenticationScheme, challengeResult.AuthenticationSchemes[0]);
        Assert.Equal("/", challengeResult.Properties!.RedirectUri);
    }

    [Theory]
    [InlineData("/Contacts")]
    [InlineData("/Contacts/")]
    [InlineData("/Contacts/Details/123")]
    [InlineData("/Contacts?page=1")]
    public void LoginShouldAllowVariationsOfSafelistedPaths(string localUrl)
    {
        AccountController controller = new();

        Mock<IUrlHelper> urlHelperMock = new();
        urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
            .Returns(true);

        controller.Url = urlHelperMock.Object;

        IActionResult result = controller.Login(localUrl);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(localUrl, challengeResult.Properties!.RedirectUri);
    }

    [Theory]
    [InlineData("/Contactsss")]
    [InlineData("/Contact")]
    public void LoginShouldRejectPartialMatchesOrExtensionsOfSafelistPrefixes(string localUrl)
    {
        AccountController controller = new();

        Mock<IUrlHelper> urlHelperMock = new();
        urlHelperMock.Setup(x => x.IsLocalUrl(It.IsAny<string>()))
            .Returns(true);

        controller.Url = urlHelperMock.Object;

        IActionResult result = controller.Login(localUrl);

        ChallengeResult challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal("/", challengeResult.Properties!.RedirectUri);
    }
}