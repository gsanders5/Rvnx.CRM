using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class ContactReadServiceGetContactFormTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ContactReadService _service;

    public ContactReadServiceGetContactFormTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ContactReadService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetContactFormAsyncWhenContactDoesNotExistReturnsNull()
    {
        // Arrange
        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        ContactFormDto? result = await _service.GetContactFormAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetContactFormAsyncMapsBasicFieldsCorrectly()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Jane",
            LastName = "Doe",
            MaidenName = "Smith",
            Nickname = "Janie",
            JobTitle = "Engineer",
            Company = "Tech Corp",
            IsHidden = true,
            Pronouns = "She/Her",
            Gender = "Female",
            Religion = "Atheist"
        };

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([contact]);

        _repositoryMock.Setup(r => r.ListAsync<Attachment>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
            It.IsAny<Expression<Func<Label, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        ContactFormDto? result = await _service.GetContactFormAsync(contactId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contact.Id, result.Id);
        Assert.Equal(contact.FirstName, result.FirstName);
        Assert.Equal(contact.LastName, result.LastName);
        Assert.Equal(contact.MaidenName, result.MaidenName);
        Assert.Equal(contact.Nickname, result.Nickname);
        Assert.Equal(contact.JobTitle, result.JobTitle);
        Assert.Equal(contact.Company, result.Company);
        Assert.True(result.IsHidden);
        Assert.Equal(contact.Pronouns, result.Pronouns);
        Assert.Equal(contact.Gender, result.Gender);
        Assert.Equal(contact.Religion, result.Religion);
    }

    [Fact]
    public async Task GetContactFormAsyncPrioritizesPrimaryEmailAndPhone()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "John",
            LastName = "Smith"
        };

        contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Email, Label = "Work", Value = "work@example.com" });
        contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Email, Label = ContactMethodLabels.Primary, Value = "primary@example.com" });

        contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Phone, Label = "Home", Value = "555-1234" });
        contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Phone, Label = ContactMethodLabels.Primary, Value = "555-9999" });

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([contact]);

        _repositoryMock.Setup(r => r.ListAsync<Attachment>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
            It.IsAny<Expression<Func<Label, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        ContactFormDto? result = await _service.GetContactFormAsync(contactId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("primary@example.com", result.Email);
        Assert.Equal("555-9999", result.Phone);
    }

    [Fact]
    public async Task GetContactFormAsyncMapsBirthdayAndReminderCorrectly()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Birthday",
            LastName = "Person"
        };

        DateOnly birthdayDate = new(1990, 5, 15);
        SignificantDate significantDate = new()
        {
            Title = SignificantDateTitles.Birthday,
            EventDate = birthdayDate
        };
        significantDate.ReminderOffsets.Add(new ReminderOffset { DaysBeforeEvent = 0, IsActive = true });

        contact.SignificantDates.Add(significantDate);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([contact]);

        _repositoryMock.Setup(r => r.ListAsync<Attachment>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
            It.IsAny<Expression<Func<Label, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([]);

        // Act
        ContactFormDto? result = await _service.GetContactFormAsync(contactId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(birthdayDate.ToDateTime(TimeOnly.MinValue), result.Birthday);
        Assert.True(result.RemindOnBirthday);
    }

    [Fact]
    public async Task GetContactFormAsyncMapsProfileImageAndLabelsCorrectly()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        Guid profileImageId = Guid.NewGuid();
        Guid labelId = Guid.NewGuid();

        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Image",
            LastName = "Label"
        };
        contact.ContactLabels.Add(new ContactLabel { ContactId = contactId, LabelId = labelId });

        Attachment profileAttachment = new()
        { Id = profileImageId, ContactId = contactId, AttachmentType = AttachmentTypes.ProfileImage };
        List<Label> allLabels =
        [
            new Label { Id = labelId, Name = "Friends", Color = "Red" },
            new Label { Id = Guid.NewGuid(), Name = "Work", Color = "Blue" }
        ];

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync([contact]);

        _repositoryMock.Setup(r => r.ListAsync<Attachment>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync([profileAttachment]);

        _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
            It.IsAny<Expression<Func<Label, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<string[]>()))
            .ReturnsAsync(allLabels);

        // Act
        ContactFormDto? result = await _service.GetContactFormAsync(contactId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(profileImageId, result.ProfileImageId);

        Assert.Equal(2, result.AllLabels.Count);
        Assert.Contains(result.AllLabels, l => l.Name == "Friends");
        Assert.Contains(result.AllLabels, l => l.Name == "Work");

        Assert.Single(result.AssignedLabelIds);
        Assert.Equal(labelId, result.AssignedLabelIds.First());
    }
}