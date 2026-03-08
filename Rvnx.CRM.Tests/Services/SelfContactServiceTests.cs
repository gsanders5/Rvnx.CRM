using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;
using System.Security.Claims;

namespace Rvnx.CRM.Tests.Services;

public class SelfContactServiceTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Mock<ICurrentUserService> _currentUserServiceMock;
    private readonly Mock<IUserSynchronizationService> _userSynchronizationServiceMock;
    private readonly SelfContactService _service;
    private readonly ClaimsPrincipal _principal;

    public SelfContactServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _userSynchronizationServiceMock = new Mock<IUserSynchronizationService>();

        _service = new SelfContactService(_repositoryMock.Object, _currentUserServiceMock.Object, _userSynchronizationServiceMock.Object);
        _principal = new ClaimsPrincipal();
    }

    [Fact]
    public async Task GetSelfContactIdAsyncWithNoUserIdReturnsNull()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns((Guid?)null);

        Guid? result = await _service.GetSelfContactIdAsync(_principal);

        Assert.Null(result);
        _userSynchronizationServiceMock.Verify(s => s.SyncUserAsync(_principal), Times.Once());
    }

    [Fact]
    public async Task GetSelfContactIdAsyncWithUserReturnsSelfContactId()
    {
        Guid userId = Guid.NewGuid();
        Guid selfContactId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

        Rvnx.CRM.Core.Models.User user = new() { Id = userId, SelfContactId = selfContactId };
        _repositoryMock.Setup(r => r.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        Guid? result = await _service.GetSelfContactIdAsync(_principal);

        Assert.Equal(selfContactId, result);
    }

    [Fact]
    public async Task GetSelfContactFormAsyncWithNoUserIdReturnsNull()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns((Guid?)null);

        ContactFormDto? result = await _service.GetSelfContactFormAsync(_principal);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetSelfContactFormAsyncWithUserNoNameReturnsEmailOnly()
    {
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
        _currentUserServiceMock.Setup(c => c.UserName).Returns((string?)null);

        Rvnx.CRM.Core.Models.User user = new() { Id = userId, Email = "test@example.com" };
        _repositoryMock.Setup(r => r.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        ContactFormDto? result = await _service.GetSelfContactFormAsync(_principal);

        Assert.NotNull(result);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("", result.FirstName);
        Assert.Null(result.LastName);
    }

    [Fact]
    public async Task GetSelfContactFormAsyncWithUserFirstLastNameSplitsNameCorrectly()
    {
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);
        _currentUserServiceMock.Setup(c => c.UserName).Returns("John Doe Smith");

        Rvnx.CRM.Core.Models.User user = new() { Id = userId, Email = "john@example.com" };
        _repositoryMock.Setup(r => r.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        ContactFormDto? result = await _service.GetSelfContactFormAsync(_principal);

        Assert.NotNull(result);
        Assert.Equal("john@example.com", result.Email);
        Assert.Equal("John", result.FirstName);
        Assert.Equal("Doe Smith", result.LastName);
    }

    [Fact]
    public async Task CreateSelfContactAsyncWithUnauthenticatedUserReturnsFailure()
    {
        _currentUserServiceMock.Setup(c => c.UserId).Returns((Guid?)null);
        ContactFormDto dto = new();

        ContactOperationResult result = await _service.CreateSelfContactAsync(_principal, dto);

        Assert.False(result.Success);
        Assert.Contains("User not authenticated.", result.Errors);
    }

    [Fact]
    public async Task CreateSelfContactAsyncWithNonExistentUserReturnsFailure()
    {
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

        _repositoryMock.Setup(r => r.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId, It.IsAny<CancellationToken>())).ReturnsAsync((Rvnx.CRM.Core.Models.User?)null);
        _repositoryMock.Setup(r => r.ListAsync<Rvnx.CRM.Core.Models.User>(It.IsAny<Expression<Func<Rvnx.CRM.Core.Models.User, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        ContactFormDto dto = new();

        ContactOperationResult result = await _service.CreateSelfContactAsync(_principal, dto);

        Assert.False(result.Success);
        Assert.Contains("User entity not found.", result.Errors);
    }

    [Fact]
    public async Task CreateSelfContactAsyncWithExistingSelfContactReturnsExistingId()
    {
        Guid userId = Guid.NewGuid();
        Guid existingContactId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

        Rvnx.CRM.Core.Models.User user = new() { Id = userId, SelfContactId = existingContactId };
        _repositoryMock.Setup(r => r.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        ContactFormDto dto = new();

        ContactOperationResult result = await _service.CreateSelfContactAsync(_principal, dto);

        Assert.True(result.Success);
        Assert.Equal(existingContactId, result.ContactId);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), It.IsAny<CancellationToken>()), Times.Never());
    }

    [Fact]
    public async Task CreateSelfContactAsyncWithValidDtoCreatesContactAndDelegatesToHelper()
    {
        Guid userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(c => c.UserId).Returns(userId);

        Rvnx.CRM.Core.Models.User user = new() { Id = userId, SelfContactId = null };
        _repositoryMock.Setup(r => r.GetByIdAsync<Rvnx.CRM.Core.Models.User>(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        // Setup lists for helper (ContactUpdateHelper uses these internally for reminders)
        _repositoryMock.Setup(r => r.ListAsync<ReminderOffset>(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        ContactFormDto dto = new()
        {
            FirstName = "Test",
            LastName = "User",
            Email = "test@example.com",
            Phone = "555-1234",
            Birthday = new DateTime(1990, 1, 1),
            RemindOnBirthday = true
        };

        ContactOperationResult result = await _service.CreateSelfContactAsync(_principal, dto);

        Assert.True(result.Success);
        Assert.NotNull(result.ContactId);

        // 1. Contact added
        _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "Test" && c.LastName == "User"), It.IsAny<CancellationToken>()), Times.Once());

        // 2. User updated
        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<Rvnx.CRM.Core.Models.User>(u => u.Id == userId && u.SelfContactId == result.ContactId), It.IsAny<CancellationToken>()), Times.Once());

        // 3. ContactMethods (Email, Phone) via ContactUpdateHelper
        _repositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm => cm.Type == ContactMethodType.Email && cm.Value == "test@example.com"), It.IsAny<CancellationToken>()), Times.Once());
        _repositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(cm => cm.Type == ContactMethodType.Phone && cm.Value == "555-1234"), It.IsAny<CancellationToken>()), Times.Once());

        // 4. Birthday via ContactUpdateHelper
        _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd => sd.EventDate == new DateOnly(1990, 1, 1) && sd.Title == Rvnx.CRM.Core.Constants.SignificantDateTitles.Birthday), It.IsAny<CancellationToken>()), Times.Once());

        // 5. ReminderOffset via ContactUpdateHelper
        _repositoryMock.Verify(r => r.AddAsync(It.Is<ReminderOffset>(ro => ro.DaysBeforeEvent == 0 && ro.IsActive == true), It.IsAny<CancellationToken>()), Times.Once());

        // 6. SaveChangesAsync called twice (once for user/contact, once for helper methods)
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
