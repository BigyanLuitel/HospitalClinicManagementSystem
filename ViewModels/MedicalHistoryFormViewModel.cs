using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.ViewModels;

public class MedicalHistoryFormViewModel
{
    [Required]
    [Display(Name = "Patient")]
    public int PatientID { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public int DoctorID { get; set; }

    [Display(Name = "Appointment")]
    public int? AppointmentID { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Visit date")]
    public DateTime VisitDate { get; set; } = DateTime.Today;

    [StringLength(300)]
    public string? Diagnosis { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public IEnumerable<SelectListItem> Patients { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Doctors { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Appointments { get; set; } = Array.Empty<SelectListItem>();
}
