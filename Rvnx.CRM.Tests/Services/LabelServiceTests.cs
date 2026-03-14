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
    public async Task UpdateAsyncReturnsNotFoundWhenDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);
        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(Guid.NewGuid(), "NewName", null);
        Assert.True(result.IsNotFound);
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
    public async Task AssignLabelAsyncAddsContactLabelWhenNotExists()
    {
        _mockRepo.Setup(r =>
                r.ListAsync(It.IsAny<Expression<Func<ContactLabel, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _service.AssignLabelAsync(Guid.NewGuid(), Guid.NewGuid());

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
}