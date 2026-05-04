using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Exceptions;
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
    public async Task GetFormForCreateAsyncWhenContactIsDeceasedReturnsNull()
    {
        // Arrange — IsLivingContactAsync returns 0 when the contact is marked deceased.
        Guid entityId = Guid.NewGuid();

        Core.Models.Contact.Contact deceased = new()
        {
            Id = entityId,
            FirstName = "Late",
            IsPartial = false,
            IsDeceased = true
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Core.Models.Contact.Contact, bool>> filter, CancellationToken _) =>
                filter.Compile()(deceased) ? 1 : 0);

        // Act
        ActivityFormDto? result = await _service.GetFormForCreateAsync(entityId);

        // Assert — deceased contacts cannot have a new activity form rendered.
        Assert.Null(result);
    }

    [Fact]
    public async Task QuickLogAsyncWhenContactIsDeceasedReturnsFailure()
    {
        // Arrange
        Guid entityId = Guid.NewGuid();

        Core.Models.Contact.Contact deceased = new()
        {
            Id = entityId,
            FirstName = "Late",
            IsPartial = false,
            IsDeceased = true
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Core.Models.Contact.Contact, bool>> filter, CancellationToken _) =>
                filter.Compile()(deceased) ? 1 : 0);

        // Act — use a real QuickLog suggestion type so the type guard does not short-circuit.
        OperationResult result = await _service.QuickLogAsync(entityId, Core.Constants.ActivityTypeSuggestions.QuickLog[0].Type);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("deceased", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsyncWhenContactIsDeceasedReturnsFailure()
    {
        // Arrange
        Guid entityId = Guid.NewGuid();
        ActivityFormDto dto = new()
        {
            ContactId = entityId,
            ContactIds = [entityId],
            Title = "Test"
        };

        Core.Models.Contact.Contact deceased = new()
        {
            Id = entityId,
            FirstName = "Late",
            IsPartial = false,
            IsDeceased = true
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Core.Models.Contact.Contact, bool>> filter, CancellationToken _) =>
                filter.Compile()(deceased) ? 1 : 0);

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("deceased", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFormForCreateAsyncWithValidContactIncludesSelfContactId()
    {
        Guid entityId = Guid.NewGuid();
        Guid selfContactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(selfContactId);

        ActivityFormDto? result = await _service.GetFormForCreateAsync(entityId);

        Assert.NotNull(result);
        Assert.Equal(entityId, result.ContactId);
        Assert.Contains(entityId, result.ContactIds);
        Assert.Contains(selfContactId, result.ContactIds);
        Assert.Equal(2, result.ContactIds.Count);
    }

    [Fact]
    public async Task GetFormForCreateAsyncWhenSelfContactIsEntityDoesNotDuplicateId()
    {
        Guid entityId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(entityId);

        ActivityFormDto? result = await _service.GetFormForCreateAsync(entityId);

        Assert.NotNull(result);
        Assert.Single(result.ContactIds);
        Assert.Equal(entityId, result.ContactIds[0]);
    }

    [Fact]
    public async Task CreateAsyncWithoutEntityIdInContactIdsAutomaticallyAddsEntityId()
    {
        Guid entityId = Guid.NewGuid();
        Guid otherContactId = Guid.NewGuid();

        ActivityFormDto dto = new()
        {
            ContactId = entityId,
            ContactIds = [otherContactId],
            Title = "Test Activity"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);

        List<ActivityContact>? addedActivityContacts = null;
        _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => addedActivityContacts = acs.ToList())
            .ReturnsAsync([]);

        OperationResult result = await _service.CreateAsync(dto);

        Assert.True(result.Success);
        Assert.NotNull(addedActivityContacts);
        Assert.Equal(2, addedActivityContacts.Count);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == entityId);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == otherContactId);
    }

    [Fact]
    public async Task CreateAsyncWithInvalidContactReturnsFailure()
    {
        Guid entityId = Guid.NewGuid();
        ActivityFormDto dto = new() { ContactId = entityId };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        OperationResult result = await _service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Equal("Contact not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateAsyncWhenContactRemovedDeletesActivityContact()
    {
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
            ContactId = entityId,
            ContactIds = [entityId, keepContactId] // Implicitly removing removeContactId
        };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(existingActivity);

        List<ActivityContact>? removedActivityContacts = null;
        _repositoryMock.Setup(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => removedActivityContacts = acs.ToList())
            .Returns(Task.CompletedTask);

        OperationResult result = await _service.UpdateAsync(activityId, dto);

        Assert.True(result.Success);
        Assert.NotNull(removedActivityContacts);
        Assert.Single(removedActivityContacts);
        Assert.Equal(removeContactId, removedActivityContacts[0].ContactId);
    }

    [Fact]
    public async Task UpdateAsyncWithEntityConcurrencyExceptionAndDeletedEntityReturnsFailure()
    {
        Guid activityId = Guid.NewGuid();
        ActivityFormDto dto = new() { ContactId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(new Activity { Id = activityId });

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException());

        _repositoryMock.Setup(r => r.ExistsAsync<Activity>(activityId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        OperationResult result = await _service.UpdateAsync(activityId, dto);

        Assert.False(result.Success);
        Assert.Equal("Activity not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsyncWhenActivityExistsReturnsEntityIdOfFirstContact()
    {
        Guid activityId = Guid.NewGuid();
        Guid primaryContactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ExistsAsync<Activity>(activityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        _repositoryMock.Setup(r => r.ListProjectedAsync<ActivityContact, Guid>(
                It.IsAny<Expression<Func<ActivityContact, bool>>>(),
                It.IsAny<Expression<Func<ActivityContact, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([primaryContactId]);

        OperationResult result = await _service.DeleteAsync(activityId);

        Assert.True(result.Success);
        Assert.Equal(primaryContactId, result.RedirectId);

        _repositoryMock.Verify(r => r.DeleteAsync(It.IsAny<Expression<Func<Activity, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncWhenActivityNotFoundReturnsFailure()
    {
        Guid activityId = Guid.NewGuid();
        ActivityFormDto dto = new() { ContactId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync((Activity?)null);

        OperationResult result = await _service.UpdateAsync(activityId, dto);

        Assert.False(result.Success);
        Assert.Equal("Activity not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task DeleteAsyncWhenActivityNotFoundReturnsFailure()
    {
        Guid activityId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ExistsAsync<Activity>(activityId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        OperationResult result = await _service.DeleteAsync(activityId);

        Assert.False(result.Success);
        Assert.Equal("Activity not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task GetFormAsyncWhenActivityNotFoundReturnsNull()
    {
        Guid activityId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync((Activity?)null);

        ActivityFormDto? result = await _service.GetFormAsync(activityId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetFormAsyncWhenActivityFoundReturnsDto()
    {
        Guid activityId = Guid.NewGuid();
        Guid contactId1 = Guid.NewGuid();
        Guid contactId2 = Guid.NewGuid();

        Activity activity = new()
        {
            Id = activityId,
            Title = "Test Title",
            Description = "Test Desc",
            ActivityDate = new DateTime(2025, 1, 1),
            ActivityType = "Meeting",
            Location = "Office",
            ActivityContacts = [
                new ActivityContact { ContactId = contactId1 },
                new ActivityContact { ContactId = contactId2 }
            ]
        };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(activity);

        ActivityFormDto? result = await _service.GetFormAsync(activityId);

        Assert.NotNull(result);
        Assert.Equal(activityId, result.Id);
        Assert.Equal(contactId1, result.ContactId);
        Assert.Contains(contactId1, result.ContactIds);
        Assert.Contains(contactId2, result.ContactIds);
        Assert.Equal(2, result.ContactIds.Count);
        Assert.Equal("Test Title", result.Title);
        Assert.Equal("Test Desc", result.Description);
        Assert.Equal(new DateTime(2025, 1, 1), result.ActivityDate);
        Assert.Equal("Meeting", result.ActivityType);
        Assert.Equal("Office", result.Location);
    }

    [Fact]
    public async Task GetFormForCreateAsyncWhenContactIsInvalidReturnsNull()
    {
        Guid entityId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);

        ActivityFormDto? result = await _service.GetFormForCreateAsync(entityId);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsyncReturnsActivity()
    {
        Guid activityId = Guid.NewGuid();
        Activity activity = new() { Id = activityId };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(activity);

        Activity? result = await _service.GetByIdAsync(activityId);

        Assert.NotNull(result);
        Assert.Equal(activityId, result.Id);
    }

    [Fact]
    public async Task QuickLogAsyncWithInvalidTypeReturnsFailure()
    {
        OperationResult result = await _service.QuickLogAsync(Guid.NewGuid(), "Not A Real Type");

        Assert.False(result.Success);
        Assert.Equal("Invalid activity type.", result.ErrorMessage);
    }

    [Fact]
    public async Task QuickLogAsyncWithNonQuickLogTypeReturnsFailure()
    {
        OperationResult result = await _service.QuickLogAsync(Guid.NewGuid(), "Email");

        Assert.False(result.Success);
        Assert.Equal("Invalid activity type.", result.ErrorMessage);
    }

    [Fact]
    public async Task QuickLogAsyncWithEmptyTypeReturnsFailure()
    {
        OperationResult result = await _service.QuickLogAsync(Guid.NewGuid(), "");

        Assert.False(result.Success);
        Assert.Equal("Invalid activity type.", result.ErrorMessage);
    }

    [Fact]
    public async Task QuickLogAsyncWithValidTypeCreatesActivityWithTodayAndSelfContact()
    {
        Guid contactId = Guid.NewGuid();
        Guid selfContactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(selfContactId);

        Activity? addedActivity = null;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Activity>(), It.IsAny<CancellationToken>()))
            .Callback<Activity, CancellationToken>((a, _) => addedActivity = a)
            .ReturnsAsync((Activity a, CancellationToken _) => a);

        List<ActivityContact>? addedActivityContacts = null;
        _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => addedActivityContacts = acs.ToList())
            .ReturnsAsync([]);

        OperationResult result = await _service.QuickLogAsync(contactId, "Phone Call");

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        Assert.NotNull(addedActivity);
        Assert.Equal("Phone Call", addedActivity.Title);
        Assert.Equal("Phone Call", addedActivity.ActivityType);
        Assert.Equal(DateTime.Today, addedActivity.ActivityDate);
        Assert.NotNull(addedActivityContacts);
        Assert.Equal(2, addedActivityContacts.Count);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == contactId);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == selfContactId);
    }

    [Fact]
    public async Task QuickLogAsyncWhenSelfContactIsTargetDoesNotDuplicate()
    {
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync(contactId);

        List<ActivityContact>? addedActivityContacts = null;
        _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => addedActivityContacts = acs.ToList())
            .ReturnsAsync([]);

        OperationResult result = await _service.QuickLogAsync(contactId, "Meeting");

        Assert.True(result.Success);
        Assert.NotNull(addedActivityContacts);
        Assert.Single(addedActivityContacts);
        Assert.Equal(contactId, addedActivityContacts[0].ContactId);
    }

    [Fact]
    public async Task QuickLogAsyncWithInvalidContactReturnsFailure()
    {
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync((Guid?)null);

        OperationResult result = await _service.QuickLogAsync(contactId, "Text/Message");

        Assert.False(result.Success);
        Assert.Equal("Contact not found.", result.ErrorMessage);
    }

    [Fact]
    public async Task QuickLogAsyncWhenSelfContactIsNullIncludesOnlyTargetContact()
    {
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Core.Models.Contact.Contact, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _selfContactServiceMock.Setup(s => s.GetSelfContactIdAsync()).ReturnsAsync((Guid?)null);

        List<ActivityContact>? addedActivityContacts = null;
        _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => addedActivityContacts = acs.ToList())
            .ReturnsAsync([]);

        OperationResult result = await _service.QuickLogAsync(contactId, "Phone Call");

        Assert.True(result.Success);
        Assert.NotNull(addedActivityContacts);
        Assert.Single(addedActivityContacts);
        Assert.Equal(contactId, addedActivityContacts[0].ContactId);
    }

    [Fact]
    public async Task UpdateAsyncWhenConcurrencyExceptionAndActivityStillExistsRethrows()
    {
        Guid activityId = Guid.NewGuid();
        ActivityFormDto dto = new() { ContactId = Guid.NewGuid() };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(new Activity { Id = activityId, ActivityContacts = [] });

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException());

        _repositoryMock.Setup(r => r.ExistsAsync<Activity>(activityId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<EntityConcurrencyException>(() => _service.UpdateAsync(activityId, dto));
    }

    [Fact]
    public async Task UpdateAsyncWhenNewContactsAddedIncludesThemInActivityContacts()
    {
        Guid activityId = Guid.NewGuid();
        Guid entityId = Guid.NewGuid();
        Guid existingContactId = Guid.NewGuid();
        Guid newContactId = Guid.NewGuid();

        Activity existingActivity = new()
        {
            Id = activityId,
            ActivityContacts = [
                new ActivityContact { ActivityId = activityId, ContactId = existingContactId }
            ]
        };

        ActivityFormDto dto = new()
        {
            ContactId = entityId,
            ContactIds = [entityId, existingContactId, newContactId]
        };

        _repositoryMock.Setup(r => r.GetByIdWithIncludesAsync<Activity>(activityId, It.IsAny<string[]>()))
            .ReturnsAsync(existingActivity);

        List<ActivityContact>? addedActivityContacts = null;
        _repositoryMock.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<ActivityContact>, CancellationToken>((acs, _) => addedActivityContacts = acs.ToList())
            .ReturnsAsync([]);

        OperationResult result = await _service.UpdateAsync(activityId, dto);

        Assert.True(result.Success);
        Assert.NotNull(addedActivityContacts);
        Assert.Equal(2, addedActivityContacts.Count);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == newContactId);
        Assert.Contains(addedActivityContacts, ac => ac.ContactId == entityId);
        _repositoryMock.Verify(r => r.DeleteRangeAsync(It.IsAny<IEnumerable<ActivityContact>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByContactAsyncReturnsMappedActivities()
    {
        Guid contactId = Guid.NewGuid();
        Guid activityId = Guid.NewGuid();

        List<ActivityContact> activityContacts = [
            new ActivityContact {
                ContactId = contactId,
                ActivityId = activityId,
                Activity = new Activity { Id = activityId, Title = "Test Title" }
            }
        ];

        _repositoryMock.Setup(r => r.ListAsync<ActivityContact>(
                It.IsAny<Expression<Func<ActivityContact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
            .ReturnsAsync(activityContacts);

        List<ActivityDto> result = await _service.GetByContactAsync(contactId);

        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(activityId, result[0].Id);
        Assert.Equal("Test Title", result[0].Title);
    }
}
