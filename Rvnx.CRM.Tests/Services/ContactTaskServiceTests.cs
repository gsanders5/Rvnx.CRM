using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Tests.Helpers;

namespace Rvnx.CRM.Tests.Services;

public class ContactTaskServiceTests : IDisposable
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ContactTaskService _serviceWithMock;

    private readonly CRMDbContext _context;
    private readonly Repository _repository;
    private readonly ContactTaskService _serviceWithDb;

    public ContactTaskServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _serviceWithMock = new ContactTaskService(_repositoryMock.Object);

        _context = TestDbContextFactory.CreateForDefaultUser();
        _repository = new Repository(_context);
        _serviceWithDb = new ContactTaskService(_repository);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ToggleCompleteAsyncSetsCompletedDateWhenMarkingComplete()
    {
        Guid taskId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        ContactTask task = new()
        {
            Id = taskId,
            ContactId = contactId,
            Title = "Test Task",
            DueDate = DateOnly.FromDateTime(DateTime.Today),
            IsCompleted = false
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactTask>(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        ContactTask? captured = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ContactTask>(), It.IsAny<CancellationToken>()))
            .Callback<ContactTask, CancellationToken>((t, _) => captured = t)
            .ReturnsAsync(task);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        DateTime before = DateTime.UtcNow;

        OperationResult result = await _serviceWithMock.ToggleCompleteAsync(taskId);

        DateTime after = DateTime.UtcNow;

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.True(captured.IsCompleted);
        Assert.NotNull(captured.CompletedDate);
        Assert.InRange(captured.CompletedDate!.Value, before, after);
    }

    [Fact]
    public async Task ToggleCompleteAsyncClearsCompletedDateWhenMarkingIncomplete()
    {
        Guid taskId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        ContactTask task = new()
        {
            Id = taskId,
            ContactId = contactId,
            Title = "Test Task",
            DueDate = DateOnly.FromDateTime(DateTime.Today),
            IsCompleted = true,
            CompletedDate = DateTime.UtcNow.AddDays(-1)
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<ContactTask>(taskId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        ContactTask? captured = null;
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<ContactTask>(), It.IsAny<CancellationToken>()))
            .Callback<ContactTask, CancellationToken>((t, _) => captured = t)
            .ReturnsAsync(task);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _serviceWithMock.ToggleCompleteAsync(taskId);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.False(captured.IsCompleted);
        Assert.Null(captured.CompletedDate);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncExcludesCompletedAndNullContactIdTasks()
    {
        Guid contactId = Guid.NewGuid();

        _context.Contacts!.Add(new Contact
        {
            Id = contactId,
            FirstName = "Alice",
            LastName = "Smith"
        });

        DateOnly futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7));

        _context.ContactTasks!.AddRange(
            new ContactTask
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Active Task",
                DueDate = futureDate,
                IsCompleted = false
            },
            new ContactTask
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                Title = "Completed Task",
                DueDate = futureDate,
                IsCompleted = true,
                CompletedDate = DateTime.UtcNow.AddDays(-1)
            },
            new ContactTask
            {
                Id = Guid.NewGuid(),
                ContactId = null,
                Title = "Orphaned Task",
                DueDate = futureDate,
                IsCompleted = false
            }
        );

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _serviceWithDb.GetCalendarEventsAsync();

        Assert.Single(events);
        CalendarEventDto ev = events[0];
        Assert.Contains("Active Task", ev.Title);
        Assert.Contains("Alice", ev.Title);
        Assert.Equal(CalendarColors.Task, ev.Color);
        Assert.True(ev.AllDay);
        Assert.Equal(contactId, ev.ContactId);
        Assert.Equal(futureDate.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), ev.Start);
    }

    [Fact]
    public async Task GetCalendarEventsAsyncSuppressesTasksAttachedToDeceasedContact()
    {
        // Arrange — symmetrical to SignificantDate calendar filter: tasks for a deceased
        // contact should not appear in upcoming-event surfaces (calendar feed).
        Guid livingId = Guid.NewGuid();
        Guid deceasedId = Guid.NewGuid();

        _context.Contacts!.AddRange(
            new Contact { Id = livingId, FirstName = "Alive", LastName = "Person" },
            new Contact
            {
                Id = deceasedId,
                FirstName = "Late",
                LastName = "Person",
                IsDeceased = true,
                DateOfDeath = new DateOnly(2024, 1, 15)
            }
        );

        DateOnly futureDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3));

        _context.ContactTasks!.AddRange(
            new ContactTask
            {
                Id = Guid.NewGuid(),
                ContactId = livingId,
                Title = "Living Task",
                DueDate = futureDate,
                IsCompleted = false
            },
            new ContactTask
            {
                Id = Guid.NewGuid(),
                ContactId = deceasedId,
                Title = "Deceased Task",
                DueDate = futureDate,
                IsCompleted = false
            }
        );

        await _context.SaveChangesAsync();

        List<CalendarEventDto> events = await _serviceWithDb.GetCalendarEventsAsync();

        Assert.Single(events);
        Assert.Contains("Living Task", events[0].Title);
    }

    [Fact]
    public async Task CreateAsyncWhenContactIsDeceasedReturnsFailure()
    {
        Guid contactId = Guid.NewGuid();
        ContactTaskFormDto dto = new()
        {
            ContactId = contactId,
            Title = "Send a card",
            DueDate = DateOnly.FromDateTime(DateTime.Today.AddDays(7))
        };

        Contact deceased = new()
        {
            Id = contactId,
            FirstName = "Late",
            IsPartial = false,
            IsDeceased = true
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Contact, bool>> filter, CancellationToken _) =>
                filter.Compile()(deceased) ? 1 : 0);

        OperationResult result = await _serviceWithMock.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Contains("deceased", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactTask>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetFormForCreateAsyncWhenContactIsDeceasedReturnsNull()
    {
        // Arrange — forward-looking guard at the form-render level.
        Guid contactId = Guid.NewGuid();

        Contact deceased = new()
        {
            Id = contactId,
            FirstName = "Late",
            IsPartial = false,
            IsDeceased = true
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Contact, bool>> filter, CancellationToken _) =>
                filter.Compile()(deceased) ? 1 : 0);

        ContactTaskFormDto? result = await _serviceWithMock.GetFormForCreateAsync(contactId);

        Assert.Null(result);
    }
}
