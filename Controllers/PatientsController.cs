using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HospitalClinicManagementSystem.Controllers;

public class PatientsController : Controller
{
    private readonly PatientRepository _patients;
    public PatientsController(PatientRepository patients) => _patients=patients;

    public async Task<IActionResult> Index(string? search,string? sortBy="registered",string? sortOrder="desc",int page=1,int pageSize=10)
    {
        ViewBag.Search=search;ViewBag.SortBy=sortBy;ViewBag.SortOrder=sortOrder;ViewBag.PageSize=pageSize;
        return View(await _patients.GetPagedAsync(search,sortBy,sortOrder,page,pageSize));
    }

    public async Task<IActionResult> Details(int id)
    {var item=await _patients.GetByIdAsync(id);return item is null?NotFound():View(item);}

    public IActionResult Create()=>View(new Patient{DateOfBirth=DateTime.Today.AddYears(-20)});

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Patient patient)
    {
        if(patient.DateOfBirth>DateTime.Today)ModelState.AddModelError(nameof(patient.DateOfBirth),"Date of birth cannot be in the future.");
        if(!ModelState.IsValid)return View(patient);
        var id=await _patients.CreateAsync(patient);TempData["Success"]="Patient registered successfully.";return RedirectToAction(nameof(Details),new{id});
    }

    public async Task<IActionResult> Edit(int id)
    {var item=await _patients.GetByIdAsync(id);return item is null?NotFound():View(item);}

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,Patient patient)
    {
        if(id!=patient.PatientID)return BadRequest();if(patient.DateOfBirth>DateTime.Today)ModelState.AddModelError(nameof(patient.DateOfBirth),"Date of birth cannot be in the future.");
        if(!ModelState.IsValid)return View(patient);await _patients.UpdateAsync(patient);TempData["Success"]="Patient updated successfully.";return RedirectToAction(nameof(Details),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var ok=await _patients.DeleteAsync(id);
        TempData[ok?"Success":"Error"]=ok?"Patient removed successfully.":"This patient has related clinical records and cannot be deleted. Keep the record for medical integrity.";
        return RedirectToAction(nameof(Index));
    }
}
