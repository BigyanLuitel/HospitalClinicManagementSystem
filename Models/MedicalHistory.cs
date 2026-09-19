using System.ComponentModel.DataAnnotations;

namespace HospitalClinicManagementSystem.Models;

public class MedicalHistory
{
    public int HistoryID { get; set; }

    [Required]
    public int PatientID { get; set; }

    [Required]
    public int DoctorID { get; set; }

    public int? AppointmentID { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Visit date")]
    public DateTime VisitDate { get; set; }

    [StringLength(300)]
    public string? Diagnosis { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }
}
