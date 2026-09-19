using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.ViewModels;

public class BillFormViewModel
{
    [Required]
    [Display(Name = "Appointment")]
    public int AppointmentID { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientID { get; set; }

    [Required, Range(0.01, 99999999)]
    public decimal Amount { get; set; }

    [Required]
    [Display(Name = "Payment status")]
    public string PaymentStatus { get; set; } = "Unpaid";

    [Display(Name = "Payment method")]
    public string? PaymentMethod { get; set; }

    public IEnumerable<SelectListItem> Appointments { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Patients { get; set; } = Array.Empty<SelectListItem>();
}
