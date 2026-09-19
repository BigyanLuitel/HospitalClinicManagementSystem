using System.ComponentModel.DataAnnotations;

namespace HospitalClinicManagementSystem.Models;

public class Patient
{
    public int PatientID { get; set; }

    [Required, StringLength(50)]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Required, StringLength(50)]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required, DataType(DataType.Date)]
    [Display(Name = "Date of birth")]
    public DateTime DateOfBirth { get; set; }

    [Required, StringLength(10)]
    public string Gender { get; set; } = string.Empty;

    [Required, StringLength(15)]
    [Display(Name = "Contact number")]
    public string ContactNumber { get; set; } = string.Empty;

    [EmailAddress, StringLength(100)]
    public string? Email { get; set; }

    [StringLength(200)]
    public string? Address { get; set; }

    [StringLength(5)]
    [Display(Name = "Blood group")]
    public string? BloodGroup { get; set; }

    [Display(Name = "Registered")]
    public DateTime RegistrationDate { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
