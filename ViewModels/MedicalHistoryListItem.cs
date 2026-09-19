namespace HospitalClinicManagementSystem.ViewModels;

public class MedicalHistoryListItem
{
    public int HistoryID { get; set; }
    public int PatientID { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorID { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int? AppointmentID { get; set; }
    public DateTime VisitDate { get; set; }
    public string? Diagnosis { get; set; }
    public string? Notes { get; set; }
}
