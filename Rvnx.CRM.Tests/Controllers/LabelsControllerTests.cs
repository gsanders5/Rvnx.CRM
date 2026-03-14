using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class LabelsControllerTests : IDisposable
{
    private readonly Mock<ILabelService> _mockService;
    private readonly LabelsController _controller;

    public LabelsControllerTests()
    {
        _mockService = new Mock<ILabelService>();
        _controller = new LabelsController(_mockService.Object);
    }

    public void Dispose()
    {
        _controller.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task IndexReturnsViewWithLabels()
    {
        List<LabelDto> labels = [new LabelDto { Id = Guid.NewGuid(), Name = "Test" }];
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(labels);

        IActionResult result = await _controller.Index();

        ViewResult viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(labels, viewResult.Model);
    }

    [Fact]
    public async Task CreatePostWithValidDataRedirectsToIndex()
    {
        LabelFormDto dto = new()
        { Name = "Test", Color = "#123" };
        _mockService.Setup(s => s.CreateAsync("Test", "#123")).ReturnsAsync(LabelOperationResult.Ok(Guid.NewGuid()));

        IActionResult result = await _controller.Create(dto);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task EditPostWithValidDataRedirectsToIndex()
    {
        Guid id = Guid.NewGuid();
        LabelFormDto dto = new()
        { Id = id, Name = "Test", Color = "#123" };
        _mockService.Setup(s => s.UpdateAsync(id, "Test", "#123")).ReturnsAsync(LabelOperationResult.Ok(id));

        IActionResult result = await _controller.Edit(id, dto);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task DeleteConfirmedDeletesAndRedirects()
    {
        Guid id = Guid.NewGuid();

        IActionResult result = await _controller.DeleteConfirmed(id);

        RedirectToActionResult redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockService.Verify(s => s.DeleteAsync(id), Times.Once);
    }
}