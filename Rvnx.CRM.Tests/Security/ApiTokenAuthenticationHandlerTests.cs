using FluentAssertions;
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
        var context = new DefaultHttpContext();

        var sut = new ApiTokenAuthenticationHandler(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        // Act
        var result = await sut.AuthenticateAsync();

        // Assert
        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithInvalidTokenReturnsFail()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer invalid_token";

        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);

        var sut = new ApiTokenAuthenticationHandler(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        // Act
        var result = await sut.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
        result.Failure!.Message.Should().Be("Invalid or missing API token.");
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithValidTokenReturnsSuccess()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer crm_validtoken123";

        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.UserName).Returns("Test User");

        var sut = new ApiTokenAuthenticationHandler(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        // Act
        var result = await sut.AuthenticateAsync();

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Principal.Should().NotBeNull();
        result.Principal!.Identity!.IsAuthenticated.Should().BeTrue();
        result.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value.Should().Be(userId.ToString());
        result.Principal.FindFirst(ClaimTypes.Name)!.Value.Should().Be("Test User");
    }
}
