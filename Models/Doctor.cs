using System.ComponentModel.DataAnnotations;

namespace HospitalClinicManagementSystem.Models;

public class Doctor
{
    public int DoctorID { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Full name")]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Specialization { get; set; } = string.Empty;

    [Required, StringLength(15)]
    [Display(Name = "Contact number")]
    public string ContactNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [StringLength(50)]
    [Display(Name = "Available days")]
    public string? AvailableDays { get; set; }

    [Required, Range(0, 9999999)]
    [Display(Name = "Consultation fee")]
    public decimal ConsultationFee { get; set; }

    [Required, StringLength(20)]
    public string Status { get; set; } = "Active";
}
