using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;
using System.Reflection;

namespace Rvnx.CRM.Tests.Controllers;

public class ImmichControllerTests
{
    private static ImmichController CreateController(Mock<IImmichService> immichMock)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
        return new ImmichController(immichMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }

    private static TValue GetProperty<TValue>(object source, string name)
    {
        PropertyInfo? prop = source.GetType().GetProperty(name) ?? throw new InvalidOperationException($"Missing property {name}");
        return (TValue)prop.GetValue(source)!;
    }

    [Fact]
    public async Task GalleryReturnsEmptyPartialWhenImmichOff()
    {
        Mock<IImmichService> immichMock = new();
        immichMock.SetupGet(s => s.IsEnabled).Returns(false);

        ImmichController controller = CreateController(immichMock);

        IActionResult result = await controller.Gallery(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        PartialViewResult partial = Assert.IsType<PartialViewResult>(result);
        IReadOnlyList<ImmichAssetDto> assets = Assert.IsAssignableFrom<IReadOnlyList<ImmichAssetDto>>(partial.Model);
        Assert.Empty(assets);
        immichMock.Verify(s => s.GetAssetsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GalleryForwardsIdsToService()
    {
        Guid personId = Guid.NewGuid();
        Guid tagId = Guid.NewGuid();

        Mock<IImmichService> immichMock = new();
        immichMock.SetupGet(s => s.IsEnabled).Returns(true);

        ImmichAssetDto[] assets = [new ImmichAssetDto(Guid.NewGuid(), "a.jpg", "IMAGE")];
        immichMock.Setup(s => s.GetAssetsAsync(personId, tagId, 24, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assets);

        ImmichController controller = CreateController(immichMock);

        IActionResult result = await controller.Gallery(personId, tagId, CancellationToken.None);

        PartialViewResult partial = Assert.IsType<PartialViewResult>(result);
        IReadOnlyList<ImmichAssetDto> returned = Assert.IsAssignableFrom<IReadOnlyList<ImmichAssetDto>>(partial.Model);
        Assert.Single(returned);
    }

    [Fact]
    public async Task ThumbnailReturnsNotFoundWhenPayloadNull()
    {
        Mock<IImmichService> immichMock = new();
        immichMock.Setup(s => s.GetThumbnailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmichMediaPayload?)null);

        ImmichController controller = CreateController(immichMock);

        IActionResult result = await controller.Thumbnail(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task ThumbnailReturnsFileWithPrivateCacheHeader()
    {
        Mock<IImmichService> immichMock = new();
        using HttpResponseMessage response = new() { Content = new StreamContent(new MemoryStream(new byte[] { 1, 2, 3 })) };
        MemoryStream stream = new(new byte[] { 1, 2, 3 });
        immichMock.Setup(s => s.GetThumbnailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImmichMediaPayload(response, stream, "image/webp"));

        ImmichController controller = CreateController(immichMock);

        IActionResult result = await controller.Thumbnail(Guid.NewGuid(), CancellationToken.None);

        FileStreamResult file = Assert.IsType<FileStreamResult>(result);
        Assert.Equal("image/webp", file.ContentType);
        Assert.Equal("private, max-age=3600", controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task PeopleDelegatesToService()
    {
        Mock<IImmichService> immichMock = new();
        ImmichOptionDto[] results = [new ImmichOptionDto(Guid.NewGuid(), "Bob")];
        immichMock.Setup(s => s.SearchPeopleAsync("bob", It.IsAny<CancellationToken>())).ReturnsAsync(results);

        ImmichController controller = CreateController(immichMock);

        IActionResult result = await controller.People("bob", CancellationToken.None);

        JsonResult json = Assert.IsType<JsonResult>(result);
        IReadOnlyList<ImmichOptionDto> returned = GetProperty<IReadOnlyList<ImmichOptionDto>>(json.Value!, "results");
        Assert.Single(returned);
    }
}
