using Moq;
using Rvnx.CRM.Core.Constants;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Exceptions;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using Rvnx.CRM.Core.Models.Base;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Infrastructure.Services;

namespace Rvnx.CRM.Tests.Services;

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
    public async Task CreateAsyncWithValidDataReturnsSuccess()
    {
        // Arrange
        Guid contactId = Guid.NewGuid();
        NoteFormViewModel dto = new()
        {
            EntityId = contactId,
            EntityType = EntityTypes.Person,
            Title = "New Note",
            Value = "Note content"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note());

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        Assert.Equal(EntityTypes.Person, result.RedirectType);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncWithInvalidEntityTypeReturnsFailure()
    {
        // Arrange
        NoteFormViewModel dto = new()
        {
            EntityId = Guid.NewGuid(),
            EntityType = EntityTypes.Company,
            Title = "New Note",
            Value = "Note content"
        };

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Only Person entities are supported.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsyncWhenContactNotFoundReturnsFailure()
    {
        // Arrange
        NoteFormViewModel dto = new()
        {
            EntityId = Guid.NewGuid(),
            EntityType = EntityTypes.Person,
            Title = "New Note",
            Value = "Note content"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // IsValidContactAsync

        // Act
        OperationResult result = await _service.CreateAsync(dto);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Contact not found.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncWithValidDataReturnsSuccess()
    {
        // Arrange
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Note existingNote = new()
        { Id = noteId, ContactId = contactId, Title = "Old Title" };
        NoteFormViewModel dto = new()
        { Id = noteId, EntityId = contactId, EntityType = EntityTypes.Person, Title = "New Title" };

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note());

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OperationResult result = await _service.UpdateAsync(noteId, dto);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        Assert.Equal(EntityTypes.Person, result.RedirectType);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncWhenNoteExistsDeletesAndReturnsSuccess()
    {
        // Arrange
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Note existingNote = new() { Id = noteId, ContactId = contactId };

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _repositoryMock.Setup(r => r.DeleteAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OperationResult result = await _service.DeleteAsync(noteId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        Assert.Equal(EntityTypes.Person, result.RedirectType);
        _repositoryMock.Verify(r => r.DeleteAsync<Note>(noteId, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncWhenNoteDoesNotExistReturnsFailure()
    {
        // Arrange
        Guid noteId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Note?)null);

        // Act
        OperationResult result = await _service.DeleteAsync(noteId);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Note not found.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.DeleteAsync<Note>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenNoteExistsRethrows()
    {
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Note existingNote = new()
        { Id = noteId, ContactId = contactId, Title = "Test Note" };
        NoteFormViewModel dto = new()
        { Id = noteId, EntityId = contactId, EntityType = EntityTypes.Person, Title = "Updated Note" };

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

        _repositoryMock.Setup(r => r.ExistsAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await Assert.ThrowsAsync<EntityConcurrencyException>(() => _service.UpdateAsync(noteId, dto));
    }

    [Fact]
    public async Task UpdateAsyncThrowsEntityConcurrencyExceptionWhenNoteDoesNotExistReturnsFailure()
    {
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Note existingNote = new()
        { Id = noteId, ContactId = contactId, Title = "Test Note" };
        NoteFormViewModel dto = new()
        { Id = noteId, EntityId = contactId, EntityType = EntityTypes.Person, Title = "Updated Note" };

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new EntityConcurrencyException("Concurrency conflict"));

        _repositoryMock.Setup(r => r.ExistsAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        OperationResult result = await _service.UpdateAsync(noteId, dto);

        Assert.False(result.Success);
        Assert.Equal("Note not found.", result.ErrorMessage);
    }
}