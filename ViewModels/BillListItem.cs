namespace HospitalClinicManagementSystem.ViewModels;

public class BillListItem
{
    public int BillID { get; set; }
    public int PatientID { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public int AppointmentID { get; set; }
    public DateTime AppointmentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = string.Empty;
    public string? PaymentMethod { get; set; }
    public DateTime BillDate { get; set; }
}
