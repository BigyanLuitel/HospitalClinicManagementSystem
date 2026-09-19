using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace HospitalClinicManagementSystem.Controllers;

public class DoctorsController : Controller
{
    private readonly DoctorRepository _doctors;
    public DoctorsController(DoctorRepository doctors)=>_doctors=doctors;

    public async Task<IActionResult> Index(string? search,string? specialization,string? sortBy="name",string? sortOrder="asc",int page=1,int pageSize=10)
    {
        ViewBag.Search=search;ViewBag.Specialization=specialization;ViewBag.SortBy=sortBy;ViewBag.SortOrder=sortOrder;ViewBag.PageSize=pageSize;ViewBag.Specializations=await _doctors.GetSpecializationsAsync();
        return View(await _doctors.GetPagedAsync(search,specialization,sortBy,sortOrder,page,pageSize));
    }
    public async Task<IActionResult> Details(int id){var d=await _doctors.GetByIdAsync(id);return d is null?NotFound():View(d);}
    public IActionResult Create()=>View(new Doctor{Status="Active"});
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Doctor doctor){if(!ModelState.IsValid)return View(doctor);var id=await _doctors.CreateAsync(doctor);TempData["Success"]="Doctor profile created.";return RedirectToAction(nameof(Details),new{id});}
    public async Task<IActionResult> Edit(int id){var d=await _doctors.GetByIdAsync(id);return d is null?NotFound():View(d);}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,Doctor doctor){if(id!=doctor.DoctorID)return BadRequest();if(!ModelState.IsValid)return View(doctor);await _doctors.UpdateAsync(doctor);TempData["Success"]="Doctor profile updated.";return RedirectToAction(nameof(Details),new{id});}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id){await _doctors.DeactivateAsync(id);TempData["Success"]="Doctor marked inactive.";return RedirectToAction(nameof(Index));}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id){var ok=await _doctors.DeleteAsync(id);TempData[ok?"Success":"Error"]=ok?"Doctor removed.":"This doctor has related records and cannot be deleted. Mark the profile inactive instead.";return RedirectToAction(nameof(Index));}
}
