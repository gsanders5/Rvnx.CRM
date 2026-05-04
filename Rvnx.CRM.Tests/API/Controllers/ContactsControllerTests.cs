using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.API.Controllers;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.Interfaces;

namespace Rvnx.CRM.Tests.API.Controllers;

public class ContactsControllerTests
{
    private readonly Mock<IContactReadService> _readServiceMock;
    private readonly Mock<IContactManagementService> _managementServiceMock;
    private readonly ContactsController _sut;

    public ContactsControllerTests()
    {
        _readServiceMock = new Mock<IContactReadService>();
        _managementServiceMock = new Mock<IContactManagementService>();
        _sut = new ContactsController(
            _readServiceMock.Object,
            _managementServiceMock.Object,
            Mock.Of<IContactImportService>(),
            Mock.Of<IContactExportService>(),
            Mock.Of<ICsvExportService>());
    }

    [Fact]
    public async Task ListShouldReturnOkWithContacts()
    {
        List<ContactDto> contacts = [new ContactDto { Id = Guid.NewGuid() }];
        _readServiceMock.Setup(s => s.GetIndexDataAsync(false)).ReturnsAsync(contacts);

        IActionResult result = await _sut.List();

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equivalent(contacts, okResult.Value);
    }

    [Fact]
    public async Task GetShouldReturnOkWithContactWhenFound()
    {
        Guid id = Guid.NewGuid();
        ContactFormDto contact = new()
        { Id = id };
        _readServiceMock.Setup(s => s.GetContactFormAsync(id)).ReturnsAsync(contact);

        IActionResult result = await _sut.Get(id);

        OkObjectResult okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equivalent(contact, okResult.Value);
    }

    [Fact]
    public async Task GetShouldReturnNotFoundWhenContactDoesNotExist()
    {
        Guid id = Guid.NewGuid();
        _readServiceMock.Setup(s => s.GetContactFormAsync(id)).ReturnsAsync((ContactFormDto?)null);

        IActionResult result = await _sut.Get(id);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateShouldReturnCreatedAtActionWhenSuccessful()
    {
        ContactFormDto model = new()
        { FirstName = "Test" };
        Guid id = Guid.NewGuid();
        ContactOperationResult operationResult = ContactOperationResult.Ok(id);

        _managementServiceMock.Setup(s => s.CreateContactAsync(model)).ReturnsAsync(operationResult);

        IActionResult result = await _sut.Create(model);

        CreatedAtActionResult createdResult = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ContactsController.Get), createdResult.ActionName);
        Assert.NotNull(createdResult.RouteValues);
        Assert.True(createdResult.RouteValues.ContainsKey("id"));
        Assert.Equal(id, createdResult.RouteValues["id"]);
    }

    [Fact]
    public async Task CreateShouldReturnBadRequestWhenFailed()
    {
        ContactFormDto model = new()
        { FirstName = "Test" };
        ContactOperationResult operationResult = ContactOperationResult.Failure("Error");

        _managementServiceMock.Setup(s => s.CreateContactAsync(model)).ReturnsAsync(operationResult);

        IActionResult result = await _sut.Create(model);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateShouldReturnNoContentWhenSuccessful()
    {
        Guid id = Guid.NewGuid();
        ContactFormDto model = new()
        { Id = id, FirstName = "Test" };
        ContactOperationResult operationResult = ContactOperationResult.Ok(id);

        _managementServiceMock.Setup(s => s.UpdateContactAsync(id, model, null, null, null)).ReturnsAsync(operationResult);

        IActionResult result = await _sut.Update(id, model);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task UpdateShouldReturnNotFoundWhenContactIsNotFound()
    {
        Guid id = Guid.NewGuid();
        ContactFormDto model = new()
        { Id = id, FirstName = "Test" };
        ContactOperationResult operationResult = ContactOperationResult.NotFound();

        _managementServiceMock.Setup(s => s.UpdateContactAsync(id, model, null, null, null)).ReturnsAsync(operationResult);

        IActionResult result = await _sut.Update(id, model);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteShouldReturnNoContent()
    {
        Guid id = Guid.NewGuid();

        _managementServiceMock.Setup(s => s.DeleteContactAsync(id)).Returns(Task.CompletedTask);

        IActionResult result = await _sut.Delete(id);

        Assert.IsType<NoContentResult>(result);
    }
}
