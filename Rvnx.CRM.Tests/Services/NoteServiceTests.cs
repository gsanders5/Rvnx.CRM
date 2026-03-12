using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;
using Xunit;

namespace Rvnx.CRM.Tests.Services
{
    public class NoteServiceTests
    {
        private readonly Mock<IRepository> _repositoryMock;
        private readonly Mock<IEntityService> _entityServiceMock;
        private readonly NoteService _service;

        public NoteServiceTests()
        {
            _repositoryMock = new Mock<IRepository>();
            _entityServiceMock = new Mock<IEntityService>();
            _service = new NoteService(_repositoryMock.Object, _entityServiceMock.Object);
        }

        [Fact]
        public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenNoteExistsRethrows()
        {
            // Arrange
            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            Note existingNote = new Note { Id = noteId, ContactId = contactId, Title = "Test Note" };
            NoteFormViewModel dto = new NoteFormViewModel { Id = noteId, EntityId = contactId, EntityType = EntityTypes.Person, Title = "Updated Note" };

            _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingNote);

            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // IsValidContactAsync

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

            _repositoryMock.Setup(r => r.ExistsAsync<Note>(noteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<EntityConcurrencyException>(() => _service.UpdateAsync(noteId, dto));
        }

        [Fact]
        public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenNoteDoesNotExistReturnsFailure()
        {
            // Arrange
            Guid noteId = Guid.NewGuid();
            Guid contactId = Guid.NewGuid();
            Note existingNote = new Note { Id = noteId, ContactId = contactId, Title = "Test Note" };
            NoteFormViewModel dto = new NoteFormViewModel { Id = noteId, EntityId = contactId, EntityType = EntityTypes.Person, Title = "Updated Note" };

            _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingNote);

            _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1); // IsValidContactAsync

            _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

            _repositoryMock.Setup(r => r.ExistsAsync<Note>(noteId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            OperationResult result = await _service.UpdateAsync(noteId, dto);

            // Assert
            Assert.False(result.Success);
            Assert.Equal("Note not found.", result.ErrorMessage);
        }
    }
}
