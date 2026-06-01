using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Models.Base;
using System.Collections.Generic;
using Rvnx.CRM.Core.Extensions;
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
    public async Task GetFavoriteSidebarItemsAsyncWhenUserIdNullReturnsEmptyList()
    {
        // Arrange
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        // Act
        List<FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _repositoryMock.Verify(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncWhenUserHasNoFavoritesReturnsEmptyList()
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
        List<FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _repositoryMock.Verify(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncWhenUserHasFavoritesFiltersAndMapsData()
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

        List<FavoriteSidebarItemDto> contactDtos = [
            new() { Id = contactId1, FirstName = "Alice", LastName = "Smith" },
            new() { Id = contactId2, FirstName = "Bob", LastName = "Jones" }
        ];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactDtos);

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        List<FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, i => i.Id == contactId1 && i.FirstName == "Alice");
        Assert.Contains(result, i => i.Id == contactId2 && i.FirstName == "Bob");
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncWhenFavoritesHaveAttachmentsMapsProfileImage()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        Guid contactId = Guid.NewGuid();
        Guid attachmentId = Guid.NewGuid();
        List<Guid> favoriteIds = [contactId];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(favoriteIds);

        List<FavoriteSidebarItemDto> contactDtos = [
            new() { Id = contactId, FirstName = "Alice", LastName = "Smith" }
        ];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactDtos);

        List<(Guid, Guid)> attachments = [(contactId, attachmentId)];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(attachments);

        // Act
        List<FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(attachmentId, result[0].ProfileImageId);
    }

    [Fact]
    public async Task GetFavoriteSidebarItemsAsyncReturnsSortedData()
    {
        // Arrange
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);

        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();
        Guid contactId3 = Guid.NewGuid();
        List<Guid> favoriteIds = [contactId1, contactId2, contactId3];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<ContactFavorite, bool>>>(),
            It.IsAny<Expression<Func<ContactFavorite, Guid>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(favoriteIds);

        List<FavoriteSidebarItemDto> contactDtos = [
            new() { Id = contactId1, FirstName = "Zebra", LastName = "Zoo" },
            new() { Id = contactId2, FirstName = "Apple", LastName = "Bee" },
            new() { Id = contactId3, FirstName = "Apple", LastName = "Aardvark" }
        ];

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, FavoriteSidebarItemDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(contactDtos);

        _repositoryMock.Setup(x => x.ListProjectedAsync(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        List<FavoriteSidebarItemDto> result = await _service.GetFavoriteSidebarItemsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Apple", result[0].FirstName);
        Assert.Equal("Aardvark", result[0].LastName);
        Assert.Equal("Apple", result[1].FirstName);
        Assert.Equal("Bee", result[1].LastName);
        Assert.Equal("Zebra", result[2].FirstName);
        Assert.Equal("Zoo", result[2].LastName);
    }
}
