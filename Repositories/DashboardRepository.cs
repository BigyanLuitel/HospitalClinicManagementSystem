using System.Data;
using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Repositories;

public class DashboardRepository
{
    private readonly IDbConnectionFactory _factory;
    public DashboardRepository(IDbConnectionFactory factory)=>_factory=factory;

    public async Task<DashboardViewModel> GetDashboardAsync()
    {
        await using var c=_factory.CreateConnection();await c.OpenAsync();
        const string summary=@"SELECT
            (SELECT COUNT(*) FROM Patients),
            (SELECT COUNT(*) FROM Doctors WHERE Status='Active'),
            (SELECT COUNT(*) FROM Appointments WHERE AppointmentDate=CAST(GETDATE() AS DATE) AND Status<>'Cancelled'),
            (SELECT COUNT(*) FROM Bills WHERE PaymentStatus<>'Paid'),
            (SELECT COALESCE(SUM(Amount),0) FROM Bills WHERE PaymentStatus='Paid' AND YEAR(BillDate)=YEAR(GETDATE()) AND MONTH(BillDate)=MONTH(GETDATE()));";
        var vm=new DashboardViewModel();
        await using(var cmd=new SqlCommand(summary,c)){await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();vm.PatientCount=r.GetInt32(0);vm.ActiveDoctorCount=r.GetInt32(1);vm.TodayAppointmentCount=r.GetInt32(2);vm.UnpaidBillCount=r.GetInt32(3);vm.ThisMonthRevenue=r.GetDecimal(4);}

        var upcoming=new List<AppointmentListItem>();
        const string sql=@"SELECT TOP 8 a.AppointmentID,a.PatientID,p.FirstName+' '+p.LastName,a.DoctorID,d.FullName,d.Specialization,a.AppointmentDate,a.AppointmentTime,a.Reason,a.Status,a.CreatedDate,d.ConsultationFee
                           FROM Appointments a JOIN Patients p ON p.PatientID=a.PatientID JOIN Doctors d ON d.DoctorID=a.DoctorID
                           WHERE a.Status='Scheduled' AND (a.AppointmentDate>CAST(GETDATE() AS DATE) OR (a.AppointmentDate=CAST(GETDATE() AS DATE) AND a.AppointmentTime>=CAST(GETDATE() AS TIME)))
                           ORDER BY a.AppointmentDate,a.AppointmentTime;";
        await using(var cmd=new SqlCommand(sql,c)){await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())upcoming.Add(new AppointmentListItem{AppointmentID=r.GetInt32(0),PatientID=r.GetInt32(1),PatientName=r.GetString(2),DoctorID=r.GetInt32(3),DoctorName=r.GetString(4),Specialization=r.GetString(5),AppointmentDate=r.GetDateTime(6),AppointmentTime=r.GetTimeSpan(7),Reason=r.IsDBNull(8)?null:r.GetString(8),Status=r.GetString(9),CreatedDate=r.GetDateTime(10),ConsultationFee=r.GetDecimal(11)});}
        vm.UpcomingAppointments=upcoming;return vm;
    }

    public async Task<ReportViewModel> GetReportAsync(DateTime fromDate,DateTime toDate)
    {
        var vm=new ReportViewModel{FromDate=fromDate.Date,ToDate=toDate.Date};await using var c=_factory.CreateConnection();await c.OpenAsync();
        const string appSummary=@"SELECT COUNT(*),SUM(CASE WHEN Status='Scheduled' THEN 1 ELSE 0 END),SUM(CASE WHEN Status='Completed' THEN 1 ELSE 0 END),SUM(CASE WHEN Status='Cancelled' THEN 1 ELSE 0 END)
                                  FROM Appointments WHERE AppointmentDate BETWEEN @FromDate AND @ToDate;";
        await using(var cmd=new SqlCommand(appSummary,c)){cmd.Parameters.Add("@FromDate",SqlDbType.Date).Value=fromDate.Date;cmd.Parameters.Add("@ToDate",SqlDbType.Date).Value=toDate.Date;await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();vm.TotalAppointments=r.IsDBNull(0)?0:r.GetInt32(0);vm.ScheduledAppointments=r.IsDBNull(1)?0:r.GetInt32(1);vm.CompletedAppointments=r.IsDBNull(2)?0:r.GetInt32(2);vm.CancelledAppointments=r.IsDBNull(3)?0:r.GetInt32(3);}
        const string billSummary=@"SELECT COALESCE(SUM(Amount),0),COALESCE(SUM(CASE WHEN PaymentStatus='Paid' THEN Amount ELSE 0 END),0),COALESCE(SUM(CASE WHEN PaymentStatus<>'Paid' THEN Amount ELSE 0 END),0)
                                   FROM Bills WHERE CAST(BillDate AS DATE) BETWEEN @FromDate AND @ToDate;";
        await using(var cmd=new SqlCommand(billSummary,c)){cmd.Parameters.Add("@FromDate",SqlDbType.Date).Value=fromDate.Date;cmd.Parameters.Add("@ToDate",SqlDbType.Date).Value=toDate.Date;await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();vm.TotalBilled=r.GetDecimal(0);vm.TotalPaid=r.GetDecimal(1);vm.TotalOutstanding=r.GetDecimal(2);}
        var daily=new List<DailyAppointmentCount>();
        const string dailySql=@"SELECT AppointmentDate,COUNT(*) FROM Appointments WHERE AppointmentDate BETWEEN @FromDate AND @ToDate GROUP BY AppointmentDate ORDER BY AppointmentDate;";
        await using(var cmd=new SqlCommand(dailySql,c)){cmd.Parameters.Add("@FromDate",SqlDbType.Date).Value=fromDate.Date;cmd.Parameters.Add("@ToDate",SqlDbType.Date).Value=toDate.Date;await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())daily.Add(new DailyAppointmentCount{Date=r.GetDateTime(0),Count=r.GetInt32(1)});}
        vm.DailyAppointments=daily;return vm;
    }
}
