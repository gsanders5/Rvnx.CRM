using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

public class ApiTokenTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ApiTokenService _sut;

    public ApiTokenTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _sut = new ApiTokenService(_repositoryMock.Object);
    }

    [Fact]
    public async Task CreateTokenAsyncShouldGenerateValidTokenAndStoreHash()
    {
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        string name = "Test Token";
        DateTime? expiresAt = DateTime.UtcNow.AddDays(30);

        ApiToken? capturedToken = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ApiToken>(), default))
            .Callback<ApiToken, CancellationToken>((t, _) => capturedToken = t);

        (ApiToken token, string rawToken) = await _sut.CreateTokenAsync(userId, groupId, name, expiresAt);

        Assert.NotNull(token);
        Assert.NotNull(rawToken);
        Assert.NotEmpty(rawToken);
        Assert.StartsWith("crm_", rawToken);

        Assert.NotNull(capturedToken);
        Assert.Equal(userId, capturedToken!.UserId);
        Assert.Equal(groupId, capturedToken.GroupId);
        Assert.Equal(name, capturedToken.Name);
        Assert.NotEqual(rawToken, capturedToken.TokenHash);
        Assert.Equal(rawToken[..8], capturedToken.TokenPrefix);
        Assert.Equal(expiresAt, capturedToken.ExpiresAt);
        Assert.True(capturedToken.IsActive);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ApiToken>(), default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}