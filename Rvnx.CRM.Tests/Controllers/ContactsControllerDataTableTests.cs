using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Rvnx.CRM.Core.DTOs.Common;
using Rvnx.CRM.Core.DTOs.Contact;
using Rvnx.CRM.Core.DTOs.DataTable;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers;

namespace Rvnx.CRM.Tests.Controllers;

public class ContactsControllerDataTableTests : IDisposable
{
    private readonly Mock<ILogger<ContactsController>> _loggerMock = new();
    private readonly Mock<ICurrentUserService> _userMock = new();
    private readonly Mock<IContactImportService> _contactImportServiceMock = new();
    private readonly Mock<IContactExportService> _contactExportServiceMock = new();
    private readonly Mock<IContactManagementService> _contactManagementServiceMock = new();
    private readonly Mock<IContactReadService> _contactReadServiceMock = new();
    private readonly Mock<ISelfContactService> _selfContactServiceMock = new();
    private readonly Mock<IUrlHelper> _urlHelperMock = new();
    private readonly ContactsController _controller;

    public ContactsControllerDataTableTests()
    {
        _userMock.Setup(s => s.UserId).Returns(Guid.NewGuid());
        _userMock.Setup(s => s.IsAuthenticated).Returns(true);

        _controller = new ContactsController(
            _loggerMock.Object,
            _userMock.Object,
            _contactImportServiceMock.Object,
            _contactExportServiceMock.Object,
            _contactManagementServiceMock.Object,
            _contactReadServiceMock.Object,
            _selfContactServiceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            Url = _urlHelperMock.Object
        };
        _controller.TempData = new TempDataDictionary(_controller.HttpContext, Mock.Of<ITempDataProvider>());

        _urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>())).Returns("http://localhost/mock-url");
    }

    [Fact]
    public async Task DataTableReturnsJsonWithCorrectData()
    {
        // Arrange
        var query = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
        {
            { "draw", "1" },
            { "start", "0" },
            { "length", "10" },
            { "search[value]", "" },
            { "order[0][column]", "1" },
            { "order[0][dir]", "asc" }
        };
        _controller.ControllerContext.HttpContext.Request.Query = new QueryCollection(query);

        List<ContactDto> items = [
            new ContactDto { Id = Guid.NewGuid(), FirstName = "John", LastName = "Doe", FullName = "John Doe" }
        ];
        PagedResult<ContactDto> pagedResult = new() { Items = items, TotalCount = 100, FilteredCount = 10 };

        _contactReadServiceMock.Setup(s => s.GetContactDataTableAsync(It.IsAny<DataTableRequestDto>(), false))
            .ReturnsAsync(pagedResult);

        // Act
        IActionResult result = await _controller.DataTable();

        // Assert
        JsonResult jsonResult = Assert.IsType<JsonResult>(result);
        DataTableResponseDto<ContactDataTableDto> value = Assert.IsType<DataTableResponseDto<ContactDataTableDto>>(jsonResult.Value);
        Assert.Equal(1, value.Draw);
        Assert.Equal(100, value.RecordsTotal);
        Assert.Equal(10, value.RecordsFiltered);
        Assert.Single(value.Data);
        Assert.Equal("John", value.Data.First().FirstName);
        Assert.Contains("John Doe", value.Data.First().NameHtml); // Contains name because it's in the link text
        Assert.Contains("btn-group", value.Data.First().ActionsHtml); // Contains actions HTML
    }

    public void Dispose()
    {
        _controller.Dispose();
        GC.SuppressFinalize(this);
    }
}
