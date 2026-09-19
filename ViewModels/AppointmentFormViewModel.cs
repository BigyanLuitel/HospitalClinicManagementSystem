using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HospitalClinicManagementSystem.ViewModels;

public class AppointmentFormViewModel
{
    public int AppointmentID { get; set; }

    [Required]
    [Display(Name = "Patient")]
    public int PatientID { get; set; }

    [Required]
    [Display(Name = "Doctor")]
    public int DoctorID { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Appointment date")]
    public DateTime AppointmentDate { get; set; } = DateTime.Today;

    [Required, DataType(DataType.Time)]
    [Display(Name = "Appointment time")]
    public TimeSpan AppointmentTime { get; set; } = new(9, 0, 0);

    [StringLength(200)]
    public string? Reason { get; set; }

    [Required]
    public string Status { get; set; } = "Scheduled";

    public IEnumerable<SelectListItem> Patients { get; set; } = Array.Empty<SelectListItem>();
    public IEnumerable<SelectListItem> Doctors { get; set; } = Array.Empty<SelectListItem>();
}
