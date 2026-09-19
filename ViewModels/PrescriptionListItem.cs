namespace HospitalClinicManagementSystem.ViewModels;

public class PrescriptionListItem
{
    public int PrescriptionID { get; set; }
    public int AppointmentID { get; set; }
    public int PatientID { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int DoctorID { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Medicines { get; set; } = string.Empty;
    public string? Dosage { get; set; }
    public DateTime DateIssued { get; set; }
}
