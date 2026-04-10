using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class FavoriteServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly FavoriteService _service;

    public FavoriteServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new FavoriteService(_repositoryMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task ToggleFavoriteAsyncWhenUserIdNullReturnsFalseAndDoesNothing()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);
        Guid contactId = Guid.NewGuid();

        // Act
        bool result = await _service.ToggleFavoriteAsync(contactId);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.ListAsync(It.IsAny<Expression<Func<ContactFavorite, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()), Times.Never);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ContactFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ContactFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleFavoriteAsyncWhenNotFavoritedAddsFavoriteAndReturnsTrue()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        _repositoryMock.Setup(x => x.ListAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        bool result = await _service.ToggleFavoriteAsync(contactId);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.AddAsync(It.Is<ContactFavorite>(cf => cf.ContactId == contactId && cf.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<ContactFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleFavoriteAsyncWhenAlreadyFavoritedDeletesFavoriteAndReturnsFalse()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        ContactFavorite existingFavorite = new() { ContactId = contactId, UserId = userId };
        _repositoryMock.Setup(x => x.ListAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingFavorite]);

        // Act
        bool result = await _service.ToggleFavoriteAsync(contactId);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ContactFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.DeleteAsync(existingFavorite, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetFavoriteContactIdsAsyncWhenUserIdNullReturnsEmptyHashSet()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        HashSet<Guid> result = await _service.GetFavoriteContactIdsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _repositoryMock.Verify(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFavoriteContactIdsAsyncWhenUserHasFavoritesReturnsHashSetOfContactIds()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();
        List<Guid> favoriteIds = [contactId1, contactId2];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(favoriteIds);

        // Act
        HashSet<Guid> result = await _service.GetFavoriteContactIdsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(contactId1, result);
        Assert.Contains(contactId2, result);
    }
}
