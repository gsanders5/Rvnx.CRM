using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ContactUpdateHelperTests
{
    private readonly Mock<IRepository> _repositoryMock;

    public ContactUpdateHelperTests()
    {
        _repositoryMock = new Mock<IRepository>();
    }

    [Fact]
    public async Task UpdateOrAddContactMethodAsyncWithNewValueAndNoExistingMethodAddsNewMethod()
    {
        Guid contactId = Guid.NewGuid();

        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repositoryMock.Object, contactId, ContactMethodType.Email, "test@example.com", null);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<ContactMethod>(c =>
            c.ContactId == contactId &&
            c.Type == ContactMethodType.Email &&
            c.Value == "test@example.com" &&
            c.Label == ContactMethodLabels.Primary), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync<ContactMethod>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddContactMethodAsyncWithNewValueAndExistingMethodDifferentValueUpdatesMethod()
    {
        Guid contactId = Guid.NewGuid();
        ContactMethod existingMethod = new() { Id = Guid.NewGuid(), ContactId = contactId, Type = ContactMethodType.Email, Value = "old@example.com" };

        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repositoryMock.Object, contactId, ContactMethodType.Email, "new@example.com", existingMethod);

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<ContactMethod>(c => c.Value == "new@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync<ContactMethod>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddContactMethodAsyncWithNewValueAndExistingMethodSameValueDoesNothing()
    {
        Guid contactId = Guid.NewGuid();
        ContactMethod existingMethod = new() { Id = Guid.NewGuid(), ContactId = contactId, Type = ContactMethodType.Email, Value = "same@example.com" };

        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repositoryMock.Object, contactId, ContactMethodType.Email, "same@example.com", existingMethod);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync<ContactMethod>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddContactMethodAsyncWithEmptyValueAndExistingMethodDeletesMethod()
    {
        Guid contactId = Guid.NewGuid();
        Guid methodId = Guid.NewGuid();
        ContactMethod existingMethod = new() { Id = methodId, ContactId = contactId, Type = ContactMethodType.Email, Value = "old@example.com" };

        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repositoryMock.Object, contactId, ContactMethodType.Email, "", existingMethod);

        _repositoryMock.Verify(r => r.DeleteAsync<ContactMethod>(methodId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddContactMethodAsyncWithEmptyValueAndNoExistingMethodDoesNothing()
    {
        Guid contactId = Guid.NewGuid();

        await ContactUpdateHelper.UpdateOrAddContactMethodAsync(_repositoryMock.Object, contactId, ContactMethodType.Email, null, null);

        _repositoryMock.Verify(r => r.DeleteAsync<ContactMethod>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ContactMethod>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithNewDateAndNoExistingDateAddsDate()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, null, false);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(d =>
            d.ContactId == contactId &&
            d.EventDate == new DateOnly(1990, 5, 15) &&
            d.Title == SignificantDateTitles.Birthday &&
            d.RecurrenceType == RecurrenceType.Annual &&
            d.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithSentinelYearAddsDateWithYearOne()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1, 5, 15);

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, null, false);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(d =>
            d.ContactId == contactId &&
            d.EventDate == new DateOnly(1, 5, 15) &&
            d.Title == SignificantDateTitles.Birthday &&
            d.RecurrenceType == RecurrenceType.Annual &&
            d.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithNewDateAndExistingDateDifferentUpdatesDate()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);
        SignificantDate existingDate = new() { Id = Guid.NewGuid(), EventDate = new DateOnly(1985, 1, 1) };

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, existingDate, false);

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<SignificantDate>(d => d.EventDate == new DateOnly(1990, 5, 15)), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithSentinelYearAndExistingDateUpdatesDateToYearOne()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1, 5, 15);
        SignificantDate existingDate = new() { Id = Guid.NewGuid(), EventDate = new DateOnly(1990, 5, 15) };

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, existingDate, false);

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<SignificantDate>(d => d.EventDate == new DateOnly(1, 5, 15)), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithNewDateAndExistingDateSameDoesNotUpdateDate()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);
        SignificantDate existingDate = new() { Id = Guid.NewGuid(), EventDate = new DateOnly(1990, 5, 15) };

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, existingDate, false);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithEmptyDateAndExistingDateDeletesDate()
    {
        Guid contactId = Guid.NewGuid();
        Guid dateId = Guid.NewGuid();
        SignificantDate existingDate = new() { Id = dateId };

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, null, existingDate, false);

        _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(dateId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithEmptyDateAndNoExistingDateDoesNothing()
    {
        Guid contactId = Guid.NewGuid();

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, null, null, false);

        _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithRemindTrueAndNoExistingOffsetAddsOffset()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, null, true);

        _repositoryMock.Verify(r => r.AddAsync(It.Is<ReminderOffset>(o => o.DaysBeforeEvent == 0 && o.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithRemindTrueAndExistingOffsetInactiveUpdatesOffsetToActive()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);
        SignificantDate existingDate = new() { Id = Guid.NewGuid(), EventDate = new DateOnly(1990, 5, 15) };
        ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), DaysBeforeEvent = 0, IsActive = false };

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingOffset]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, existingDate, true);

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<ReminderOffset>(o => o.IsActive == true), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithRemindTrueAndExistingOffsetActiveDoesNothingToOffset()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);
        SignificantDate existingDate = new() { Id = Guid.NewGuid(), EventDate = new DateOnly(1990, 5, 15) };
        ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), DaysBeforeEvent = 0, IsActive = true };

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingOffset]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, existingDate, true);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithRemindFalseAndExistingOffsetActiveDisablesOffset()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);
        SignificantDate existingDate = new() { Id = Guid.NewGuid(), EventDate = new DateOnly(1990, 5, 15) };
        ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), DaysBeforeEvent = 0, IsActive = true };

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingOffset]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, existingDate, false);

        _repositoryMock.Verify(r => r.UpdateAsync(It.Is<ReminderOffset>(o => o.IsActive == false), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithRemindFalseAndExistingOffsetInactiveDoesNothingToOffset()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);
        SignificantDate existingDate = new() { Id = Guid.NewGuid(), EventDate = new DateOnly(1990, 5, 15) };
        ReminderOffset existingOffset = new() { Id = Guid.NewGuid(), DaysBeforeEvent = 0, IsActive = false };

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingOffset]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, existingDate, false);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateOrAddBirthdayAsyncWithRemindFalseAndNoExistingOffsetDoesNothingToOffset()
    {
        Guid contactId = Guid.NewGuid();
        DateTime newDate = new(1990, 5, 15);

        _repositoryMock.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ReminderOffset, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await ContactUpdateHelper.UpdateOrAddBirthdayAsync(_repositoryMock.Object, contactId, newDate, null, false);

        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ReminderOffset>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
