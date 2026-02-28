using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Dates;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Dates;
using Rvnx.CRM.Infrastructure.Services;
using Rvnx.CRM.Core.Extensions;
using Rvnx.CRM.Core.Models.Contact;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Rvnx.CRM.Tests.Services
{
    public class SignificantDateServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly SignificantDateService _service;

        public SignificantDateServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _service = new SignificantDateService(_repositoryMock.Object);
        }

        [Fact]
        public async Task CreateAsyncWithValidContactCreatesSignificantDate()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var dto = new SignificantDateDto
            {
                EntityId = contactId,
                Title = "Anniversary",
                Date = new DateTime(2020, 5, 15),
                RemindMe = true,
                EventFrequency = TimeSpan.FromDays(30)
            };

            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Should not check for existing birthday since title isn't Birthday

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>())).ReturnsAsync(new SignificantDate());
            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(contactId, result.RedirectId);
            _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd =>
                sd.Title == "Anniversary" &&
                sd.Date == new DateTime(2020, 5, 15) &&
                sd.ContactId == contactId &&
                sd.RemindMe == true &&
                sd.EventFrequency == TimeSpan.FromDays(30)
            ), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsyncWithInvalidContactReturnsFailure()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var dto = new SignificantDateDto { EntityId = contactId };

            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Contact not found.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsyncWithBirthdayTitleSetsYearlyFrequency()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var dto = new SignificantDateDto
            {
                EntityId = contactId,
                Title = "Birthday", // Title is Birthday
                EventFrequency = TimeSpan.Zero // Initially 0
            };

            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Simulate no existing birthday
            _repositoryMock.Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            _repositoryMock.Setup(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>())).ReturnsAsync(new SignificantDate());

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.True(result.Success);
            // Verify frequency was overridden to 365 days
            _repositoryMock.Verify(r => r.AddAsync(It.Is<SignificantDate>(sd =>
                sd.EventFrequency == TimeSpan.FromDays(365)
            ), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsyncWithExistingBirthdayReturnsFailure()
        {
            // Arrange
            var contactId = Guid.NewGuid();
            var dto = new SignificantDateDto
            {
                EntityId = contactId,
                Title = "birthday" // Case insensitive match
            };

            // Setup IsValidContactAsync (via CountAsync<Contact>)
            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Simulate an existing birthday
            _repositoryMock.Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.CreateAsync(dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("A birthday is already set for this contact.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.AddAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsyncWithBirthdayTitleSetsYearlyFrequency()
        {
            // Arrange
            var id = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var existingDate = new SignificantDate
            {
                Id = id,
                ContactId = contactId,
                Title = "Other Event"
            };

            var dto = new SignificantDateDto
            {
                Id = id,
                EntityId = contactId,
                Title = "Birthday", // Changing to Birthday
                EventFrequency = TimeSpan.Zero // Initial value
            };

            _repositoryMock.Setup(r => r.GetByIdAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync(existingDate);
            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Simulate no existing birthday (other than potentially itself)
            _repositoryMock.Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(0);

            // Act
            var result = await _service.UpdateAsync(id, dto);

            // Assert
            Assert.True(result.Success);
            _repositoryMock.Verify(r => r.UpdateAsync(It.Is<SignificantDate>(sd =>
                sd.Id == id &&
                sd.Title == "Birthday" &&
                sd.EventFrequency == TimeSpan.FromDays(365) // Overridden
            ), It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsyncWithExistingBirthdayReturnsFailure()
        {
            // Arrange
            var id = Guid.NewGuid();
            var contactId = Guid.NewGuid();

            var existingDate = new SignificantDate
            {
                Id = id,
                ContactId = contactId,
                Title = "Other Event"
            };

            var dto = new SignificantDateDto
            {
                Id = id,
                EntityId = contactId,
                Title = "Birthday"
            };

            _repositoryMock.Setup(r => r.GetByIdAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync(existingDate);
            // Setup IsValidContactAsync (via CountAsync<Contact>)
            _repositoryMock.Setup(r => r.CountAsync<Contact>(It.IsAny<Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Simulate an existing birthday for this contact
            _repositoryMock.Setup(r => r.CountAsync<SignificantDate>(It.IsAny<Expression<Func<SignificantDate, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1);

            // Act
            var result = await _service.UpdateAsync(id, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("A birthday is already set for this contact.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<SignificantDate>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsyncWithNotFoundDateReturnsFailure()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync((SignificantDate?)null);

            // Act
            var result = await _service.UpdateAsync(id, new SignificantDateDto());

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Significant date not found.", result.ErrorMessage);
        }

        [Fact]
        public async Task DeleteAsyncWithValidIdReturnsOkAndRedirectsToContact()
        {
            // Arrange
            var id = Guid.NewGuid();
            var contactId = Guid.NewGuid();
            var existingDate = new SignificantDate
            {
                Id = id,
                ContactId = contactId
            };

            _repositoryMock.Setup(r => r.GetByIdAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync(existingDate);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.True(result.Success);
            Assert.Equal(contactId, result.RedirectId);
            Assert.Equal(EntityTypes.Person, result.RedirectType);

            _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(id, It.IsAny<CancellationToken>()), Times.Once);
            _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsyncWithNotFoundIdReturnsFailure()
        {
            // Arrange
            var id = Guid.NewGuid();
            _repositoryMock.Setup(r => r.GetByIdAsync<SignificantDate>(id, It.IsAny<CancellationToken>())).ReturnsAsync((SignificantDate?)null);

            // Act
            var result = await _service.DeleteAsync(id);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Significant date not found.", result.ErrorMessage);
            _repositoryMock.Verify(r => r.DeleteAsync<SignificantDate>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
