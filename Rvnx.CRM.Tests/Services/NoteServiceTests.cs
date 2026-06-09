using Moq;
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
    private readonly Mock<IContactLookupService> _contactLookupServiceMock;
    private readonly NoteService _service;

    public NoteServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _contactLookupServiceMock = new Mock<IContactLookupService>();
        _service = new NoteService(_repositoryMock.Object, _contactLookupServiceMock.Object);
    }

    [Fact]
    public async Task CreateAsyncWithValidDataReturnsSuccess()
    {
        Guid contactId = Guid.NewGuid();
        NoteFormViewModel dto = new()
        {
            ContactId = contactId,
            Title = "New Note",
            Value = "Note content"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note());

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.CreateAsync(dto);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncWhenContactNotFoundReturnsFailure()
    {
        NoteFormViewModel dto = new()
        {
            ContactId = Guid.NewGuid(),
            Title = "New Note",
            Value = "Note content"
        };

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // IsValidContactAsync

        OperationResult result = await _service.CreateAsync(dto);

        Assert.False(result.Success);
        Assert.Equal("Contact not found.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Never);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncWithValidDataReturnsSuccess()
    {
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Note existingNote = new()
        { Id = noteId, ContactId = contactId, Title = "Old Title" };
        NoteFormViewModel dto = new()
        { Id = noteId, ContactId = contactId, Title = "New Title" };

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1); // IsValidContactAsync

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note());

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.UpdateAsync(noteId, dto);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncWhenNoteExistsDeletesAndReturnsSuccess()
    {
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListProjectedAsync<Note, Guid?>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Note, Guid?>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([contactId]);

        _repositoryMock.Setup(r => r.DeleteAsync<Note>(It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.DeleteAsync(noteId);

        Assert.True(result.Success);
        Assert.Equal(contactId, result.RedirectId);
        _repositoryMock.Verify(r => r.DeleteAsync<Note>(It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncWhenNoteDoesNotExistReturnsFailure()
    {
        Guid noteId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.ListProjectedAsync<Note, Guid?>(
                It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(),
                It.IsAny<System.Linq.Expressions.Expression<Func<Note, Guid?>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        OperationResult result = await _service.DeleteAsync(noteId);

        Assert.False(result.Success);
        Assert.Equal("Note not found.", result.ErrorMessage);
        _repositoryMock.Verify(r => r.DeleteAsync<Note>(It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
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
        { Id = noteId, ContactId = contactId, Title = "Updated Note" };

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
        { Id = noteId, ContactId = contactId, Title = "Updated Note" };

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

    [Fact]
    public async Task CreateAsyncWithIsFavoriteTruePersistsFlag()
    {
        Guid contactId = Guid.NewGuid();
        NoteFormViewModel dto = new()
        {
            ContactId = contactId,
            Title = "Pinned Note",
            Value = "Important content",
            IsFavorite = true
        };

        Note? captured = null;

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .Callback<Note, CancellationToken>((n, _) => captured = n)
            .ReturnsAsync((Note n, CancellationToken _) => n);

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.CreateAsync(dto);

        Assert.True(result.Success);
        Assert.NotNull(captured);
        Assert.True(captured!.IsFavorite);
    }

    [Fact]
    public async Task UpdateAsyncTogglingIsFavoritePersistsFlag()
    {
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Note existingNote = new()
        {
            Id = noteId,
            ContactId = contactId,
            Title = "Existing",
            Value = "Body",
            IsFavorite = false
        };
        NoteFormViewModel dto = new()
        {
            Id = noteId,
            ContactId = contactId,
            Title = "Existing",
            Value = "Body",
            IsFavorite = true
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note());

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.UpdateAsync(noteId, dto);

        Assert.True(result.Success);
        Assert.True(existingNote.IsFavorite);
    }

    [Fact]
    public async Task GetByContactAsyncPreservesIsFavoriteOnReturnedDtos()
    {
        Guid contactId = Guid.NewGuid();
        List<Note> notes =
        [
            new() { Id = Guid.NewGuid(), ContactId = contactId, Title = "Pinned", Value = "x", IsFavorite = true },
            new() { Id = Guid.NewGuid(), ContactId = contactId, Title = "Normal", Value = "y", IsFavorite = false }
        ];

        _repositoryMock.Setup(r => r.ListAsync(
                It.IsAny<System.Linq.Expressions.Expression<Func<Note, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(notes);

        List<NoteDto> result = await _service.GetByContactAsync(contactId);

        Assert.Equal(2, result.Count);
        Assert.True(result.Single(n => n.Title == "Pinned").IsFavorite);
        Assert.False(result.Single(n => n.Title == "Normal").IsFavorite);
    }

    [Fact]
    public async Task ToggleFavoriteAsyncFlipsTheFlagAndSaves()
    {
        Guid noteId = Guid.NewGuid();
        Guid contactId = Guid.NewGuid();
        Note existingNote = new()
        {
            Id = noteId,
            ContactId = contactId,
            Title = "Test",
            Value = "v",
            IsFavorite = false
        };

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingNote);

        _repositoryMock.Setup(r => r.CountAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Contact, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note());

        _repositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        OperationResult result = await _service.ToggleFavoriteAsync(noteId);

        Assert.True(result.Success);
        Assert.True(existingNote.IsFavorite);
        Assert.Equal(contactId, result.RedirectId);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ToggleFavoriteAsyncWhenNoteMissingReturnsNotFound()
    {
        Guid noteId = Guid.NewGuid();

        _repositoryMock.Setup(r => r.GetByIdAsync<Note>(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Note?)null);

        OperationResult result = await _service.ToggleFavoriteAsync(noteId);

        Assert.False(result.Success);
        Assert.True(result.IsNotFound);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Note>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
