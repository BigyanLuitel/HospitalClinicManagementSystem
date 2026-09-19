using System.ComponentModel.DataAnnotations;

namespace HospitalClinicManagementSystem.Models;

public class Prescription
{
    public int PrescriptionID { get; set; }

    [Required]
    public int AppointmentID { get; set; }

    [Required]
    public int PatientID { get; set; }

    [Required]
    public int DoctorID { get; set; }

    [Required, StringLength(500)]
    public string Medicines { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Dosage { get; set; }

    [Display(Name = "Date issued")]
    public DateTime DateIssued { get; set; }
}
