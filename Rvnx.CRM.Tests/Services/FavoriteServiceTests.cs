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
        _repositoryMock.Verify(x => x.CountAsync(It.IsAny<Expression<Func<ContactFavorite, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ContactFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Expression<Func<ContactFavorite, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ToggleFavoriteAsyncWhenNotFavoritedAddsFavoriteAndReturnsTrue()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        _repositoryMock.Setup(x => x.CountAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        bool result = await _service.ToggleFavoriteAsync(contactId);

        // Assert
        Assert.True(result);
        _repositoryMock.Verify(x => x.AddAsync(It.Is<ContactFavorite>(cf => cf.ContactId == contactId && cf.UserId == userId), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Expression<Func<ContactFavorite, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleFavoriteAsyncWhenAlreadyFavoritedDeletesFavoriteAndReturnsFalse()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        _repositoryMock.Setup(x => x.CountAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.ToggleFavoriteAsync(contactId);

        // Assert
        Assert.False(result);
        _repositoryMock.Verify(x => x.AddAsync(It.IsAny<ContactFavorite>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(x => x.DeleteAsync(It.IsAny<Expression<Func<ContactFavorite, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetFavoriteSidebarItemsAsync_UserIdNull_ReturnsEmptyList()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        var result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetFavoriteSidebarItemsAsync_NoFavorites_ReturnsEmptyList()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetFavoriteSidebarItemsAsync_NoValidContacts_ReturnsEmptyList()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        Guid contactId1 = Guid.NewGuid();
        List<Guid> favoriteIds = [contactId1];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(favoriteIds);

        _repositoryMock.Setup(x => x.ListProjectedAsync<Contact, Core.DTOs.Contact.FavoriteSidebarItemDto>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, Core.DTOs.Contact.FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        var result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "Test names follow a standard convention")]
    public async Task GetFavoriteSidebarItemsAsync_WithValidContacts_ReturnsOrderedListWithProfileImages()
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

        var item1 = new Core.DTOs.Contact.FavoriteSidebarItemDto { Id = contactId1, FirstName = "Zoe", LastName = "Smith" };
        var item2 = new Core.DTOs.Contact.FavoriteSidebarItemDto { Id = contactId2, FirstName = "Alice", LastName = "Jones" };
        List<Core.DTOs.Contact.FavoriteSidebarItemDto> items = [item1, item2];

        _repositoryMock.Setup(x => x.ListProjectedAsync<Contact, Core.DTOs.Contact.FavoriteSidebarItemDto>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, Core.DTOs.Contact.FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        Guid attachmentId2 = Guid.NewGuid();
        List<(Guid ContactId, Guid AttachmentId)> attachments = [(contactId2, attachmentId2)];

        _repositoryMock.Setup(x => x.ListProjectedAsync<Core.Models.Base.Attachment, (Guid, Guid)>(
            It.IsAny<Expression<Func<Core.Models.Base.Attachment, bool>>>(),
            It.IsAny<Expression<Func<Core.Models.Base.Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);

        // Act
        var result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        // Alice should be first because of sorting
        Assert.Equal(contactId2, result[0].Id);
        Assert.Equal("Alice", result[0].FirstName);
        Assert.Equal(attachmentId2, result[0].ProfileImageId);

        // Zoe should be second
        Assert.Equal(contactId1, result[1].Id);
        Assert.Equal("Zoe", result[1].FirstName);
        Assert.Null(result[1].ProfileImageId);
    }
}
