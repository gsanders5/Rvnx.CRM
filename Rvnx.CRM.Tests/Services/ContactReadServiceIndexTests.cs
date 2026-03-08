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

public class ContactReadServiceIndexTests
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly ContactReadService _service;

    public ContactReadServiceIndexTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new ContactReadService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetIndexDataAsyncCorrectlyRestitchesBulkLoadedRelatedEntities()
    {
        var contact1Id = Guid.NewGuid();
        var contact2Id = Guid.NewGuid();
        var attachmentId = Guid.NewGuid();
        var labelId = Guid.NewGuid();

        var contacts = new List<ContactDto>
        {
            new ContactDto { Id = contact1Id, FirstName = "Alice" },
            new ContactDto { Id = contact2Id, FirstName = "Bob" }
        };

        _repositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, ContactDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        _repositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid)> { (contact1Id, attachmentId) });

        _repositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
            It.IsAny<Expression<Func<ContactLabel, bool>>>(),
            It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid, string, string?)> { (contact2Id, labelId, "Friend", "Blue") });

        _repositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
            It.IsAny<Expression<Func<SignificantDate, bool>>>(),
            It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, DateOnly)> { (contact1Id, new DateOnly(1990, 5, 10)) });

        var result = await _service.GetIndexDataAsync(false);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var alice = result.First(c => c.Id == contact1Id);
        var bob = result.First(c => c.Id == contact2Id);

        Assert.Equal(attachmentId, alice.ProfileImageId);
        Assert.Empty(alice.Labels);
        Assert.Equal(new DateTime(1990, 5, 10), alice.Birthday);

        Assert.Null(bob.ProfileImageId);
        Assert.Single(bob.Labels);
        Assert.Equal(labelId, bob.Labels.First().Id);
        Assert.Equal("Friend", bob.Labels.First().Name);
        Assert.Equal("Blue", bob.Labels.First().Color);
        Assert.Null(bob.Birthday);
    }

    [Fact]
    public async Task GetIndexDataAsyncGracefullyHandlesMissingOrDuplicateKeys()
    {
        var contact1Id = Guid.NewGuid();
        var contact2Id = Guid.NewGuid();
        var attachment1Id = Guid.NewGuid();
        var attachment2Id = Guid.NewGuid();

        var contacts = new List<ContactDto>
        {
            new ContactDto { Id = contact1Id, FirstName = "Alice" },
            new ContactDto { Id = contact2Id, FirstName = "Bob" } // Bob has no attachments returned
        };

        _repositoryMock.Setup(x => x.ListProjectedAsync<Contact, ContactDto>(
            It.IsAny<Expression<Func<Contact, bool>>>(),
            It.IsAny<Expression<Func<Contact, ContactDto>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(contacts);

        // Alice has two profile images (a duplicate condition the system should handle gracefully by picking one)
        // Also returning an attachment for a contact ID that does NOT exist in our DTO list (e.g., an orphaned or mistmatched record)
        var missingContactId = Guid.NewGuid();
        _repositoryMock.Setup(x => x.ListProjectedAsync<Attachment, (Guid, Guid)>(
            It.IsAny<Expression<Func<Attachment, bool>>>(),
            It.IsAny<Expression<Func<Attachment, (Guid, Guid)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid)> {
                (contact1Id, attachment1Id),
                (contact1Id, attachment2Id), // Duplicate
                (missingContactId, Guid.NewGuid()) // Key not in map
            });

        _repositoryMock.Setup(x => x.ListProjectedAsync<ContactLabel, (Guid, Guid, string, string?)>(
            It.IsAny<Expression<Func<ContactLabel, bool>>>(),
            It.IsAny<Expression<Func<ContactLabel, (Guid, Guid, string, string?)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, Guid, string, string?)>());

        _repositoryMock.Setup(x => x.ListProjectedAsync<SignificantDate, (Guid, DateOnly)>(
            It.IsAny<Expression<Func<SignificantDate, bool>>>(),
            It.IsAny<Expression<Func<SignificantDate, (Guid, DateOnly)>>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(Guid, DateOnly)>());

        var result = await _service.GetIndexDataAsync(false);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var alice = result.First(c => c.Id == contact1Id);
        var bob = result.First(c => c.Id == contact2Id);

        // Alice should have one of the profile images (it uses GroupBy.First(), so it should be attachment1Id)
        Assert.Equal(attachment1Id, alice.ProfileImageId);

        // Bob should have null ProfileImageId, since he had no attachments in the db query
        Assert.Null(bob.ProfileImageId);

        // Ensure no exception was thrown by the missingContactId
    }
}
