using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.Repositories;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.Controllers;

public class PrescriptionsController : Controller
{
    private readonly PrescriptionRepository _prescriptions;private readonly PatientRepository _patients;private readonly DoctorRepository _doctors;private readonly AppointmentRepository _appointments;
    public PrescriptionsController(PrescriptionRepository prescriptions,PatientRepository patients,DoctorRepository doctors,AppointmentRepository appointments){_prescriptions=prescriptions;_patients=patients;_doctors=doctors;_appointments=appointments;}
    public async Task<IActionResult> Index(int? patientId,string? search,string? sortBy="date",string? sortOrder="desc",int page=1,int pageSize=10){ViewBag.PatientId=patientId;ViewBag.Search=search;ViewBag.SortBy=sortBy;ViewBag.SortOrder=sortOrder;ViewBag.PageSize=pageSize;return View(await _prescriptions.GetPagedAsync(patientId,search,sortBy,sortOrder,page,pageSize));}
    public async Task<IActionResult> Details(int id){var p=await _prescriptions.GetByIdAsync(id);return p is null?NotFound():View(p);}
    public async Task<IActionResult> Create(int? appointmentId)
    {
        var vm=new PrescriptionFormViewModel();if(appointmentId.HasValue){var a=await _appointments.GetDetailsAsync(appointmentId.Value);if(a is not null){vm.AppointmentID=a.AppointmentID;vm.PatientID=a.PatientID;vm.DoctorID=a.DoctorID;}}
        await PopulateAsync(vm);return View(vm);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PrescriptionFormViewModel vm)
    {
        var a=vm.AppointmentID>0?await _appointments.GetDetailsAsync(vm.AppointmentID):null;
        if(a is null)ModelState.AddModelError(nameof(vm.AppointmentID),"Select a valid appointment.");else if(a.PatientID!=vm.PatientID || a.DoctorID!=vm.DoctorID)ModelState.AddModelError(nameof(vm.AppointmentID),"Appointment, patient and doctor must match.");
        if(!ModelState.IsValid){await PopulateAsync(vm);return View(vm);}var id=await _prescriptions.CreateAsync(new Prescription{AppointmentID=vm.AppointmentID,PatientID=vm.PatientID,DoctorID=vm.DoctorID,Medicines=vm.Medicines,Dosage=vm.Dosage});TempData["Success"]="Prescription issued.";return RedirectToAction(nameof(Details),new{id});
    }
    private async Task PopulateAsync(PrescriptionFormViewModel vm)
    {
        vm.Patients=(await _patients.GetAllAsync()).Select(p=>new SelectListItem(p.FullName+" (#"+p.PatientID+")",p.PatientID.ToString()));vm.Doctors=(await _doctors.GetActiveAsync()).Select(d=>new SelectListItem(d.FullName,d.DoctorID.ToString()));
        vm.Appointments=(await _appointments.GetLookupAsync(false)).Where(a=>a.Status!="Cancelled").Select(a=>new SelectListItem($"#{a.AppointmentID} — {a.AppointmentDate:dd MMM yyyy} — {a.PatientName} / {a.DoctorName}",a.AppointmentID.ToString()));
    }
}
