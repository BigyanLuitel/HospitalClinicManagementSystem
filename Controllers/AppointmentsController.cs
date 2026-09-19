using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.Repositories;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.Controllers;

public class AppointmentsController : Controller
{
    private readonly AppointmentRepository _appointments;
    private readonly PatientRepository _patients;
    private readonly DoctorRepository _doctors;
    public AppointmentsController(AppointmentRepository appointments,PatientRepository patients,DoctorRepository doctors){_appointments=appointments;_patients=patients;_doctors=doctors;}

    public async Task<IActionResult> Index(string? search,int? doctorId,string? status,DateTime? fromDate,DateTime? toDate,string? sortBy="date",string? sortOrder="desc",int page=1,int pageSize=10)
    {
        ViewBag.Search=search;ViewBag.DoctorId=doctorId;ViewBag.Status=status;ViewBag.FromDate=fromDate?.ToString("yyyy-MM-dd");ViewBag.ToDate=toDate?.ToString("yyyy-MM-dd");ViewBag.SortBy=sortBy;ViewBag.SortOrder=sortOrder;ViewBag.PageSize=pageSize;
        ViewBag.Doctors=await _doctors.GetActiveAsync();
        return View(await _appointments.GetPagedAsync(search,doctorId,status,fromDate,toDate,sortBy,sortOrder,page,pageSize));
    }

    public async Task<IActionResult> Details(int id){var a=await _appointments.GetDetailsAsync(id);return a is null?NotFound():View(a);}

    public async Task<IActionResult> Create(int? patientId)
    {
        var vm=new AppointmentFormViewModel{PatientID=patientId??0,AppointmentDate=DateTime.Today,AppointmentTime=new TimeSpan(9,0,0),Status="Scheduled"};
        await PopulateAsync(vm);return View(vm);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AppointmentFormViewModel vm)
    {
        ValidateAppointment(vm);
        if(ModelState.IsValid && await _appointments.HasConflictAsync(vm.DoctorID,vm.AppointmentDate,vm.AppointmentTime))
            ModelState.AddModelError(nameof(vm.AppointmentTime),"This doctor already has an appointment at the selected date and time.");
        if(!ModelState.IsValid){await PopulateAsync(vm);return View(vm);}
        var id=await _appointments.CreateAsync(ToEntity(vm));TempData["Success"]="Appointment booked successfully.";return RedirectToAction(nameof(Details),new{id});
    }

    public async Task<IActionResult> Edit(int id)
    {
        var a=await _appointments.GetByIdAsync(id);if(a is null)return NotFound();
        var vm=new AppointmentFormViewModel{AppointmentID=a.AppointmentID,PatientID=a.PatientID,DoctorID=a.DoctorID,AppointmentDate=a.AppointmentDate,AppointmentTime=a.AppointmentTime,Reason=a.Reason,Status=a.Status};await PopulateAsync(vm);return View(vm);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id,AppointmentFormViewModel vm)
    {
        if(id!=vm.AppointmentID)return BadRequest();ValidateAppointment(vm);
        if(ModelState.IsValid && await _appointments.HasConflictAsync(vm.DoctorID,vm.AppointmentDate,vm.AppointmentTime,id))ModelState.AddModelError(nameof(vm.AppointmentTime),"This doctor already has an appointment at the selected date and time.");
        if(!ModelState.IsValid){await PopulateAsync(vm);return View(vm);}var entity=ToEntity(vm);entity.AppointmentID=id;await _appointments.UpdateAsync(entity);TempData["Success"]="Appointment updated.";return RedirectToAction(nameof(Details),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id){var ok=await _appointments.CancelAsync(id);TempData[ok?"Success":"Error"]=ok?"Appointment cancelled.":"Completed appointments cannot be cancelled.";return RedirectToAction(nameof(Details),new{id});}

    private void ValidateAppointment(AppointmentFormViewModel vm)
    {
        if(vm.PatientID<=0)ModelState.AddModelError(nameof(vm.PatientID),"Select a patient.");
        if(vm.DoctorID<=0)ModelState.AddModelError(nameof(vm.DoctorID),"Select a doctor.");
        if(vm.AppointmentDate.Date<DateTime.Today && vm.AppointmentID==0)ModelState.AddModelError(nameof(vm.AppointmentDate),"New appointments cannot be booked in the past.");
        var allowed=new[]{"Scheduled","Completed","Cancelled"};if(!allowed.Contains(vm.Status))ModelState.AddModelError(nameof(vm.Status),"Invalid appointment status.");
    }
    private async Task PopulateAsync(AppointmentFormViewModel vm)
    {
        vm.Patients=(await _patients.GetAllAsync()).Select(p=>new SelectListItem(p.FullName+" (#"+p.PatientID+")",p.PatientID.ToString()));
        vm.Doctors=(await _doctors.GetActiveAsync()).Select(d=>new SelectListItem(d.FullName+" — "+d.Specialization,d.DoctorID.ToString()));
    }
    private static Appointment ToEntity(AppointmentFormViewModel vm)=>new(){AppointmentID=vm.AppointmentID,PatientID=vm.PatientID,DoctorID=vm.DoctorID,AppointmentDate=vm.AppointmentDate,AppointmentTime=vm.AppointmentTime,Reason=vm.Reason,Status=vm.Status};
}
