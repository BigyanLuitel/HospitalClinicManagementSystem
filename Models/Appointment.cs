using System.ComponentModel.DataAnnotations;

namespace HospitalClinicManagementSystem.Models;

public class Appointment
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
    public DateTime AppointmentDate { get; set; }

    [Required, DataType(DataType.Time)]
    [Display(Name = "Appointment time")]
    public TimeSpan AppointmentTime { get; set; }

    [StringLength(200)]
    public string? Reason { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Scheduled";

    public DateTime CreatedDate { get; set; }
}
