using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

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
        var labels = new List<LabelDto> { new LabelDto { Id = Guid.NewGuid(), Name = "Test" } };
        _mockService.Setup(s => s.GetAllAsync()).ReturnsAsync(labels);

        var result = await _controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal(labels, viewResult.Model);
    }

    [Fact]
    public async Task CreatePostWithValidDataRedirectsToIndex()
    {
        var dto = new LabelFormDto { Name = "Test", Color = "#123" };
        _mockService.Setup(s => s.CreateAsync("Test", "#123")).ReturnsAsync(LabelOperationResult.Ok(Guid.NewGuid()));

        var result = await _controller.Create(dto);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task EditPostWithValidDataRedirectsToIndex()
    {
        var id = Guid.NewGuid();
        var dto = new LabelFormDto { Id = id, Name = "Test", Color = "#123" };
        _mockService.Setup(s => s.UpdateAsync(id, "Test", "#123")).ReturnsAsync(LabelOperationResult.Ok(id));

        var result = await _controller.Edit(id, dto);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
    }

    [Fact]
    public async Task DeleteConfirmedDeletesAndRedirects()
    {
        var id = Guid.NewGuid();

        var result = await _controller.DeleteConfirmed(id);

        var redirectResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectResult.ActionName);
        _mockService.Verify(s => s.DeleteAsync(id), Times.Once);
    }
}
