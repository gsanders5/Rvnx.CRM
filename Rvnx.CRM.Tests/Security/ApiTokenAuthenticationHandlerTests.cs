using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Rvnx.CRM.API.Authentication;
using Rvnx.CRM.Core.Interfaces;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Rvnx.CRM.Tests.Security;

public class ApiTokenAuthenticationHandlerTests
{
    private readonly Mock<IOptionsMonitor<ApiTokenAuthenticationOptions>> _optionsMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<UrlEncoder> _encoderMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;

    public ApiTokenAuthenticationHandlerTests()
    {
        _optionsMock = new Mock<IOptionsMonitor<ApiTokenAuthenticationOptions>>();
        _optionsMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new ApiTokenAuthenticationOptions());

        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        _encoderMock = new Mock<UrlEncoder>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithoutAuthorizationHeaderReturnsNoResult()
    {
        // Arrange
        DefaultHttpContext context = new();

        ApiTokenAuthenticationHandler sut = new(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        // Act
        AuthenticateResult result = await sut.AuthenticateAsync();

        // Assert
        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithInvalidTokenReturnsFail()
    {
        // Arrange
        DefaultHttpContext context = new();
        context.Request.Headers["Authorization"] = "Bearer invalid_token";

        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);

        ApiTokenAuthenticationHandler sut = new(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        // Act
        AuthenticateResult result = await sut.AuthenticateAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Equal("Invalid or missing API token.", result.Failure!.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithValidTokenReturnsSuccess()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        DefaultHttpContext context = new();
        context.Request.Headers["Authorization"] = "Bearer crm_validtoken123";

        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.UserName).Returns("Test User");

        ApiTokenAuthenticationHandler sut = new(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        // Act
        AuthenticateResult result = await sut.AuthenticateAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.True(result.Principal!.Identity!.IsAuthenticated);
        Assert.Equal(userId.ToString(), result.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal("Test User", result.Principal.FindFirst(ClaimTypes.Name)!.Value);
    }
}