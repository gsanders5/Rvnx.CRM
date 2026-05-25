using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.Core.DTOs.Base;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;
using Rvnx.CRM.Web.ViewModels.Immich;
using System.Reflection;
using System.Text;

namespace Rvnx.CRM.Tests.Controllers;

public class ImmichControllerTests
{
    private static ImmichController CreateController(
        Mock<IImmichService> immichMock,
        Mock<IAttachmentService>? attachmentMock = null,
        Mock<IContactManagementService>? mgmtMock = null,
        Mock<IFileValidationService>? validationMock = null)
    {
        DefaultHttpContext httpContext = new();
        httpContext.Features.Set<IHttpResponseFeature>(new HttpResponseFeature());

        Mock<IUrlHelper> urlHelperMock = new();
        urlHelperMock.Setup(u => u.IsLocalUrl(It.IsAny<string>())).Returns(true);

        Mock<IFileValidationService> fileValidation = validationMock ?? DefaultValidationMock();

        ImmichController controller = new(
            immichMock.Object,
            (attachmentMock ?? new Mock<IAttachmentService>()).Object,
            (mgmtMock ?? new Mock<IContactManagementService>()).Object,
            fileValidation.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            Url = urlHelperMock.Object,
        };
        return controller;
    }

    private static Mock<IFileValidationService> DefaultValidationMock()
    {
        Mock<IFileValidationService> mock = new();
        mock.Setup(v => v.IsAllowedFileSize(It.IsAny<long>())).Returns(true);
        return mock;
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

        ImmichGalleryRequest request = new() { ContactId = Guid.NewGuid(), PersonId = Guid.NewGuid(), TagId = Guid.NewGuid() };
        IActionResult result = await controller.Gallery(request, CancellationToken.None);

        PartialViewResult partial = Assert.IsType<PartialViewResult>(result);
        ImmichGalleryViewModel vm = Assert.IsType<ImmichGalleryViewModel>(partial.Model);
        Assert.Empty(vm.Assets);
        immichMock.Verify(s => s.GetAssetsAsync(It.IsAny<Guid?>(), It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GalleryPopulatesViewModelWithContext()
    {
        Guid contactId = Guid.NewGuid();
        Guid personId = Guid.NewGuid();
        Guid tagId = Guid.NewGuid();

        Mock<IImmichService> immichMock = new();
        immichMock.SetupGet(s => s.IsEnabled).Returns(true);
        immichMock.SetupGet(s => s.WebBaseUrl).Returns("https://photos.example.com");

        ImmichAssetDto[] assets = [new ImmichAssetDto(Guid.NewGuid(), "a.jpg", "IMAGE")];
        immichMock.Setup(s => s.GetAssetsAsync(personId, tagId, 24, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assets);

        ImmichController controller = CreateController(immichMock);

        ImmichGalleryRequest request = new() { ContactId = contactId, PersonId = personId, PersonName = "Bob", TagId = tagId, TagValue = "BobTag" };
        IActionResult result = await controller.Gallery(request, CancellationToken.None);

        PartialViewResult partial = Assert.IsType<PartialViewResult>(result);
        ImmichGalleryViewModel vm = Assert.IsType<ImmichGalleryViewModel>(partial.Model);
        Assert.Equal(contactId, vm.ContactId);
        Assert.Equal(personId, vm.PersonId);
        Assert.Equal("Bob", vm.PersonName);
        Assert.Equal(tagId, vm.TagId);
        Assert.Equal("BobTag", vm.TagValue);
        Assert.Equal("https://photos.example.com", vm.WebBaseUrl);
        Assert.Single(vm.Assets);
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
    public async Task SetAsProfilePhotoReturnsNotFoundWhenImmichDown()
    {
        Mock<IImmichService> immichMock = new();
        immichMock.Setup(s => s.GetOriginalAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ImmichMediaPayload?)null);

        ImmichController controller = CreateController(immichMock);

        IActionResult result = await controller.SetAsProfilePhoto(Guid.NewGuid(), Guid.NewGuid(), "a.jpg", null, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SetAsProfilePhotoUploadsAndSetsProfile()
    {
        Guid contactId = Guid.NewGuid();
        Guid assetId = Guid.NewGuid();
        Guid newAttachmentId = Guid.NewGuid();
        byte[] imageBytes = Encoding.UTF8.GetBytes("fake-jpeg");

        Mock<IImmichService> immichMock = new();
        HttpResponseMessage response = new() { Content = new ByteArrayContent(imageBytes) };
        MemoryStream content = new(imageBytes);
        immichMock.Setup(s => s.GetOriginalAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImmichMediaPayload(response, content, "image/jpeg"));

        Mock<IAttachmentService> attachmentMock = new();
        attachmentMock.Setup(s => s.UploadAttachmentAsync(contactId, It.IsAny<byte[]>(), "photo.jpg"))
            .ReturnsAsync(AttachmentOperationResult.Ok(newAttachmentId));

        Mock<IContactManagementService> mgmtMock = new();
        mgmtMock.Setup(s => s.SetAttachmentAsProfilePhotoAsync(contactId, newAttachmentId))
            .ReturnsAsync(ContactOperationResult.Ok(contactId));

        ImmichController controller = CreateController(immichMock, attachmentMock, mgmtMock);

        IActionResult result = await controller.SetAsProfilePhoto(contactId, assetId, "photo.jpg", "/Contacts/Details/" + contactId, CancellationToken.None);

        attachmentMock.Verify(s => s.UploadAttachmentAsync(contactId, It.IsAny<byte[]>(), "photo.jpg"), Times.Once);
        mgmtMock.Verify(s => s.SetAttachmentAsProfilePhotoAsync(contactId, newAttachmentId), Times.Once);
        Assert.IsType<LocalRedirectResult>(result);
    }

    [Fact]
    public async Task SetAsProfilePhotoSynthesizesFilenameWhenMissing()
    {
        Guid contactId = Guid.NewGuid();
        Guid assetId = Guid.NewGuid();

        Mock<IImmichService> immichMock = new();
        HttpResponseMessage response = new() { Content = new ByteArrayContent([1, 2, 3]) };
        immichMock.Setup(s => s.GetOriginalAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImmichMediaPayload(response, new MemoryStream([1, 2, 3]), "image/png"));

        string? capturedFileName = null;
        Mock<IAttachmentService> attachmentMock = new();
        attachmentMock.Setup(s => s.UploadAttachmentAsync(contactId, It.IsAny<byte[]>(), It.IsAny<string>()))
            .Callback<Guid, byte[], string>((_, _, fn) => capturedFileName = fn)
            .ReturnsAsync(AttachmentOperationResult.Ok(Guid.NewGuid()));

        Mock<IContactManagementService> mgmtMock = new();
        mgmtMock.Setup(s => s.SetAttachmentAsProfilePhotoAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
            .ReturnsAsync(ContactOperationResult.Ok(contactId));

        ImmichController controller = CreateController(immichMock, attachmentMock, mgmtMock);

        await controller.SetAsProfilePhoto(contactId, assetId, null, "/x", CancellationToken.None);

        Assert.Equal($"immich-{assetId}.png", capturedFileName);
    }

    [Fact]
    public async Task SetAsProfilePhotoRejectsEarlyWhenContentLengthExceedsLimit()
    {
        Guid contactId = Guid.NewGuid();
        Guid assetId = Guid.NewGuid();

        Mock<IImmichService> immichMock = new();
        HttpResponseMessage response = new() { Content = new ByteArrayContent([1, 2, 3]) };
        response.Content.Headers.ContentLength = 999_999_999;
        immichMock.Setup(s => s.GetOriginalAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImmichMediaPayload(response, new MemoryStream([1, 2, 3]), "image/jpeg"));

        Mock<IFileValidationService> validation = new();
        validation.Setup(v => v.IsAllowedFileSize(999_999_999L)).Returns(false);

        Mock<IAttachmentService> attachmentMock = new();
        ImmichController controller = CreateController(immichMock, attachmentMock, validationMock: validation);

        IActionResult result = await controller.SetAsProfilePhoto(contactId, assetId, "x.jpg", null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        attachmentMock.Verify(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SetAsProfilePhotoReturnsBadRequestWhenUploadFails()
    {
        Guid contactId = Guid.NewGuid();
        Guid assetId = Guid.NewGuid();

        Mock<IImmichService> immichMock = new();
        HttpResponseMessage response = new() { Content = new ByteArrayContent([1]) };
        immichMock.Setup(s => s.GetOriginalAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ImmichMediaPayload(response, new MemoryStream([1]), "image/jpeg"));

        Mock<IAttachmentService> attachmentMock = new();
        attachmentMock.Setup(s => s.UploadAttachmentAsync(It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<string>()))
            .ReturnsAsync(AttachmentOperationResult.Failure("Invalid file signature."));

        ImmichController controller = CreateController(immichMock, attachmentMock);

        IActionResult result = await controller.SetAsProfilePhoto(contactId, assetId, "x.jpg", null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
