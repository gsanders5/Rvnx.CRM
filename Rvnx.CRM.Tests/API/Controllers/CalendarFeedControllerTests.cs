using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Rvnx.CRM.API.Authentication;
using Rvnx.CRM.API.Controllers;
using Rvnx.CRM.Core.DTOs.Calendar;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Core.Models;
using System.Text;

namespace Rvnx.CRM.Tests.API.Controllers;

public class CalendarFeedControllerTests
{
    private readonly Mock<ISignificantDateService> _significantDateServiceMock;
    private readonly Mock<IContactTaskService> _contactTaskServiceMock;
    private readonly Mock<ICalendarFeedService> _calendarFeedServiceMock;
    private readonly Mock<IApiTokenService> _apiTokenServiceMock;
    private readonly CalendarFeedController _sut;

    public CalendarFeedControllerTests()
    {
        _significantDateServiceMock = new Mock<ISignificantDateService>();
        _contactTaskServiceMock = new Mock<IContactTaskService>();
        _calendarFeedServiceMock = new Mock<ICalendarFeedService>();
        _apiTokenServiceMock = new Mock<IApiTokenService>();

        _sut = new CalendarFeedController(
            _significantDateServiceMock.Object,
            _contactTaskServiceMock.Object,
            _calendarFeedServiceMock.Object,
            _apiTokenServiceMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task FeedShouldReturnUnauthorizedWhenTokenIsInvalid()
    {
        _apiTokenServiceMock.Setup(s => s.ResolveTokenAsync("crm_bogus")).ReturnsAsync((ApiToken?)null);

        IActionResult result = await _sut.Feed("crm_bogus");

        Assert.IsType<UnauthorizedResult>(result);
        _significantDateServiceMock.Verify(s => s.GetCalendarEventsAsync(), Times.Never);
        _contactTaskServiceMock.Verify(s => s.GetCalendarEventsAsync(), Times.Never);
    }

    [Fact]
    public async Task FeedShouldStoreResolvedTokenInHttpContextItems()
    {
        ApiToken token = new()
        { UserId = Guid.NewGuid() };
        SetupValidFeed(token, dateEvents: [], taskEvents: [], ics: string.Empty);

        await _sut.Feed("crm_valid");

        Assert.Same(token, _sut.HttpContext.Items[ApiTokenAuthenticationOptions.ResolvedTokenItemKey]);
    }

    [Fact]
    public async Task FeedShouldReturnIcsFileWithCorrectContentTypeAndFileName()
    {
        string ics = "BEGIN:VCALENDAR\r\nEND:VCALENDAR";
        SetupValidFeed(new ApiToken(), dateEvents: [], taskEvents: [], ics);

        IActionResult result = await _sut.Feed("crm_valid");

        FileContentResult file = Assert.IsType<FileContentResult>(result);
        Assert.Equal("text/calendar; charset=utf-8", file.ContentType);
        Assert.Equal("rvnx-calendar.ics", file.FileDownloadName);
        Assert.Equal(Encoding.UTF8.GetBytes(ics), file.FileContents);
    }

    [Fact]
    public async Task FeedShouldCombineSignificantDatesAndTaskEvents()
    {
        List<CalendarEventDto> dateEvents = [new CalendarEventDto { Title = "Birthday" }];
        List<CalendarEventDto> taskEvents = [new CalendarEventDto { Title = "Follow up" }];
        SetupValidFeed(new ApiToken(), dateEvents, taskEvents, ics: string.Empty);

        await _sut.Feed("crm_valid");

        _calendarFeedServiceMock.Verify(s => s.BuildIcsFeed(dateEvents, taskEvents), Times.Once);
    }

    private void SetupValidFeed(ApiToken token, List<CalendarEventDto> dateEvents, List<CalendarEventDto> taskEvents, string ics)
    {
        _apiTokenServiceMock.Setup(s => s.ResolveTokenAsync("crm_valid")).ReturnsAsync(token);
        _significantDateServiceMock.Setup(s => s.GetCalendarEventsAsync()).ReturnsAsync(dateEvents);
        _contactTaskServiceMock.Setup(s => s.GetCalendarEventsAsync()).ReturnsAsync(taskEvents);
        _calendarFeedServiceMock
            .Setup(s => s.BuildIcsFeed(It.IsAny<IEnumerable<CalendarEventDto>>(), It.IsAny<IEnumerable<CalendarEventDto>>()))
            .Returns(ics);
    }
}
