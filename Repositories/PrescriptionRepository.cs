using System.Data;
using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Repositories;

public class PrescriptionRepository
{
    private readonly IDbConnectionFactory _factory;
    public PrescriptionRepository(IDbConnectionFactory factory)=>_factory=factory;

    public async Task<PagedResult<PrescriptionListItem>> GetPagedAsync(int? patientId,string? search,string? sortBy,string? sortOrder,int page,int pageSize)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,5,100);var direction=string.Equals(sortOrder,"asc",StringComparison.OrdinalIgnoreCase)?"ASC":"DESC";
        var sortMap=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){{"date","pr.DateIssued"},{"patient","p.FirstName"},{"doctor","d.FullName"},{"medicine","pr.Medicines"}};
        var sortColumn=sortMap.TryGetValue(sortBy??"",out var mapped)?mapped:"pr.DateIssued";
        const string where=@"WHERE (@PatientID IS NULL OR pr.PatientID=@PatientID)
                             AND (@Search IS NULL OR p.FirstName LIKE '%' + @Search + '%' OR p.LastName LIKE '%' + @Search + '%'
                                  OR d.FullName LIKE '%' + @Search + '%' OR pr.Medicines LIKE '%' + @Search + '%')";
        await using var c=_factory.CreateConnection();await c.OpenAsync();int total;
        await using(var count=new SqlCommand($"SELECT COUNT(*) FROM Prescriptions pr JOIN Patients p ON p.PatientID=pr.PatientID JOIN Doctors d ON d.DoctorID=pr.DoctorID {where};",c))
        {AddFilters(count,patientId,search);total=Convert.ToInt32(await count.ExecuteScalarAsync());}
        var list=new List<PrescriptionListItem>();var sql=$@"SELECT pr.PrescriptionID,pr.AppointmentID,pr.PatientID,p.FirstName+' '+p.LastName,pr.DoctorID,d.FullName,pr.Medicines,pr.Dosage,pr.DateIssued
                   FROM Prescriptions pr JOIN Patients p ON p.PatientID=pr.PatientID JOIN Doctors d ON d.DoctorID=pr.DoctorID
                   {where} ORDER BY {sortColumn} {direction},pr.PrescriptionID DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        await using(var cmd=new SqlCommand(sql,c)){AddFilters(cmd,patientId,search);cmd.Parameters.Add("@Offset",SqlDbType.Int).Value=(page-1)*pageSize;cmd.Parameters.Add("@PageSize",SqlDbType.Int).Value=pageSize;
            await using var r=await cmd.ExecuteReaderAsync();while(await r.ReadAsync())list.Add(new PrescriptionListItem{PrescriptionID=r.GetInt32(0),AppointmentID=r.GetInt32(1),PatientID=r.GetInt32(2),PatientName=r.GetString(3),DoctorID=r.GetInt32(4),DoctorName=r.GetString(5),Medicines=r.GetString(6),Dosage=r.IsDBNull(7)?null:r.GetString(7),DateIssued=r.GetDateTime(8)});}
        return new PagedResult<PrescriptionListItem>{Items=list,Page=page,PageSize=pageSize,TotalItems=total};
    }

    public async Task<PrescriptionListItem?> GetByIdAsync(int id)
    {
        const string sql=@"SELECT pr.PrescriptionID,pr.AppointmentID,pr.PatientID,p.FirstName+' '+p.LastName,pr.DoctorID,d.FullName,pr.Medicines,pr.Dosage,pr.DateIssued
                           FROM Prescriptions pr JOIN Patients p ON p.PatientID=pr.PatientID JOIN Doctors d ON d.DoctorID=pr.DoctorID WHERE pr.PrescriptionID=@Id;";
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);cmd.Parameters.Add("@Id",SqlDbType.Int).Value=id;await using var r=await cmd.ExecuteReaderAsync();if(!await r.ReadAsync())return null;
        return new PrescriptionListItem{PrescriptionID=r.GetInt32(0),AppointmentID=r.GetInt32(1),PatientID=r.GetInt32(2),PatientName=r.GetString(3),DoctorID=r.GetInt32(4),DoctorName=r.GetString(5),Medicines=r.GetString(6),Dosage=r.IsDBNull(7)?null:r.GetString(7),DateIssued=r.GetDateTime(8)};
    }

    public async Task<int> CreateAsync(Prescription p)
    {
        const string sql=@"INSERT INTO Prescriptions(AppointmentID,PatientID,DoctorID,Medicines,Dosage) VALUES(@AppointmentID,@PatientID,@DoctorID,@Medicines,@Dosage);SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var c=_factory.CreateConnection();await c.OpenAsync();await using var cmd=new SqlCommand(sql,c);cmd.Parameters.Add("@AppointmentID",SqlDbType.Int).Value=p.AppointmentID;cmd.Parameters.Add("@PatientID",SqlDbType.Int).Value=p.PatientID;cmd.Parameters.Add("@DoctorID",SqlDbType.Int).Value=p.DoctorID;cmd.Parameters.Add("@Medicines",SqlDbType.NVarChar,500).Value=p.Medicines.Trim();cmd.Parameters.Add("@Dosage",SqlDbType.NVarChar,200).Value=(object?)p.Dosage?.Trim()??DBNull.Value;return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
    private static void AddFilters(SqlCommand cmd,int? patientId,string? search){cmd.Parameters.Add("@PatientID",SqlDbType.Int).Value=(object?)patientId??DBNull.Value;cmd.Parameters.Add("@Search",SqlDbType.NVarChar,100).Value=string.IsNullOrWhiteSpace(search)?DBNull.Value:search.Trim();}
}
