namespace HospitalClinicManagementSystem.ViewModels;

public class DashboardViewModel
{
    public int PatientCount { get; set; }
    public int ActiveDoctorCount { get; set; }
    public int TodayAppointmentCount { get; set; }
    public int UnpaidBillCount { get; set; }
    public decimal ThisMonthRevenue { get; set; }
    public IReadOnlyList<AppointmentListItem> UpcomingAppointments { get; set; } = Array.Empty<AppointmentListItem>();
}
