using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Enumerations;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services
{
    public class ContactImportServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IVCardService> _vCardServiceMock;
        private readonly Mock<ILogger<ContactImportService>> _loggerMock;
        private readonly ContactImportService _service;

        public ContactImportServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _vCardServiceMock = new Mock<IVCardService>();
            _loggerMock = new Mock<ILogger<ContactImportService>>();

            _loggerMock.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            _service = new ContactImportService(_repositoryMock.Object, _vCardServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task ImportFromVCardAsyncValidContactsAddsToRepository()
        {
            // Arrange
            List<Contact> contacts = new()
            {
                new() { FirstName = "John", LastName = "Doe" },
                new() { FirstName = "Jane", LastName = "Smith" }
            };

            _vCardServiceMock.Setup(v => v.ParseVCard(It.IsAny<Stream>()))
                .Returns(contacts);

            // Mock no duplicates found
            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Contact, bool>>>(), default))
                .ReturnsAsync(0);
            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default))
                .ReturnsAsync(0);

            using MemoryStream stream = new();

            // Act
            ContactImportResult result = await _service.ImportFromVCardAsync(stream);

            // Assert
            Assert.Equal(2, result.AddedCount);
            Assert.Equal(0, result.SkippedCount);

            _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "John"), default), Times.Once);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<Contact>(c => c.FirstName == "Jane"), default), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Exactly(2));
        }

        [Fact]
        public async Task ImportFromVCardAsyncDuplicateNameSkipsImport()
        {
            // Arrange
            List<Contact> contacts = new()
            {
                new() { FirstName = "Duplicate", LastName = "User" }
            };

            _vCardServiceMock.Setup(v => v.ParseVCard(It.IsAny<Stream>()))
                .Returns(contacts);

            // Mock duplicate found by name
            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Contact, bool>>>(), default))
                .ReturnsAsync(1);

            using MemoryStream stream = new();

            // Act
            ContactImportResult result = await _service.ImportFromVCardAsync(stream);

            // Assert
            Assert.Equal(0, result.AddedCount);
            Assert.Equal(1, result.SkippedCount);

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), default), Times.Never);
            _repositoryMock.Verify(r => r.SaveChangesAsync(default), Times.Never);
        }

        [Fact]
        public async Task ImportFromVCardAsyncDuplicateContactMethodSkipsImport()
        {
            // Arrange
            Contact contact = new()
            {
                FirstName = "Unique",
                LastName = "User",
                ContactMethods = new List<ContactMethod>
                {
                    new() { Type = ContactMethodType.Email, Value = "test@example.com" }
                }
            };
            List<Contact> contacts = new()
            { contact };

            _vCardServiceMock.Setup(v => v.ParseVCard(It.IsAny<Stream>()))
                .Returns(contacts);

            // Mock unique name
            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<Contact, bool>>>(), default))
                .ReturnsAsync(0);

            // Mock duplicate method found
            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<Expression<Func<ContactMethod, bool>>>(), default))
                .ReturnsAsync(1);

            using MemoryStream stream = new();

            // Act
            ContactImportResult result = await _service.ImportFromVCardAsync(stream);

            // Assert
            Assert.Equal(0, result.AddedCount);
            Assert.Equal(1, result.SkippedCount);

            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Contact>(), default), Times.Never);
        }

        [Fact]
        public async Task ImportFromVCardAsyncExceptionInParsingLogsAndRethrows()
        {
            // Arrange
            _vCardServiceMock.Setup(v => v.ParseVCard(It.IsAny<Stream>()))
                .Throws(new InvalidOperationException("Parse error"));

            using MemoryStream stream = new();

            // Act & Assert
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.ImportFromVCardAsync(stream));
            Assert.Equal("Parse error", ex.Message);

            // Verify logging
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error importing VCF")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
