using System.Data;
using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Repositories;

public class MedicalHistoryRepository
{
    private readonly IDbConnectionFactory _factory;
    public MedicalHistoryRepository(IDbConnectionFactory factory) => _factory=factory;

    public async Task<PagedResult<MedicalHistoryListItem>> GetPagedAsync(int? patientId, string? search, string? sortBy, string? sortOrder, int page, int pageSize)
    {
        page=Math.Max(1,page); pageSize=Math.Clamp(pageSize,5,100);
        var sortMap=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"date","h.VisitDate"},{"patient","p.FirstName"},{"doctor","d.FullName"},{"diagnosis","h.Diagnosis"}};
        var sortColumn=sortMap.TryGetValue(sortBy??"",out var mapped)?mapped:"h.VisitDate";
        var direction=string.Equals(sortOrder,"asc",StringComparison.OrdinalIgnoreCase)?"ASC":"DESC";
        const string where=@"WHERE (@PatientID IS NULL OR h.PatientID=@PatientID)
                             AND (@Search IS NULL OR p.FirstName LIKE '%' + @Search + '%' OR p.LastName LIKE '%' + @Search + '%'
                                  OR h.Diagnosis LIKE '%' + @Search + '%' OR h.Notes LIKE '%' + @Search + '%')";
        await using var c=_factory.CreateConnection(); await c.OpenAsync();
        int total;
        await using(var count=new SqlCommand($"SELECT COUNT(*) FROM MedicalHistory h JOIN Patients p ON p.PatientID=h.PatientID {where};",c))
        { AddFilters(count,patientId,search); total=Convert.ToInt32(await count.ExecuteScalarAsync()); }
        var list=new List<MedicalHistoryListItem>();
        var sql=$@"SELECT h.HistoryID,h.PatientID,p.FirstName+' '+p.LastName,h.DoctorID,d.FullName,h.AppointmentID,h.VisitDate,h.Diagnosis,h.Notes
                   FROM MedicalHistory h JOIN Patients p ON p.PatientID=h.PatientID JOIN Doctors d ON d.DoctorID=h.DoctorID
                   {where} ORDER BY {sortColumn} {direction},h.HistoryID DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        await using(var cmd=new SqlCommand(sql,c))
        { AddFilters(cmd,patientId,search);cmd.Parameters.Add("@Offset",SqlDbType.Int).Value=(page-1)*pageSize;cmd.Parameters.Add("@PageSize",SqlDbType.Int).Value=pageSize;
          await using var r=await cmd.ExecuteReaderAsync(); while(await r.ReadAsync()) list.Add(new MedicalHistoryListItem{HistoryID=r.GetInt32(0),PatientID=r.GetInt32(1),PatientName=r.GetString(2),DoctorID=r.GetInt32(3),DoctorName=r.GetString(4),AppointmentID=r.IsDBNull(5)?null:r.GetInt32(5),VisitDate=r.GetDateTime(6),Diagnosis=r.IsDBNull(7)?null:r.GetString(7),Notes=r.IsDBNull(8)?null:r.GetString(8)}); }
        return new PagedResult<MedicalHistoryListItem>{Items=list,Page=page,PageSize=pageSize,TotalItems=total};
    }

    public async Task<int> CreateAsync(MedicalHistory h)
    {
        const string sql=@"INSERT INTO MedicalHistory(PatientID,DoctorID,AppointmentID,VisitDate,Diagnosis,Notes)
                           VALUES(@PatientID,@DoctorID,@AppointmentID,@VisitDate,@Diagnosis,@Notes); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);
        cmd.Parameters.Add("@PatientID",SqlDbType.Int).Value=h.PatientID;cmd.Parameters.Add("@DoctorID",SqlDbType.Int).Value=h.DoctorID;
        cmd.Parameters.Add("@AppointmentID",SqlDbType.Int).Value=(object?)h.AppointmentID??DBNull.Value;cmd.Parameters.Add("@VisitDate",SqlDbType.Date).Value=h.VisitDate.Date;
        cmd.Parameters.Add("@Diagnosis",SqlDbType.NVarChar,300).Value=(object?)h.Diagnosis?.Trim()??DBNull.Value;cmd.Parameters.Add("@Notes",SqlDbType.NVarChar,500).Value=(object?)h.Notes?.Trim()??DBNull.Value;
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> AppointmentHasHistoryAsync(int appointmentId)
    {
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand("SELECT COUNT(*) FROM MedicalHistory WHERE AppointmentID=@Id;",c);cmd.Parameters.Add("@Id",SqlDbType.Int).Value=appointmentId;return Convert.ToInt32(await cmd.ExecuteScalarAsync())>0;
    }

    private static void AddFilters(SqlCommand cmd,int? patientId,string? search)
    {cmd.Parameters.Add("@PatientID",SqlDbType.Int).Value=(object?)patientId??DBNull.Value;cmd.Parameters.Add("@Search",SqlDbType.NVarChar,100).Value=string.IsNullOrWhiteSpace(search)?DBNull.Value:search.Trim();}
}
