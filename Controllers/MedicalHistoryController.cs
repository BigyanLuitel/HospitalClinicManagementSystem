using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.Repositories;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.Controllers;

public class MedicalHistoryController : Controller
{
    private readonly MedicalHistoryRepository _history;private readonly PatientRepository _patients;private readonly DoctorRepository _doctors;private readonly AppointmentRepository _appointments;
    public MedicalHistoryController(MedicalHistoryRepository history,PatientRepository patients,DoctorRepository doctors,AppointmentRepository appointments){_history=history;_patients=patients;_doctors=doctors;_appointments=appointments;}

    public async Task<IActionResult> Index(int? patientId,string? search,string? sortBy="date",string? sortOrder="desc",int page=1,int pageSize=10){ViewBag.PatientId=patientId;ViewBag.Search=search;ViewBag.SortBy=sortBy;ViewBag.SortOrder=sortOrder;ViewBag.PageSize=pageSize;return View(await _history.GetPagedAsync(patientId,search,sortBy,sortOrder,page,pageSize));}

    public async Task<IActionResult> Create(int? patientId,int? appointmentId)
    {
        var vm=new MedicalHistoryFormViewModel{PatientID=patientId??0,AppointmentID=appointmentId,VisitDate=DateTime.Today};
        if(appointmentId.HasValue){var a=await _appointments.GetDetailsAsync(appointmentId.Value);if(a is not null){vm.PatientID=a.PatientID;vm.DoctorID=a.DoctorID;vm.VisitDate=a.AppointmentDate;}}
        await PopulateAsync(vm);return View(vm);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MedicalHistoryFormViewModel vm)
    {
        if(vm.PatientID<=0)ModelState.AddModelError(nameof(vm.PatientID),"Select a patient.");if(vm.DoctorID<=0)ModelState.AddModelError(nameof(vm.DoctorID),"Select a doctor.");
        if(vm.AppointmentID.HasValue)
        {
            var a=await _appointments.GetDetailsAsync(vm.AppointmentID.Value);
            if(a is null || a.PatientID!=vm.PatientID || a.DoctorID!=vm.DoctorID)ModelState.AddModelError(nameof(vm.AppointmentID),"The appointment must belong to the selected patient and doctor.");
            else if(await _history.AppointmentHasHistoryAsync(vm.AppointmentID.Value))ModelState.AddModelError(nameof(vm.AppointmentID),"A medical-history entry already exists for this appointment.");
        }
        if(!ModelState.IsValid){await PopulateAsync(vm);return View(vm);}
        await _history.CreateAsync(new MedicalHistory{PatientID=vm.PatientID,DoctorID=vm.DoctorID,AppointmentID=vm.AppointmentID,VisitDate=vm.VisitDate,Diagnosis=vm.Diagnosis,Notes=vm.Notes});TempData["Success"]="Medical history entry saved.";return RedirectToAction(nameof(Index),new{patientId=vm.PatientID});
    }

    private async Task PopulateAsync(MedicalHistoryFormViewModel vm)
    {
        vm.Patients=(await _patients.GetAllAsync()).Select(p=>new SelectListItem(p.FullName+" (#"+p.PatientID+")",p.PatientID.ToString()));
        vm.Doctors=(await _doctors.GetActiveAsync()).Select(d=>new SelectListItem(d.FullName,d.DoctorID.ToString()));
        vm.Appointments=(await _appointments.GetLookupAsync(false)).Select(a=>new SelectListItem($"#{a.AppointmentID} — {a.AppointmentDate:dd MMM yyyy} — {a.PatientName} / {a.DoctorName}",a.AppointmentID.ToString()));
    }
}
