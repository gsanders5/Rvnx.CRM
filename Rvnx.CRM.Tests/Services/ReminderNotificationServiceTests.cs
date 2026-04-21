using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Data;
using Rvnx.CRM.Infrastructure.Repositories;
using Rvnx.CRM.Infrastructure.Services;
using System.Diagnostics.CodeAnalysis;

namespace Rvnx.CRM.Tests.Services;

public class ReminderNotificationServiceTests
{
    private static (CRMDbContext Context, Repository Repository) CreateInMemoryDb()
    {
        DbContextOptions<CRMDbContext> options = new DbContextOptionsBuilder<CRMDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Mock<ICurrentUserService> mockUserService = new();
        mockUserService.Setup(u => u.UserId).Returns((Guid?)null);
        mockUserService.Setup(u => u.GroupId).Returns((Guid?)null);
        mockUserService.Setup(u => u.UserName).Returns("System");

        CRMDbContext context = new(options, mockUserService.Object);
        context.Database.EnsureCreated();

        return (context, new Repository(context));
    }


    private static IConfiguration BuildEnabledConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["EmailNotifications:Enabled"] = "true",
                ["EmailNotifications:SmtpSettings:SenderEmail"] = "noreply@example.com",
                ["EmailNotifications:SmtpSettings:SenderName"] = "Test CRM",
                ["EmailNotifications:SmtpSettings:Server"] = "localhost",
                ["EmailNotifications:SmtpSettings:Port"] = "587"
            })
            .Build();
    }

    private static IConfiguration BuildDisabledConfig()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["EmailNotifications:Enabled"] = "false" })
            .Build();
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task SendDueRemindersAsync_WhenDisabled_ReturnsEarlyWithoutProcessing()
    {
        (CRMDbContext context, Repository repository) = CreateInMemoryDb();

        ReminderNotificationService service = new(repository, BuildDisabledConfig());

        string result = await service.SendDueRemindersAsync(DateOnly.FromDateTime(DateTime.Today));

        Assert.Contains("disabled", result, StringComparison.OrdinalIgnoreCase);

        // No logs should have been written
        int logCount = await context.Set<ReminderLog>().CountAsync();
        Assert.Equal(0, logCount);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task SendDueRemindersAsync_WhenContactHasNoGroupId_SkipsReminderWithoutError()
    {
        (CRMDbContext context, Repository repository) = CreateInMemoryDb();

        DateOnly today = new(2025, 6, 1);

        Contact contact = new() { FirstName = "Jane", LastName = "Doe" };
        context.Contacts!.Add(contact);

        SignificantDate sd = new()
        {
            Contact = contact,
            Title = "Birthday",
            EventDate = today, // Due today (no offset, same date)
            RecurrenceType = RecurrenceType.None,
            IsActive = true
        };
        context.SignificantDates!.Add(sd);

        ReminderOffset offset = new() { SignificantDate = sd, DaysBeforeEvent = 0, IsActive = true };
        context.ReminderOffsets!.Add(offset);

        await context.SaveChangesAsync();

        ReminderNotificationService service = new(repository, BuildEnabledConfig());
        _ = await service.SendDueRemindersAsync(today);

        // No ReminderLog created since there are no recipients
        int logCount = await context.Set<ReminderLog>().CountAsync();
        Assert.Equal(0, logCount);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task SendDueRemindersAsync_WhenNoUsersInContactGroup_SkipsReminderWithoutError()
    {
        (CRMDbContext context, Repository repository) = CreateInMemoryDb();

        DateOnly today = new(2025, 6, 1);

        Guid groupId = Guid.NewGuid();

        Contact contact = new() { FirstName = "John", LastName = "Smith", GroupId = groupId };
        context.Contacts!.Add(contact);

        SignificantDate sd = new()
        {
            Contact = contact,
            Title = "Anniversary",
            EventDate = today,
            RecurrenceType = RecurrenceType.None,
            IsActive = true
        };
        context.SignificantDates!.Add(sd);

        ReminderOffset offset = new() { SignificantDate = sd, DaysBeforeEvent = 0, IsActive = true };
        context.ReminderOffsets!.Add(offset);

        await context.SaveChangesAsync();

        ReminderNotificationService service = new(repository, BuildEnabledConfig());
        _ = await service.SendDueRemindersAsync(today);

        // No log because no recipients in group
        int logCount = await context.Set<ReminderLog>().CountAsync();
        Assert.Equal(0, logCount);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task SendDueRemindersAsync_WhenReminderNotDueToday_SkipsOffset()
    {
        (CRMDbContext context, Repository repository) = CreateInMemoryDb();

        DateOnly today = new(2025, 6, 1);

        Guid groupId = Guid.NewGuid();
        UserGroup group = new() { Id = groupId, Name = "TestGroup" };
        context.UserGroups!.Add(group);

        Contact contact = new() { FirstName = "Alice", LastName = "Brown", GroupId = groupId };
        context.Contacts!.Add(contact);

        // Event is in 10 days; reminder is 3 days before — so NOT due today
        SignificantDate sd = new()
        {
            Contact = contact,
            Title = "Meeting",
            EventDate = today.AddDays(10),
            RecurrenceType = RecurrenceType.None,
            IsActive = true
        };
        context.SignificantDates!.Add(sd);

        ReminderOffset offset = new() { SignificantDate = sd, DaysBeforeEvent = 3, IsActive = true };
        context.ReminderOffsets!.Add(offset);

        await context.SaveChangesAsync();

        ReminderNotificationService service = new(repository, BuildEnabledConfig());
        _ = await service.SendDueRemindersAsync(today);

        // Not due today, no log entry
        int logCount = await context.Set<ReminderLog>().CountAsync();
        Assert.Equal(0, logCount);
    }

    [Fact]
    [SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores",
        Justification = "Test names can contain underscores for readability.")]
    public async Task SendDueRemindersAsync_WhenLogAlreadyExistsAndIsSuccessful_SkipsSending()
    {
        (CRMDbContext context, Repository repository) = CreateInMemoryDb();

        DateOnly today = new(2025, 6, 1);

        Guid groupId = Guid.NewGuid();
        UserGroup group = new() { Id = groupId, Name = "TestGroup" };
        context.UserGroups!.Add(group);

        User user = new() { Id = Guid.NewGuid(), Email = "testuser@example.com", GroupId = groupId };
        context.Users!.Add(user);

        Contact contact = new() { FirstName = "Bob", LastName = "Builder", GroupId = groupId };
        context.Contacts!.Add(contact);

        SignificantDate sd = new()
        {
            Contact = contact,
            Title = "Anniversary",
            EventDate = today, // Due today
            RecurrenceType = RecurrenceType.None,
            IsActive = true
        };
        context.SignificantDates!.Add(sd);

        ReminderOffset offset = new() { SignificantDate = sd, DaysBeforeEvent = 0, IsActive = true };
        context.ReminderOffsets!.Add(offset);

        ReminderLog existingLog = new()
        {
            ReminderOffset = offset,
            OccurrenceDate = today,
            ScheduledFor = today,
            Success = true,
            SentAt = DateTime.UtcNow
        };
        context.ReminderLogs!.Add(existingLog);

        await context.SaveChangesAsync();

        ReminderNotificationService service = new(repository, BuildEnabledConfig());
        string result = await service.SendDueRemindersAsync(today);

        // Should not have sent anything or added any new log, sent count should be 0, failed count 0
        Assert.Contains("0 sent, 0 failed", result);

        // Still only 1 log
        int logCount = await context.Set<ReminderLog>().CountAsync();
        Assert.Equal(1, logCount);
    }
}
