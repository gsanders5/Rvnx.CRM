using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid groupId = Guid.NewGuid();
        string name = "Test Token";
        DateTime? expiresAt = DateTime.UtcNow.AddDays(30);

        ApiToken? capturedToken = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<ApiToken>(), default))
            .Callback<ApiToken, CancellationToken>((t, _) => capturedToken = t);

        // Act
        var (token, rawToken) = await _sut.CreateTokenAsync(userId, groupId, name, expiresAt);

        // Assert
        token.Should().NotBeNull();
        rawToken.Should().NotBeNullOrEmpty();
        rawToken.Should().StartWith("crm_");

        capturedToken.Should().NotBeNull();
        capturedToken!.UserId.Should().Be(userId);
        capturedToken.GroupId.Should().Be(groupId);
        capturedToken.Name.Should().Be(name);
        capturedToken.TokenHash.Should().NotBe(rawToken);
        capturedToken.TokenPrefix.Should().Be(rawToken.Substring(0, 8));
        capturedToken.ExpiresAt.Should().Be(expiresAt);
        capturedToken.IsActive.Should().BeTrue();

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ApiToken>(), default), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Once);
    }
}
