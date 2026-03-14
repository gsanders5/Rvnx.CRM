using FluentAssertions;
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
        _sut = new ContactsController(_readServiceMock.Object, _managementServiceMock.Object);
    }

    [Fact]
    public async Task ListShouldReturnOkWithContacts()
    {
        var contacts = new List<ContactDto> { new ContactDto { Id = Guid.NewGuid() } };
        _readServiceMock.Setup(s => s.GetIndexDataAsync(false)).ReturnsAsync(contacts);

        var result = await _sut.List();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(contacts);
    }

    [Fact]
    public async Task GetShouldReturnOkWithContactWhenFound()
    {
        var id = Guid.NewGuid();
        var contact = new ContactFormDto { Id = id };
        _readServiceMock.Setup(s => s.GetContactFormAsync(id)).ReturnsAsync(contact);

        var result = await _sut.Get(id);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(contact);
    }

    [Fact]
    public async Task GetShouldReturnNotFoundWhenContactDoesNotExist()
    {
        var id = Guid.NewGuid();
        _readServiceMock.Setup(s => s.GetContactFormAsync(id)).ReturnsAsync((ContactFormDto?)null);

        var result = await _sut.Get(id);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateShouldReturnCreatedAtActionWhenSuccessful()
    {
        var model = new ContactFormDto { FirstName = "Test" };
        var id = Guid.NewGuid();
        var operationResult = ContactOperationResult.Ok(id);

        _managementServiceMock.Setup(s => s.CreateContactAsync(model)).ReturnsAsync(operationResult);

        var result = await _sut.Create(model);

        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(ContactsController.Get));
        createdResult.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(id);
    }

    [Fact]
    public async Task CreateShouldReturnBadRequestWhenFailed()
    {
        var model = new ContactFormDto { FirstName = "Test" };
        var operationResult = ContactOperationResult.Failure("Error");

        _managementServiceMock.Setup(s => s.CreateContactAsync(model)).ReturnsAsync(operationResult);

        var result = await _sut.Create(model);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task UpdateShouldReturnNoContentWhenSuccessful()
    {
        var id = Guid.NewGuid();
        var model = new ContactFormDto { Id = id, FirstName = "Test" };
        var operationResult = ContactOperationResult.Ok(id);

        _managementServiceMock.Setup(s => s.UpdateContactAsync(id, model, null, null, null)).ReturnsAsync(operationResult);

        var result = await _sut.Update(id, model);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task UpdateShouldReturnNotFoundWhenContactIsNotFound()
    {
        var id = Guid.NewGuid();
        var model = new ContactFormDto { Id = id, FirstName = "Test" };
        var operationResult = ContactOperationResult.NotFound();

        _managementServiceMock.Setup(s => s.UpdateContactAsync(id, model, null, null, null)).ReturnsAsync(operationResult);

        var result = await _sut.Update(id, model);

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task DeleteShouldReturnNoContent()
    {
        var id = Guid.NewGuid();

        _managementServiceMock.Setup(s => s.DeleteContactAsync(id)).Returns(Task.CompletedTask);

        var result = await _sut.Delete(id);

        result.Should().BeOfType<NoContentResult>();
    }
}
