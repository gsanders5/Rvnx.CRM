using Moq;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
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
}

public class FavoriteServiceGetFavoriteSidebarItemsAsyncTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly FavoriteService _service;

    public FavoriteServiceGetFavoriteSidebarItemsAsyncTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _service = new FavoriteService(_repositoryMock.Object, _currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncWhenUserIdNullReturnsEmptyList()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        List<Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncWhenNoFavoritesReturnsEmptyList()
    {
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncWhenAllContactsHiddenOrDeceasedReturnsEmptyList()
    {
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        Guid contactId = Guid.NewGuid();
        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([contactId]);

        // Instead of mocking the extension method, mock the underlying interface method ListProjectedAsync
        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncWithValidFavoritesReturnsSortedItemsWithAttachments()
    {
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();
        Guid attachmentId2 = Guid.NewGuid();

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([contactId1, contactId2]);

        List<Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto> items =
        [
            new Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto { Id = contactId2, FirstName = "Alice", LastName = "Smith" },
            new Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto { Id = contactId1, FirstName = "Bob", LastName = "Jones" }
        ];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        List<(Guid ContactId, Guid AttachmentId)> attachments =
        [
            (contactId2, attachmentId2)
        ];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);

        List<Rvnx.CRM.Core.DTOs.Contact.FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        Assert.Equal(2, result.Count);

        // Sorted by FirstName then LastName: Alice Smith, Bob Jones
        Assert.Equal("Alice", result[0].FirstName);
        Assert.Equal(contactId2, result[0].Id);
        Assert.Equal(attachmentId2, result[0].ProfileImageId);

        Assert.Equal("Bob", result[1].FirstName);
        Assert.Equal(contactId1, result[1].Id);
        Assert.Null(result[1].ProfileImageId);
    }
}
