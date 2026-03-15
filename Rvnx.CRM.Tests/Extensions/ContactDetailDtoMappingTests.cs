using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Models.Dates;

namespace Rvnx.CRM.Tests.Extensions;

public class ContactDetailDtoMappingTests
{
    [Fact]
    public void ToDetailDtoShouldMapSimplePropertiesCorrectly()
    {
        Contact contact = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "TestFirst",
            LastName = "TestLast",
            Nickname = "Tester",
            JobTitle = "Developer",
            Company = "TechCorp",
            IsHidden = false,
            Pronouns = "They/Them",
            Gender = "Non-binary",
            Religion = "Agnostic",
            ProfileImageId = Guid.NewGuid(),
            IsPartial = false
        };

        ContactDetailDto result = contact.ToDetailDto();

        Assert.Equal(contact.Id, result.Id);
        Assert.Equal(contact.FirstName, result.FirstName);
        Assert.Equal(contact.LastName, result.LastName);
        Assert.Equal(contact.FullName, result.FullName); // calculated property
        Assert.Equal(contact.Nickname, result.Nickname);
        Assert.Equal(contact.JobTitle, result.JobTitle);
        Assert.Equal(contact.Company, result.Company);
        Assert.Equal(contact.IsHidden, result.IsHidden);
        Assert.Equal(contact.Pronouns, result.Pronouns);
        Assert.Equal(contact.Gender, result.Gender);
        Assert.Equal(contact.Religion, result.Religion);
        Assert.Equal(contact.ProfileImageId, result.ProfileImageId);
        Assert.Equal(contact.IsPartial, result.IsPartial);
    }

    [Fact]
    public void ToDetailDtoShouldMapCollectionsCorrectly()
    {
        Guid contactId = Guid.NewGuid();
        Contact contact = new()
        {
            Id = contactId,
            FirstName = "Test",
            LastName = "Contact",
            Notes = [new Note { Id = Guid.NewGuid(), Title = "Note1", Value = "Content1", ContactId = contactId }],
            SignificantDates = [new SignificantDate { Id = Guid.NewGuid(), Title = "Birthday", ContactId = contactId }],
            Relationships = [new Relationship { Id = Guid.NewGuid(), EntityId = contactId, RelatedEntityId = Guid.NewGuid() }],
            RelatedTo = [new Relationship { Id = Guid.NewGuid(), EntityId = Guid.NewGuid(), RelatedEntityId = contactId }],
            ContactMethods = [new ContactMethod { Id = Guid.NewGuid(), Label = "Email", Value = "test@example.com", ContactId = contactId }],
            Facts = [new Fact { Id = Guid.NewGuid(), Category = "Fun Fact", Value = "Likes testing", ContactId = contactId }],
            Attachments = [new Attachment { Id = Guid.NewGuid(), FileName = "test.pdf", ContactId = contactId }]
        };

        ContactDetailDto result = contact.ToDetailDto();

        Assert.Single(result.Notes);
        Assert.Equal(contact.Notes.First().Id, result.Notes.First().Id);

        Assert.Single(result.SignificantDates);
        Assert.Equal(contact.SignificantDates.First().Id, result.SignificantDates.First().Id);

        Assert.Single(result.Relationships);
        Assert.Equal(contact.Relationships.First().Id, result.Relationships.First().Id);

        Assert.Single(result.RelatedTo);
        Assert.Equal(contact.RelatedTo.First().Id, result.RelatedTo.First().Id);

        Assert.Single(result.ContactMethods);
        Assert.Equal(contact.ContactMethods.First().Id, result.ContactMethods.First().Id);

        Assert.Single(result.Facts);
        Assert.Equal(contact.Facts.First().Id, result.Facts.First().Id);

        Assert.Single(result.Attachments);
        Assert.Equal(contact.Attachments.First().Id, result.Attachments.First().Id);
    }

    [Fact]
    public void ToDetailDtoShouldHandleNullCollectionsReturnsEmptyLists()
    {
        Contact contact = new()
        {
            Id = Guid.NewGuid(),
            FirstName = "Test",
            LastName = "Contact",
            // Explicitly setting collections to null (default behavior, but being explicit for test clarity)
            // Using null! to suppress nullable warnings because we are testing behavior when they are null
            Notes = null!,
            SignificantDates = null!,
            Relationships = null!,
            RelatedTo = null!,
            ContactMethods = null!,
            Facts = null!,
            Attachments = null!
        };

        ContactDetailDto result = contact.ToDetailDto();

        Assert.NotNull(result.Notes);
        Assert.Empty(result.Notes);

        Assert.NotNull(result.SignificantDates);
        Assert.Empty(result.SignificantDates);

        Assert.NotNull(result.Relationships);
        Assert.Empty(result.Relationships);

        Assert.NotNull(result.RelatedTo);
        Assert.Empty(result.RelatedTo);

        Assert.NotNull(result.ContactMethods);
        Assert.Empty(result.ContactMethods);

        Assert.NotNull(result.Facts);
        Assert.Empty(result.Facts);

        Assert.NotNull(result.Attachments);
        Assert.Empty(result.Attachments);

        // Pets are not mapped in ToDetailDto, so we don't assert on them here based on the provided code snippet
    }
}