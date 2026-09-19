using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.Repositories;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.Controllers;

public class BillingController : Controller
{
    private readonly BillRepository _bills;private readonly PatientRepository _patients;private readonly AppointmentRepository _appointments;
    public BillingController(BillRepository bills,PatientRepository patients,AppointmentRepository appointments){_bills=bills;_patients=patients;_appointments=appointments;}

    public async Task<IActionResult> Index(string? search,string? paymentStatus,DateTime? fromDate,DateTime? toDate,string? sortBy="date",string? sortOrder="desc",int page=1,int pageSize=10)
    {ViewBag.Search=search;ViewBag.PaymentStatus=paymentStatus;ViewBag.FromDate=fromDate?.ToString("yyyy-MM-dd");ViewBag.ToDate=toDate?.ToString("yyyy-MM-dd");ViewBag.SortBy=sortBy;ViewBag.SortOrder=sortOrder;ViewBag.PageSize=pageSize;return View(await _bills.GetPagedAsync(search,paymentStatus,fromDate,toDate,sortBy,sortOrder,page,pageSize));}
    public async Task<IActionResult> Details(int id){var b=await _bills.GetByIdAsync(id);return b is null?NotFound():View(b);}
    public async Task<IActionResult> Create(int? appointmentId)
    {
        var vm=new BillFormViewModel{PaymentStatus="Unpaid"};if(appointmentId.HasValue){var a=await _appointments.GetDetailsAsync(appointmentId.Value);if(a is not null){vm.AppointmentID=a.AppointmentID;vm.PatientID=a.PatientID;vm.Amount=a.ConsultationFee;}}
        await PopulateAsync(vm);return View(vm);
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BillFormViewModel vm)
    {
        var a=vm.AppointmentID>0?await _appointments.GetDetailsAsync(vm.AppointmentID):null;
        if(a is null)ModelState.AddModelError(nameof(vm.AppointmentID),"Select a valid appointment.");
        else{if(a.PatientID!=vm.PatientID)ModelState.AddModelError(nameof(vm.PatientID),"Patient must match the appointment.");if(a.Status!="Completed")ModelState.AddModelError(nameof(vm.AppointmentID),"Bills can be generated only for completed appointments.");if(await _bills.AppointmentHasBillAsync(vm.AppointmentID))ModelState.AddModelError(nameof(vm.AppointmentID),"A bill already exists for this appointment.");}
        if(!new[]{"Paid","Unpaid","Partially Paid"}.Contains(vm.PaymentStatus))ModelState.AddModelError(nameof(vm.PaymentStatus),"Invalid payment status.");
        if(!ModelState.IsValid){await PopulateAsync(vm);return View(vm);}var id=await _bills.CreateAsync(new Bill{AppointmentID=vm.AppointmentID,PatientID=vm.PatientID,Amount=vm.Amount,PaymentStatus=vm.PaymentStatus,PaymentMethod=vm.PaymentMethod});TempData["Success"]="Bill generated.";return RedirectToAction(nameof(Details),new{id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePayment(int id,string paymentStatus,string? paymentMethod)
    {if(!new[]{"Paid","Unpaid","Partially Paid"}.Contains(paymentStatus)){TempData["Error"]="Invalid payment status.";return RedirectToAction(nameof(Details),new{id});}await _bills.UpdatePaymentAsync(id,paymentStatus,paymentMethod);TempData["Success"]="Payment status updated.";return RedirectToAction(nameof(Details),new{id});}
    private async Task PopulateAsync(BillFormViewModel vm){vm.Patients=(await _patients.GetAllAsync()).Select(p=>new SelectListItem(p.FullName+" (#"+p.PatientID+")",p.PatientID.ToString()));vm.Appointments=(await _appointments.GetLookupAsync(true)).Select(a=>new SelectListItem($"#{a.AppointmentID} — {a.AppointmentDate:dd MMM yyyy} — {a.PatientName} — Rs {a.ConsultationFee:N2}",a.AppointmentID.ToString()));}
}
