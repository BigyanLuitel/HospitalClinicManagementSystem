namespace HospitalClinicManagementSystem.ViewModels;

public class PatientSearchResult
{
    public int PatientID { get; set; }
    public string FullName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string ContactNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? BloodGroup { get; set; }
}
