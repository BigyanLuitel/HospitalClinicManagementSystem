using System.Data;
using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Repositories;

public class AppointmentRepository
{
    private readonly IDbConnectionFactory _factory;
    public AppointmentRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<AppointmentListItem>> GetPagedAsync(
        string? search, int? doctorId, string? status, DateTime? fromDate, DateTime? toDate,
        string? sortBy, string? sortOrder, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);

        var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["date"] = "a.AppointmentDate",
            ["time"] = "a.AppointmentTime",
            ["patient"] = "p.FirstName",
            ["doctor"] = "d.FullName",
            ["status"] = "a.Status",
            ["created"] = "a.CreatedDate"
        };
        var sortColumn = sortMap.TryGetValue(sortBy ?? "", out var mapped) ? mapped : "a.AppointmentDate";
        var direction = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        const string where = @"WHERE (@Search IS NULL OR p.FirstName LIKE '%' + @Search + '%' OR p.LastName LIKE '%' + @Search + '%'
                                      OR d.FullName LIKE '%' + @Search + '%' OR a.Reason LIKE '%' + @Search + '%')
                               AND (@DoctorID IS NULL OR a.DoctorID = @DoctorID)
                               AND (@Status IS NULL OR a.Status = @Status)
                               AND (@FromDate IS NULL OR a.AppointmentDate >= @FromDate)
                               AND (@ToDate IS NULL OR a.AppointmentDate <= @ToDate)";

        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        int total;
        await using (var count = new SqlCommand($@"SELECT COUNT(*)
                                                   FROM Appointments a
                                                   JOIN Patients p ON p.PatientID=a.PatientID
                                                   JOIN Doctors d ON d.DoctorID=a.DoctorID
                                                   {where};", connection))
        {
            AddFilters(count, search, doctorId, status, fromDate, toDate);
            total = Convert.ToInt32(await count.ExecuteScalarAsync());
        }

        var list = new List<AppointmentListItem>();
        var sql = $@"SELECT a.AppointmentID,a.PatientID,p.FirstName+' '+p.LastName AS PatientName,
                            a.DoctorID,d.FullName,d.Specialization,a.AppointmentDate,a.AppointmentTime,
                            a.Reason,a.Status,a.CreatedDate,d.ConsultationFee
                     FROM Appointments a
                     JOIN Patients p ON p.PatientID=a.PatientID
                     JOIN Doctors d ON d.DoctorID=a.DoctorID
                     {where}
                     ORDER BY {sortColumn} {direction}, a.AppointmentTime {direction}, a.AppointmentID DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        await using (var cmd = new SqlCommand(sql, connection))
        {
            AddFilters(cmd, search, doctorId, status, fromDate, toDate);
            cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) list.Add(MapListItem(reader));
        }

        return new PagedResult<AppointmentListItem> { Items = list, Page = page, PageSize = pageSize, TotalItems = total };
    }

    public async Task<AppointmentListItem?> GetDetailsAsync(int id)
    {
        const string sql = @"SELECT a.AppointmentID,a.PatientID,p.FirstName+' '+p.LastName AS PatientName,
                                    a.DoctorID,d.FullName,d.Specialization,a.AppointmentDate,a.AppointmentTime,
                                    a.Reason,a.Status,a.CreatedDate,d.ConsultationFee
                             FROM Appointments a
                             JOIN Patients p ON p.PatientID=a.PatientID
                             JOIN Doctors d ON d.DoctorID=a.DoctorID
                             WHERE a.AppointmentID=@Id;";
        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection); cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await using var r = await cmd.ExecuteReaderAsync(); return await r.ReadAsync() ? MapListItem(r) : null;
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        const string sql = @"SELECT AppointmentID,PatientID,DoctorID,AppointmentDate,AppointmentTime,Reason,Status,CreatedDate
                             FROM Appointments WHERE AppointmentID=@Id;";
        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection); cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await using var r = await cmd.ExecuteReaderAsync();
        if (!await r.ReadAsync()) return null;
        return new Appointment
        {
            AppointmentID=r.GetInt32(0), PatientID=r.GetInt32(1), DoctorID=r.GetInt32(2), AppointmentDate=r.GetDateTime(3),
            AppointmentTime=r.GetTimeSpan(4), Reason=r.IsDBNull(5)?null:r.GetString(5), Status=r.GetString(6), CreatedDate=r.GetDateTime(7)
        };
    }

    public async Task<List<AppointmentListItem>> GetLookupAsync(bool completedOnly = false, int limit = 250)
    {
        var list = new List<AppointmentListItem>();
        var sql = $@"SELECT TOP ({Math.Clamp(limit, 1, 1000)}) a.AppointmentID,a.PatientID,p.FirstName+' '+p.LastName AS PatientName,
                            a.DoctorID,d.FullName,d.Specialization,a.AppointmentDate,a.AppointmentTime,
                            a.Reason,a.Status,a.CreatedDate,d.ConsultationFee
                     FROM Appointments a
                     JOIN Patients p ON p.PatientID=a.PatientID
                     JOIN Doctors d ON d.DoctorID=a.DoctorID
                     {(completedOnly ? "WHERE a.Status='Completed'" : "")}
                     ORDER BY a.AppointmentDate DESC,a.AppointmentTime DESC;";
        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection); await using var r = await cmd.ExecuteReaderAsync();
        while (await r.ReadAsync()) list.Add(MapListItem(r)); return list;
    }

    public async Task<bool> HasConflictAsync(int doctorId, DateTime date, TimeSpan time, int? excludeAppointmentId = null)
    {
        const string sql = @"SELECT COUNT(*) FROM Appointments
                             WHERE DoctorID=@DoctorID AND AppointmentDate=@Date AND AppointmentTime=@Time
                               AND Status <> 'Cancelled' AND (@ExcludeID IS NULL OR AppointmentID<>@ExcludeID);";
        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@DoctorID", SqlDbType.Int).Value = doctorId;
        cmd.Parameters.Add("@Date", SqlDbType.Date).Value = date.Date;
        cmd.Parameters.Add("@Time", SqlDbType.Time).Value = time;
        cmd.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = (object?)excludeAppointmentId ?? DBNull.Value;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
    }

    public async Task<int> CreateAsync(Appointment a)
    {
        const string sql = @"INSERT INTO Appointments(PatientID,DoctorID,AppointmentDate,AppointmentTime,Reason,Status)
                             VALUES(@PatientID,@DoctorID,@AppointmentDate,@AppointmentTime,@Reason,@Status);
                             SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var connection=_factory.CreateConnection(); await connection.OpenAsync(); await using var cmd=new SqlCommand(sql,connection); AddEntity(cmd,a); return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateAsync(Appointment a)
    {
        const string sql = @"UPDATE Appointments SET PatientID=@PatientID,DoctorID=@DoctorID,AppointmentDate=@AppointmentDate,
                             AppointmentTime=@AppointmentTime,Reason=@Reason,Status=@Status WHERE AppointmentID=@AppointmentID;";
        await using var connection=_factory.CreateConnection(); await connection.OpenAsync(); await using var cmd=new SqlCommand(sql,connection); AddEntity(cmd,a); cmd.Parameters.Add("@AppointmentID",SqlDbType.Int).Value=a.AppointmentID; return await cmd.ExecuteNonQueryAsync()>0;
    }

    public async Task<bool> CancelAsync(int id)
    {
        await using var connection=_factory.CreateConnection(); await connection.OpenAsync(); await using var cmd=new SqlCommand("UPDATE Appointments SET Status='Cancelled' WHERE AppointmentID=@Id AND Status<>'Completed';",connection); cmd.Parameters.Add("@Id",SqlDbType.Int).Value=id; return await cmd.ExecuteNonQueryAsync()>0;
    }

    private static void AddFilters(SqlCommand cmd, string? search, int? doctorId, string? status, DateTime? fromDate, DateTime? toDate)
    {
        cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim();
        cmd.Parameters.Add("@DoctorID", SqlDbType.Int).Value = (object?)doctorId ?? DBNull.Value;
        cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(status) ? DBNull.Value : status;
        cmd.Parameters.Add("@FromDate", SqlDbType.Date).Value = (object?)fromDate?.Date ?? DBNull.Value;
        cmd.Parameters.Add("@ToDate", SqlDbType.Date).Value = (object?)toDate?.Date ?? DBNull.Value;
    }

    private static void AddEntity(SqlCommand cmd, Appointment a)
    {
        cmd.Parameters.Add("@PatientID", SqlDbType.Int).Value=a.PatientID;
        cmd.Parameters.Add("@DoctorID", SqlDbType.Int).Value=a.DoctorID;
        cmd.Parameters.Add("@AppointmentDate", SqlDbType.Date).Value=a.AppointmentDate.Date;
        cmd.Parameters.Add("@AppointmentTime", SqlDbType.Time).Value=a.AppointmentTime;
        cmd.Parameters.Add("@Reason", SqlDbType.NVarChar,200).Value=(object?)a.Reason?.Trim()??DBNull.Value;
        cmd.Parameters.Add("@Status", SqlDbType.NVarChar,20).Value=a.Status;
    }

    private static AppointmentListItem MapListItem(SqlDataReader r) => new()
    {
        AppointmentID=r.GetInt32(0), PatientID=r.GetInt32(1), PatientName=r.GetString(2), DoctorID=r.GetInt32(3), DoctorName=r.GetString(4),
        Specialization=r.GetString(5), AppointmentDate=r.GetDateTime(6), AppointmentTime=r.GetTimeSpan(7), Reason=r.IsDBNull(8)?null:r.GetString(8),
        Status=r.GetString(9), CreatedDate=r.GetDateTime(10), ConsultationFee=r.GetDecimal(11)
    };
}
