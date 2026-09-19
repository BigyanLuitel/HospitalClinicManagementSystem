using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.ViewModels;

public class PrescriptionFormViewModel
{
    [Required]
    [Display(Name = "Appointment")]
    public int AppointmentID { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientID { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public int DoctorID { get; set; }

    [Required, StringLength(500)]
    [Display(Name = "Medicines")]
    public string Medicines { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Dosage / instructions")]
    public string? Dosage { get; set; }

    public IEnumerable<SelectListItem> Appointments { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Patients { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Doctors { get; set; } = Array.Empty<SelectListItem>();
}
