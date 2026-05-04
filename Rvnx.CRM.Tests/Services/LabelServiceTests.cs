using Moq;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models.Contact;
using Rvnx.CRM.Core.Services;
using System.Linq.Expressions;

namespace Rvnx.CRM.Tests.Services;

public class LabelServiceTests
{
    private readonly Mock<IRepository> _mockRepo;
    private readonly LabelService _service;

    public LabelServiceTests()
    {
        _mockRepo = new Mock<IRepository>();
        _service = new LabelService(_mockRepo.Object);
    }

    [Fact]
    public async Task GetAllAsyncReturnsMappedDtos()
    {
        List<Core.DTOs.Contact.LabelDto> labelDtos =
        [
            new Core.DTOs.Contact.LabelDto { Id = Guid.NewGuid(), Name = "Work", Color = "#ff0000" },
            new Core.DTOs.Contact.LabelDto { Id = Guid.NewGuid(), Name = "Family", Color = "#00ff00" }
        ];

        _mockRepo.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<Expression<Func<Label, Core.DTOs.Contact.LabelDto>>>(),
                It.IsAny<Expression<Func<Label, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(labelDtos);

        List<Core.DTOs.Contact.LabelDto> result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, l => l.Name == "Work" && l.Color == "#ff0000");
    }

    [Fact]
    public async Task CreateAsyncReturnsFailureWhenNameEmpty()
    {
        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("", "#000000");
        Assert.False(result.Success);
        Assert.Contains("Label name cannot be empty.", result.Errors);
    }

    [Fact]
    public async Task CreateAsyncReturnsFailureWhenNameIsWhitespace()
    {
        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("   ", "#000000");
        Assert.False(result.Success);
        Assert.Contains("Label name cannot be empty.", result.Errors);
    }

    [Fact]
    public async Task CreateAsyncReturnsFailureWhenNameExists()
    {
        List<Label> labels = [new Label { Name = "ExistingLabel" }];
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(labels);

        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("existinglabel", "#123456");

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Errors[0]);
    }

    [Fact]
    public async Task CreateAsyncReturnsFailureWhenNameExistsWithDifferentCase()
    {
        List<Label> labels = [new Label { Name = "ExistingLabel" }];
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(labels);

        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("EXISTINGLABEL", "#123456");

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Errors[0]);
    }

    [Fact]
    public async Task CreateAsyncCreatesLabelWhenValid()
    {
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Label());

        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("NewLabel", "#000000");

        Assert.True(result.Success);
        Assert.NotNull(result.LabelId);
        _mockRepo.Verify(
            r => r.AddAsync(It.Is<Label>(l => l.Name == "NewLabel" && l.Color == "#000000"),
                It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncCreatesLabelWhenCandidatesReturnsNull()
    {
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync((List<Label>?)null!);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Label());

        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("NewLabel", "#000000");

        Assert.True(result.Success);
        Assert.NotNull(result.LabelId);
        _mockRepo.Verify(
            r => r.AddAsync(It.Is<Label>(l => l.Name == "NewLabel" && l.Color == "#000000"),
                It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsyncCreatesLabelWhenColorIsNull()
    {
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Label());

        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("NewLabel", null);

        Assert.True(result.Success);
        Assert.NotNull(result.LabelId);
        _mockRepo.Verify(
            r => r.AddAsync(It.Is<Label>(l => l.Name == "NewLabel" && l.Color == null),
                It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncReturnsFailureWhenNameEmpty()
    {
        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(Guid.NewGuid(), "", "#000000");
        Assert.False(result.Success);
        Assert.Contains("Label name cannot be empty.", result.Errors);
    }

    [Fact]
    public async Task UpdateAsyncReturnsFailureWhenNameIsWhitespace()
    {
        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(Guid.NewGuid(), "   ", "#000000");
        Assert.False(result.Success);
        Assert.Contains("Label name cannot be empty.", result.Errors);
    }

    [Fact]
    public async Task UpdateAsyncReturnsNotFoundWhenDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);
        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(Guid.NewGuid(), "NewName", null);
        Assert.True(result.IsNotFound);
    }

    [Fact]
    public async Task DeleteAsyncDeletesLabelWhenExists()
    {
        Guid id = Guid.NewGuid();

        await _service.DeleteAsync(id);

        _mockRepo.Verify(r => r.DeleteAsync<Label>(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncDoesNothingWhenLabelDoesNotExist()
    {
        Guid id = Guid.NewGuid();

        await _service.DeleteAsync(id);

        _mockRepo.Verify(r => r.DeleteAsync<Label>(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncReturnsFailureWhenNameExistsOnDifferentLabel()
    {
        Guid id = Guid.NewGuid();
        Label label = new() { Id = id, Name = "OldName", Color = null };
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(id, It.IsAny<CancellationToken>())).ReturnsAsync(label);

        List<Label> labels = [new Label { Id = Guid.NewGuid(), Name = "ExistingName" }];
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync(labels);

        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(id, "ExistingName", null);

        Assert.False(result.Success);
        Assert.Contains("already exists", result.Errors[0]);
    }

    [Fact]
    public async Task UpdateAsyncUpdatesLabelWhenValid()
    {
        Guid id = Guid.NewGuid();
        Label label = new() { Id = id, Name = "OldName", Color = null };
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(id, It.IsAny<CancellationToken>())).ReturnsAsync(label);
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(id, "NewName", "#123");

        Assert.True(result.Success);
        Assert.Equal("NewName", label.Name);
        Assert.Equal("#123", label.Color);
        _mockRepo.Verify(r => r.UpdateAsync(label, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncDeletesLabelWhenNotFound()
    {
        await _service.DeleteAsync(Guid.NewGuid());

        _mockRepo.Verify(r => r.DeleteAsync<Label>(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsyncDeletesLabelWhenFound()
    {
        Guid id = Guid.NewGuid();

        await _service.DeleteAsync(id);

        _mockRepo.Verify(r => r.DeleteAsync<Label>(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignLabelAsyncAddsContactLabelWhenNotExists()
    {
        _mockRepo.Setup(r =>
                r.CountAsync(It.IsAny<Expression<Func<ContactLabel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        await _service.AssignLabelAsync(Guid.NewGuid(), Guid.NewGuid());

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<ContactLabel>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignLabelAsyncDoesNotAddContactLabelWhenAlreadyExists()
    {
        _mockRepo.Setup(r =>
                r.CountAsync(It.IsAny<Expression<Func<ContactLabel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        await _service.AssignLabelAsync(Guid.NewGuid(), Guid.NewGuid());

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<ContactLabel>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsyncAllowsSameNameWhenUpdatingSelf()
    {
        Guid id = Guid.NewGuid();
        Label label = new() { Id = id, Name = "TestLabel", Color = "#abc" };
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(id, It.IsAny<CancellationToken>())).ReturnsAsync(label);

        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(),
                It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
            .ReturnsAsync([]);

        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(id, "TestLabel", "#abc");

        Assert.True(result.Success);
        _mockRepo.Verify(r => r.UpdateAsync(label, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignLabelAsyncIdempotentWhenCalledMultipleTimes()
    {
        Guid contactId = Guid.NewGuid();
        Guid labelId = Guid.NewGuid();

        _mockRepo.SetupSequence(r =>
                r.CountAsync(It.IsAny<Expression<Func<ContactLabel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0)
            .ReturnsAsync(1);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<ContactLabel>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ContactLabel());

        await _service.AssignLabelAsync(contactId, labelId);
        await _service.AssignLabelAsync(contactId, labelId);

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<ContactLabel>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveLabelAsyncRemovesContactLabelWhenExists()
    {
        await _service.RemoveLabelAsync(Guid.NewGuid(), Guid.NewGuid());

        _mockRepo.Verify(r => r.DeleteAsync(It.IsAny<Expression<Func<ContactLabel, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLabelsForContactAsyncReturnsMappedOrderedDtos()
    {
        Guid contactId = Guid.NewGuid();
        Guid labelId1 = Guid.NewGuid();
        Guid labelId2 = Guid.NewGuid();

        List<Core.DTOs.Contact.LabelDto> labelDtos =
        [
            new Core.DTOs.Contact.LabelDto { Id = labelId2, Name = "Alpha", Color = "#ffffff" },
            new Core.DTOs.Contact.LabelDto { Id = labelId1, Name = "Zeta", Color = "#000000" }
        ];

        _mockRepo.Setup(r => r.ListProjectedAsync<ContactLabel, Core.DTOs.Contact.LabelDto, string>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, Core.DTOs.Contact.LabelDto>>>(),
                It.IsAny<Expression<Func<ContactLabel, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(labelDtos);

        List<Core.DTOs.Contact.LabelDto> result = await _service.GetLabelsForContactAsync(contactId);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal(labelId2, result[0].Id);
        Assert.Equal("#ffffff", result[0].Color);

        Assert.Equal("Zeta", result[1].Name);
        Assert.Equal(labelId1, result[1].Id);
        Assert.Equal("#000000", result[1].Color);
    }

    [Fact]
    public async Task GetLabelsForContactAsyncReturnsEmptyListWhenNoLabelsFound()
    {
        Guid contactId = Guid.NewGuid();

        _mockRepo.Setup(r => r.ListProjectedAsync<ContactLabel, Core.DTOs.Contact.LabelDto, string>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, Core.DTOs.Contact.LabelDto>>>(),
                It.IsAny<Expression<Func<ContactLabel, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<Core.DTOs.Contact.LabelDto> result = await _service.GetLabelsForContactAsync(contactId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetLabelsForContactAsyncReturnsEmptyListWhenRepositoryReturnsNull()
    {
        Guid contactId = Guid.NewGuid();

        _mockRepo.Setup(r => r.ListProjectedAsync<ContactLabel, Core.DTOs.Contact.LabelDto, string>(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, Core.DTOs.Contact.LabelDto>>>(),
                It.IsAny<Expression<Func<ContactLabel, string>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<Core.DTOs.Contact.LabelDto>?)null!);

        List<Core.DTOs.Contact.LabelDto> result = await _service.GetLabelsForContactAsync(contactId);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task BulkAssignLabelAsyncReturnsFailureWhenLabelMissing()
    {
        Guid labelId = Guid.NewGuid();
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(labelId, It.IsAny<CancellationToken>())).ReturnsAsync((Label?)null);

        Core.DTOs.Base.BulkOperationResult result = await _service.BulkAssignLabelAsync([Guid.NewGuid()], labelId);

        Assert.Equal(0, result.Successful);
        Assert.Contains("Label not found.", result.Errors);
    }

    [Fact]
    public async Task BulkAssignLabelAsyncSkipsAlreadyAssigned()
    {
        Guid labelId = Guid.NewGuid();
        Guid contactA = Guid.NewGuid();
        Guid contactB = Guid.NewGuid();

        _mockRepo.Setup(r => r.GetByIdAsync<Label>(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Label { Id = labelId, Name = "Test" });

        _mockRepo.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([contactA]);

        _mockRepo.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<Contact, bool>>>(),
                It.IsAny<Expression<Func<Contact, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([contactA, contactB]);

        Core.DTOs.Base.BulkOperationResult result = await _service.BulkAssignLabelAsync([contactA, contactB], labelId);

        Assert.Equal(1, result.Successful);
        Assert.Equal(1, result.Skipped);
        _mockRepo.Verify(r => r.AddRangeAsync(
            It.Is<IEnumerable<ContactLabel>>(list => list.Count() == 1 && list.First().ContactId == contactB),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task BulkRemoveLabelAsyncDeletesMatchingPairs()
    {
        Guid labelId = Guid.NewGuid();
        Guid contactA = Guid.NewGuid();
        Guid contactB = Guid.NewGuid();

        _mockRepo.Setup(r => r.ListProjectedAsync(
                It.IsAny<Expression<Func<ContactLabel, bool>>>(),
                It.IsAny<Expression<Func<ContactLabel, Guid>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([contactA]);

        Core.DTOs.Base.BulkOperationResult result = await _service.BulkRemoveLabelAsync([contactA, contactB], labelId);

        Assert.Equal(1, result.Successful);
        Assert.Equal(1, result.Skipped);
        _mockRepo.Verify(r => r.DeleteAsync<ContactLabel>(
            It.IsAny<Expression<Func<ContactLabel, bool>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

}
