using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.API.Services;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;

namespace Rvnx.CRM.Tests.Services;

public class ApiTokenCurrentUserServiceTests
{
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<IServiceProvider> _serviceProviderMock;
    private readonly Mock<ILogger<ApiTokenCurrentUserService>> _loggerMock;
    private readonly Mock<IApiTokenService> _tokenServiceMock;
    private readonly Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private readonly Mock<IServiceScope> _serviceScopeMock;
    private readonly Mock<IRepository> _repositoryMock;

    public ApiTokenCurrentUserServiceTests()
    {
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _serviceProviderMock = new Mock<IServiceProvider>();
        _loggerMock = new Mock<ILogger<ApiTokenCurrentUserService>>();
        _tokenServiceMock = new Mock<IApiTokenService>();
        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeMock = new Mock<IServiceScope>();
        _repositoryMock = new Mock<IRepository>();

        _serviceProviderMock.Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(_serviceScopeFactoryMock.Object);

        _serviceScopeFactoryMock.Setup(x => x.CreateScope())
            .Returns(_serviceScopeMock.Object);

        Mock<IServiceProvider> scopeServiceProviderMock = new();
        scopeServiceProviderMock.Setup(x => x.GetService(typeof(IApiTokenService)))
            .Returns(_tokenServiceMock.Object);
        scopeServiceProviderMock.Setup(x => x.GetService(typeof(IRepository)))
            .Returns(_repositoryMock.Object);

        _serviceScopeMock.Setup(x => x.ServiceProvider)
            .Returns(scopeServiceProviderMock.Object);
    }

    [Fact]
    public void PropertiesWithValidTokenShouldPopulateUserContext()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        string rawToken = "crm_validtoken123";

        ApiToken token = new()
        {
            UserId = userId,
            GroupId = groupId,
            Name = "Integration Test",
            TokenHash = "testhash",
            TokenPrefix = "crm_vali"
        };

        DefaultHttpContext context = new();
        context.Request.Headers["Authorization"] = $"Bearer {rawToken}";
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        _tokenServiceMock.Setup(x => x.ResolveTokenAsync(rawToken))
            .ReturnsAsync(token);

        ApiTokenCurrentUserService sut = new(
            _httpContextAccessorMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object);

        bool isAuthenticated = sut.IsAuthenticated;
        Guid? resolvedUserId = sut.UserId;
        Guid? resolvedGroupId = sut.GroupId;
        string? resolvedUserName = sut.UserName;

        Assert.True(isAuthenticated);
        Assert.Equal(userId, resolvedUserId);
        Assert.Equal(groupId, resolvedGroupId);
        Assert.Equal("Integration Test", resolvedUserName);

        _tokenServiceMock.Verify(x => x.ResolveTokenAsync(rawToken), Times.Once);
        _repositoryMock.Verify(x => x.UpdateAsync(token, default), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public void PropertiesWithMissingTokenShouldNotBeAuthenticated()
    {
        DefaultHttpContext context = new();
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(context);

        ApiTokenCurrentUserService sut = new(
            _httpContextAccessorMock.Object,
            _serviceProviderMock.Object,
            _loggerMock.Object);

        bool isAuthenticated = sut.IsAuthenticated;
        Guid? resolvedUserId = sut.UserId;

        Assert.False(isAuthenticated);
        Assert.Null(resolvedUserId);
        _tokenServiceMock.Verify(x => x.ResolveTokenAsync(It.IsAny<string>()), Times.Never);
    }
}
