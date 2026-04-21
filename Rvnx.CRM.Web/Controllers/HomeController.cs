using Microsoft.AspNetCore.Mvc;
using Rvnx.CRM.Core.DTOs.Dashboard;
using Rvnx.CRM.Core.Interfaces;
using Rvnx.CRM.Web.Controllers.Base;
using Rvnx.CRM.Web.Models;
using System.Diagnostics;

namespace Rvnx.CRM.Web.Controllers;

public class HomeController(IDashboardService dashboardService) : AuthorizedController
{
    private readonly IDashboardService _dashboardService = dashboardService;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        DashboardDto dashboard = await _dashboardService.GetDashboardDataAsync();
        return View(dashboard);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
