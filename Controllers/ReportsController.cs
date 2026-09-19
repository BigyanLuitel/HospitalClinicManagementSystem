using HospitalClinicManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HospitalClinicManagementSystem.Controllers;

public class ReportsController : Controller
{
    private readonly DashboardRepository _dashboard;public ReportsController(DashboardRepository dashboard)=>_dashboard=dashboard;
    public async Task<IActionResult> Index(DateTime? fromDate,DateTime? toDate)
    {
        var from=(fromDate??new DateTime(DateTime.Today.Year,DateTime.Today.Month,1)).Date;var to=(toDate??DateTime.Today).Date;
        if(from>to){(from,to)=(to,from);TempData["Error"]="The date range was reversed automatically.";}
        return View(await _dashboard.GetReportAsync(from,to));
    }
}
