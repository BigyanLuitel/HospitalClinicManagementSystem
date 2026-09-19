namespace HospitalClinicManagementSystem.ViewModels;

public class ReportViewModel
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalAppointments { get; set; }
    public int ScheduledAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public decimal TotalBilled { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public IReadOnlyList<DailyAppointmentCount> DailyAppointments { get; set; } = Array.Empty<DailyAppointmentCount>();
}

public class DailyAppointmentCount
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}
