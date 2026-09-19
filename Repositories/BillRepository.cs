using System.Data;
using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Repositories;

public class BillRepository
{
    private readonly IDbConnectionFactory _factory;
    public BillRepository(IDbConnectionFactory factory)=>_factory=factory;

    public async Task<PagedResult<BillListItem>> GetPagedAsync(string? search,string? paymentStatus,DateTime? fromDate,DateTime? toDate,string? sortBy,string? sortOrder,int page,int pageSize)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,5,100);
        var sortMap=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"date","b.BillDate"},{"patient","p.FirstName"},{"amount","b.Amount"},{"status","b.PaymentStatus"}};
        var column=sortMap.TryGetValue(sortBy??"",out var m)?m:"b.BillDate";var direction=string.Equals(sortOrder,"asc",StringComparison.OrdinalIgnoreCase)?"ASC":"DESC";
        const string where=@"WHERE (@Search IS NULL OR p.FirstName LIKE '%' + @Search + '%' OR p.LastName LIKE '%' + @Search + '%' OR CAST(b.BillID AS NVARCHAR(20))=@Search)
                             AND (@Status IS NULL OR b.PaymentStatus=@Status)
                             AND (@FromDate IS NULL OR CAST(b.BillDate AS DATE)>=@FromDate)
                             AND (@ToDate IS NULL OR CAST(b.BillDate AS DATE)<=@ToDate)";
        await using var c=_factory.CreateConnection();await c.OpenAsync();int total;
        await using(var count=new SqlCommand($"SELECT COUNT(*) FROM Bills b JOIN Patients p ON p.PatientID=b.PatientID {where};",c)){AddFilters(count,search,paymentStatus,fromDate,toDate);total=Convert.ToInt32(await count.ExecuteScalarAsync());}
        var list=new List<BillListItem>();var sql=$@"SELECT b.BillID,b.PatientID,p.FirstName+' '+p.LastName,b.AppointmentID,a.AppointmentDate,b.Amount,b.PaymentStatus,b.PaymentMethod,b.BillDate
                   FROM Bills b JOIN Patients p ON p.PatientID=b.PatientID JOIN Appointments a ON a.AppointmentID=b.AppointmentID
                   {where} ORDER BY {column} {direction},b.BillID DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        await using(var cmd=new SqlCommand(sql,c)){AddFilters(cmd,search,paymentStatus,fromDate,toDate);cmd.Parameters.Add("@Offset",SqlDbType.Int).Value=(page-1)*pageSize;cmd.Parameters.Add("@PageSize",SqlDbType.Int).Value=pageSize;await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())list.Add(Map(r));}
        return new PagedResult<BillListItem>{Items=list,Page=page,PageSize=pageSize,TotalItems=total};
    }

    public async Task<BillListItem?> GetByIdAsync(int id)
    {
        const string sql=@"SELECT b.BillID,b.PatientID,p.FirstName+' '+p.LastName,b.AppointmentID,a.AppointmentDate,b.Amount,b.PaymentStatus,b.PaymentMethod,b.BillDate
                           FROM Bills b JOIN Patients p ON p.PatientID=b.PatientID JOIN Appointments a ON a.AppointmentID=b.AppointmentID WHERE b.BillID=@Id;";
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);cmd.Parameters.Add("@Id",SqlDbType.Int).Value=id;await using var r=await cmd.ExecuteReaderAsync();return await r.ReadAsync()?Map(r):null;
    }

    public async Task<bool> AppointmentHasBillAsync(int appointmentId)
    {await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand("SELECT COUNT(*) FROM Bills WHERE AppointmentID=@Id;",c);cmd.Parameters.Add("@Id",SqlDbType.Int).Value=appointmentId;return Convert.ToInt32(await cmd.ExecuteScalarAsync())>0;}

    public async Task<int> CreateAsync(Bill b)
    {
        const string sql=@"INSERT INTO Bills(PatientID,AppointmentID,Amount,PaymentStatus,PaymentMethod) VALUES(@PatientID,@AppointmentID,@Amount,@PaymentStatus,@PaymentMethod);SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);AddEntity(cmd,b);return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdatePaymentAsync(int billId,string paymentStatus,string? paymentMethod)
    {
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand("UPDATE Bills SET PaymentStatus=@Status,PaymentMethod=@Method WHERE BillID=@Id;",c);cmd.Parameters.Add("@Status",SqlDbType.NVarChar,20).Value=paymentStatus;cmd.Parameters.Add("@Method",SqlDbType.NVarChar,30).Value=(object?)paymentMethod??DBNull.Value;cmd.Parameters.Add("@Id",SqlDbType.Int).Value=billId;return await cmd.ExecuteNonQueryAsync()>0;
    }

    public async Task<(decimal billed,decimal paid,decimal outstanding)> GetSummaryAsync(DateTime fromDate,DateTime toDate)
    {
        const string sql=@"SELECT COALESCE(SUM(Amount),0),COALESCE(SUM(CASE WHEN PaymentStatus='Paid' THEN Amount ELSE 0 END),0),COALESCE(SUM(CASE WHEN PaymentStatus<>'Paid' THEN Amount ELSE 0 END),0)
                           FROM Bills WHERE CAST(BillDate AS DATE) BETWEEN @FromDate AND @ToDate;";
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);cmd.Parameters.Add("@FromDate",SqlDbType.Date).Value=fromDate.Date;cmd.Parameters.Add("@ToDate",SqlDbType.Date).Value=toDate.Date;await using var r=await cmd.ExecuteReaderAsync();await r.ReadAsync();return (r.GetDecimal(0),r.GetDecimal(1),r.GetDecimal(2));
    }

    private static void AddFilters(SqlCommand cmd,string? search,string? status,DateTime? fromDate,DateTime? toDate){cmd.Parameters.Add("@Search",SqlDbType.NVarChar,100).Value=string.IsNullOrWhiteSpace(search)?DBNull.Value:search.Trim();cmd.Parameters.Add("@Status",SqlDbType.NVarChar,20).Value=string.IsNullOrWhiteSpace(status)?DBNull.Value:status;cmd.Parameters.Add("@FromDate",SqlDbType.Date).Value=(object?)fromDate?.Date??DBNull.Value;cmd.Parameters.Add("@ToDate",SqlDbType.Date).Value=(object?)toDate?.Date??DBNull.Value;}
    private static void AddEntity(SqlCommand cmd,Bill b){cmd.Parameters.Add("@PatientID",SqlDbType.Int).Value=b.PatientID;cmd.Parameters.Add("@AppointmentID",SqlDbType.Int).Value=b.AppointmentID;var p=cmd.Parameters.Add("@Amount",SqlDbType.Decimal);p.Precision=10;p.Scale=2;p.Value=b.Amount;cmd.Parameters.Add("@PaymentStatus",SqlDbType.NVarChar,20).Value=b.PaymentStatus;cmd.Parameters.Add("@PaymentMethod",SqlDbType.NVarChar,30).Value=(object?)b.PaymentMethod??DBNull.Value;}
    private static BillListItem Map(SqlDataReader r)=>new(){BillID=r.GetInt32(0),PatientID=r.GetInt32(1),PatientName=r.GetString(2),AppointmentID=r.GetInt32(3),AppointmentDate=r.GetDateTime(4),Amount=r.GetDecimal(5),PaymentStatus=r.GetString(6),PaymentMethod=r.IsDBNull(7)?null:r.GetString(7),BillDate=r.GetDateTime(8)};
}
