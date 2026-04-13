using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Activity;
using Rvnx.CRM.Infrastructure.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ActivityServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<ISelfContactService> _selfContactServiceMock;
    private readonly ActivityService _service;

    public ActivityServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _selfContactServiceMock = new Mock<ISelfContactService>();
        _service = new ActivityService(_repositoryMock.Object, _selfContactServiceMock.Object);
    }

    [Fact]
    public async Task GetFormForCreateAsyncWithValidContactIncludesSelfContactId()
    {
        // Arrange
        Guid entityId = Guid.NewGuid();
        Guid selfContactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(selfContactId);

        // Act
        ActivityFormDto? result = await _service.GetFormForCreateAsync(entityId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(entityId, result.EntityId);
        Assert.Contains(entityId, result.ContactIds);
        Assert.Contains(selfContactId, result.ContactIds);
        Assert.Equal(2, result.ContactIds.Count);
    }

    [Fact]
    public async Task GetFormForCreateAsyncWhenSelfContactIsEntityDoesNotDuplicateId()
    {
        // Arrange
        Guid entityId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(entityId);

        // Act
        ActivityFormDto? result = await _service.GetFormForCreateAsync(entityId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.ContactIds);
        Assert.Equal(entityId, result.ContactIds[0]);
    }

    [Fact]
    public async Task CreateAsyncWithoutEntityIdInContactIdsAutomaticallyAddsEntityId()
    {
        // Arrange
        Guid entityId = Guid.NewGuid();
        Guid otherContactId = Guid.NewGuid();

        ActivityFormDto dto = new()
        {
            EntityId = entityId,
            ContactIds = [otherContactId],
            Title = "Test Activity"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        List<ActivityContact>? addedActivityContacts = null;
        _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => addedActivityContacts = acs.ToList())
            .ReturnsAsync(new List<ActivityContact>());

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(addedActivityContacts);
        Assert.Equal(2, addedActivityContacts.Count);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == entityId);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == otherContactId);
    }

    [Fact]
    public async Task CreateAsyncWithInvalidContactReturnsFailure()
    {
        // Arrange
        Guid entityId = Guid.NewGuid();
        ActivityFormDto dto = new() { EntityId = entityId };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Contact not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAsyncWhenContactRemovedDeletesActivityContact()
    {
        // Arrange
        Guid activityId = Guid.NewGuid();
        Guid entityId = Guid.NewGuid();
        Guid keepContactId = Guid.NewGuid();
        Guid removeContactId = Guid.NewGuid();

        Activity existingActivity = new()
        {
            Id = activityId,
            ActivityContacts = [
                new ActivityContact { ActivityId = activityId, ContactId = keepContactId },
                new ActivityContact { ActivityId = activityId, ContactId = removeContactId }
            ]
        };

        ActivityFormDto dto = new()
        {
            EntityId = entityId,
            ContactIds = [entityId, keepContactId] // Implicitly removing removeContactId
        };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(existingActivity);

        List<ActivityContact>? removedActivityContacts = null;
        _repositoryMock.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => removedActivityContacts = acs.ToList())
            .Returns(Task.CompletedTask);

        // Act
        OperationResult result = await _service.UpdateAsync(activityId, dto);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(removedActivityContacts);
        Assert.Single(removedActivityContacts);
        Assert.Equal(removeContactId, removedActivityContacts[0].ContactId);
    }

    [Fact]
    public async Task UpdateAsyncWithEntityConcurrencyExceptionAndDeletedEntityReturnsFailure()
    {
        // Arrange
        Guid activityId = Guid.NewGuid();
        ActivityFormDto dto = new() { EntityId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(new Activity { Id = activityId });

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException());

        _repositoryMock.Setup(r => r.ExistsAsync<Activity>(activityId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        OperationResult result = await _service.UpdateAsync(activityId, dto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Activity not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsyncWhenActivityExistsReturnsEntityIdOfFirstContact()
    {
        // Arrange
        Guid activityId = Guid.NewGuid();
        Guid primaryContactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ExistsAsync<Activity>(activityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _repositoryMock.Setup(r => r.ListProjectedAsync<ActivityContact, Guid>(
                It.IsAny<Expression<Func<ActivityContact, bool>>>(),
                It.IsAny<Expression<Func<ActivityContact, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([primaryContactId]);

        // Act
        OperationResult result = await _service.DeleteAsync(activityId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(primaryContactId, result.RedirectId);

        _repositoryMock.Verify(r => r.DeleteAsync<Activity>(activityId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
