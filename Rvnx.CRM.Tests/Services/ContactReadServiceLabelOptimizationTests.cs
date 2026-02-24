using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactReadServiceLabelOptimizationTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly ContactReadService _service;

        public ContactReadServiceLabelOptimizationTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new ContactReadService(_repositoryMock.Object);
        }

        [Fact]
        public async Task GetContactFormAsyncFetchesLabelsEagerly()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            // Populate ContactLabels for eager loading
            contact.ContactLabels.Add(new ContactLabel { ContactId = contactId, LabelId = labelId });

            // Setup ListAsNoTrackingAsync for Contact
            // We use It.IsAny<string[]> because we are testing if the optimization works regardless of exact includes for now,
            // but we expect "ContactLabels" to be present eventually.
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            // Setup ListAsNoTrackingAsync for Label (all labels query) - return empty or relevant
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Label>(
               It.IsAny<Expression<Func<Label, bool>>>(),
               It.IsAny<CancellationToken>(),
               It.IsAny<string[]>()))
               .ReturnsAsync([]);

            // Setup ListAsNoTrackingAsync for ContactLabel (the separate query we want to eliminate)
            // Return EMPTY list to prove that if it IS called, it would return nothing (simulating DB behavior if we didn't mock it well, but here we explicitly return empty).
            // If the code uses this query, result.AssignedLabelIds will be EMPTY.
            // If the code uses contact.ContactLabels, result.AssignedLabelIds will contain labelId.
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            // Act
            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            // Assert
            Assert.NotNull(result);

            // 1. Verify that the result contains the label ID (proof that eager loading was used)
            Assert.Contains(labelId, result.AssignedLabelIds);

            // 2. Verify that the separate query for ContactLabel was NEVER called
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Never);
        }

        [Fact]
        public async Task GetContactDetailsAsyncFetchesLabelsEagerly()
        {
            // Arrange
            Guid contactId = Guid.NewGuid();
            Guid labelId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };
            Label label = new() { Id = labelId, Name = "Test Label", Color = "Blue" };

            // Populate ContactLabels for eager loading
            contact.ContactLabels.Add(new ContactLabel { ContactId = contactId, LabelId = labelId, Label = label });

            // Setup ListAsNoTrackingAsync for Contact
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([contact]);

            // Setup related entities (needed for GetContactDetailsAsync) to return empty lists to avoid null ref if logic expects them
            // We can rely on default null/empty behavior if logic handles it, but let's be safe
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Relationship>(It.IsAny<Expression<Func<Relationship, bool>>>(), default)).ReturnsAsync([]);

            // Setup ListAsNoTrackingAsync for ContactLabel (the separate query we want to eliminate)
            // Return EMPTY list. If code uses this, result.Labels will be EMPTY.
            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()))
                .ReturnsAsync([]);

            // Act
            ContactDetailDto? result = await _service.GetContactDetailsAsync(contactId);

            // Assert
            Assert.NotNull(result);

            // 1. Verify that the result contains the label (proof that eager loading was used)
            Assert.Single(result.Labels);
            Assert.Equal("Test Label", result.Labels.First().Name);

            // 2. Verify that the separate query for ContactLabel was NEVER called
            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<ContactLabel>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Never);
        }
    }
}
