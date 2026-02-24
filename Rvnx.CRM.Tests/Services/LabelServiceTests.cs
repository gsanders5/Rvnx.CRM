using FluentAssertions;
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
        // Arrange
        List<Label> labels =
        [
            new Label { Id = Guid.NewGuid(), Name = "Work", Color = "#ff0000" },
            new Label { Id = Guid.NewGuid(), Name = "Family", Color = "#00ff00" }
        ];
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync(labels);

        // Act
        List<Core.DTOs.Contact.LabelDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(l => l.Name == "Work" && l.Color == "#ff0000");
    }

    [Fact]
    public async Task CreateAsyncReturnsFailureWhenNameEmpty()
    {
        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("", "#000000");
        result.Success.Should().BeFalse();
        result.Errors.Should().Contain("Label name cannot be empty.");
    }

    [Fact]
    public async Task CreateAsyncReturnsFailureWhenNameExists()
    {
        List<Label> labels = [new Label { Name = "ExistingLabel" }];
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync(labels);

        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("existinglabel", "#123456");

        result.Success.Should().BeFalse();
        result.Errors[0].Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateAsyncCreatesLabelWhenValid()
    {
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync([]);

        _mockRepo.Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Label());

        Core.DTOs.Contact.LabelOperationResult result = await _service.CreateAsync("NewLabel", "#000000");

        result.Success.Should().BeTrue();
        result.LabelId.Should().NotBeNull();
        _mockRepo.Verify(r => r.AddAsync(It.Is<Label>(l => l.Name == "NewLabel" && l.Color == "#000000"), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsyncReturnsNotFoundWhenDoesNotExist()
    {
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Label?)null);
        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(Guid.NewGuid(), "NewName", null);
        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsyncUpdatesLabelWhenValid()
    {
        Guid id = Guid.NewGuid();
        Label label = new()
        { Id = id, Name = "OldName", Color = null };
        _mockRepo.Setup(r => r.GetByIdAsync<Label>(id, It.IsAny<CancellationToken>())).ReturnsAsync(label);
        _mockRepo.Setup(r => r.ListAsNoTrackingAsync(It.IsAny<Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>(), It.IsAny<string[]>()))
                 .ReturnsAsync([]);

        Core.DTOs.Contact.LabelOperationResult result = await _service.UpdateAsync(id, "NewName", "#123");

        result.Success.Should().BeTrue();
        label.Name.Should().Be("NewName");
        label.Color.Should().Be("#123");
        _mockRepo.Verify(r => r.UpdateAsync(label, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AssignLabelAsyncAddsContactLabelWhenNotExists()
    {
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ContactLabel, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        await _service.AssignLabelAsync(Guid.NewGuid(), Guid.NewGuid());

        _mockRepo.Verify(r => r.AddAsync(It.IsAny<ContactLabel>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveLabelAsyncRemovesContactLabelWhenExists()
    {
        Guid id = Guid.NewGuid();
        ContactLabel contactLabel = new()
        { Id = id };
        _mockRepo.Setup(r => r.ListAsync(It.IsAny<Expression<Func<ContactLabel, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([contactLabel]);

        await _service.RemoveLabelAsync(Guid.NewGuid(), Guid.NewGuid());

        _mockRepo.Verify(r => r.DeleteAsync<ContactLabel>(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
