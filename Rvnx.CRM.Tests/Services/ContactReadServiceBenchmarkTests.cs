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

namespace Rvnx.CRM.Tests.Services
{
    public class ContactReadServiceBenchmarkTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly ContactReadService _service;

        public ContactReadServiceBenchmarkTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new ContactReadService(_repositoryMock.Object);
        }

        [Fact]
        public async Task GetContactFormAsyncPerformanceOptimized()
        {
            Guid contactId = Guid.NewGuid();
            Contact contact = new() { Id = contactId, FirstName = "Test", LastName = "User" };

            // Populate collections for the test to ensure extraction logic works
            contact.ContactMethods.Add(new ContactMethod { Type = ContactMethodType.Email, Value = "test@example.com", Label = ContactMethodLabels.Primary });
            contact.SignificantDates.Add(new SignificantDate { Title = SignificantDateTitles.Birthday, Date = new DateTime(1990, 1, 1) });

            _repositoryMock.Setup(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.Is<string[]>(s => s.Contains("ContactMethods") && s.Contains("SignificantDates"))))
                .ReturnsAsync([contact]);

            _repositoryMock.Setup(r => r.ListAsync<Attachment>(It.IsAny<Expression<Func<Attachment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            ContactFormDto? result = await _service.GetContactFormAsync(contactId);

            Assert.NotNull(result);
            Assert.Equal("test@example.com", result.Email);
            Assert.Equal(new DateTime(1990, 1, 1), result.Birthday);

            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<Contact>(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<string[]>()), Times.Once);

            _repositoryMock.Verify(r => r.GetByIdAsync<Contact>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<ContactMethod>(
               It.IsAny<Expression<Func<ContactMethod, bool>>>(),
               It.IsAny<CancellationToken>(),
               It.IsAny<string[]>()), Times.Never);

            _repositoryMock.Verify(r => r.ListAsNoTrackingAsync<SignificantDate>(
               It.IsAny<Expression<Func<SignificantDate, bool>>>(),
               It.IsAny<CancellationToken>(),
               It.IsAny<string[]>()), Times.Never);
        }
    }
}
