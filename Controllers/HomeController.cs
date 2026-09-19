using System.Diagnostics;
using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HospitalClinicManagementSystem.Controllers;

public class HomeController : Controller
{
    private readonly DashboardRepository _dashboard;
    public HomeController(DashboardRepository dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index() => View(await _dashboard.GetDashboardAsync());

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
