using HospitalClinicManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HospitalClinicManagementSystem.Controllers;

public class SearchController : Controller
{
    private readonly PatientRepository _patients;public SearchController(PatientRepository patients)=>_patients=patients;
    public async Task<IActionResult> Index(string? q){ViewBag.Query=q;return View(string.IsNullOrWhiteSpace(q)?new List<HospitalClinicManagementSystem.ViewModels.PatientSearchResult>():await _patients.SearchAsync(q));}
}
