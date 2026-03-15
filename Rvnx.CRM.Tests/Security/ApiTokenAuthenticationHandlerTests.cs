using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Rvnx.CRM.API.Authentication;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Rvnx.CRM.Tests.Security;

public class ApiTokenAuthenticationHandlerTests
{
    private readonly Mock<IOptionsMonitor<ApiTokenAuthenticationOptions>> _optionsMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<UrlEncoder> _encoderMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<IApiTokenService> _apiTokenServiceMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;

    public ApiTokenAuthenticationHandlerTests()
    {
        _optionsMock = new Mock<IOptionsMonitor<ApiTokenAuthenticationOptions>>();
        _optionsMock.Setup(x => x.Get(It.IsAny<string>())).Returns(new ApiTokenAuthenticationOptions());

        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());

        _encoderMock = new Mock<UrlEncoder>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _serviceProviderMock = new Mock<IServiceProvider>();
        _apiTokenServiceMock = new Mock<IApiTokenService>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeMock.Setup(x => x.ServiceProvider).Returns(() => _serviceProviderMock.Object);

        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeFactoryMock.Setup(x => x.CreateScope()).Returns(_serviceScopeMock.Object);

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);
        _serviceProviderMock.Setup(x => x.GetService(typeof(IApiTokenService))).Returns(_apiTokenServiceMock.Object);
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithoutAuthorizationHeaderReturnsNoResult()
    {
        DefaultHttpContext context = new();

        ApiTokenAuthenticationHandler sut = new(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object,
            _serviceProviderMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        AuthenticateResult result = await sut.AuthenticateAsync();

        Assert.True(result.None);
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithInvalidTokenReturnsFail()
    {
        DefaultHttpContext context = new();
        context.Request.Headers["Authorization"] = "Bearer invalid_token";

        _apiTokenServiceMock.Setup(x => x.ResolveTokenAsync(It.IsAny<string>())).ReturnsAsync((ApiToken?)null);
        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(false);

        ApiTokenAuthenticationHandler sut = new(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object,
            _serviceProviderMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        AuthenticateResult result = await sut.AuthenticateAsync();

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Equal("Invalid or missing API token.", result.Failure!.Message);
    }

    [Fact]
    public async Task HandleAuthenticateAsyncWithValidTokenReturnsSuccess()
    {
        Guid userId = Guid.NewGuid();
        DefaultHttpContext context = new();
        context.Request.Headers["Authorization"] = "Bearer crm_validtoken123";

        _apiTokenServiceMock.Setup(x => x.ResolveTokenAsync("validtoken123")).ReturnsAsync(new ApiToken
        {
            UserId = userId,
            RevokedAt = null,
            ExpiresAt = null
        });

        _currentUserServiceMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _currentUserServiceMock.Setup(x => x.UserName).Returns("Test User");

        ApiTokenAuthenticationHandler sut = new(
            _optionsMock.Object,
            _loggerFactoryMock.Object,
            _encoderMock.Object,
            _currentUserServiceMock.Object,
            _serviceProviderMock.Object);

        await sut.InitializeAsync(new AuthenticationScheme(ApiTokenAuthenticationOptions.DefaultScheme, null, typeof(ApiTokenAuthenticationHandler)), context);

        AuthenticateResult result = await sut.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Principal);
        Assert.True(result.Principal!.Identity!.IsAuthenticated);
        Assert.Equal(userId.ToString(), result.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        Assert.Equal("Test User", result.Principal.FindFirst(ClaimTypes.Name)!.Value);
    }
}