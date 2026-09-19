using System.Data;
using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.Models;
using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Repositories;

public class DoctorRepository
{
    private readonly IDbConnectionFactory _factory;
    public DoctorRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<Doctor>> GetPagedAsync(string? search, string? specialization, string? sortBy, string? sortOrder, int page, int pageSize)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 5, 100);
        var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        { ["name"]="FullName", ["specialization"]="Specialization", ["fee"]="ConsultationFee", ["status"]="Status" };
        var column = sortMap.TryGetValue(sortBy ?? "", out var c) ? c : "FullName";
        var direction = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC";
        const string where = @"WHERE (@Search IS NULL OR FullName LIKE '%' + @Search + '%' OR ContactNumber LIKE '%' + @Search + '%')
                               AND (@Specialization IS NULL OR Specialization = @Specialization)";

        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        int total;
        await using (var count = new SqlCommand($"SELECT COUNT(*) FROM Doctors {where};", connection))
        {
            AddFilters(count, search, specialization); total = Convert.ToInt32(await count.ExecuteScalarAsync());
        }
        var items = new List<Doctor>();
        var sql = $@"SELECT DoctorID, FullName, Specialization, ContactNumber, Email, AvailableDays, ConsultationFee, Status
                     FROM Doctors {where} ORDER BY {column} {direction}, DoctorID DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        await using (var cmd = new SqlCommand(sql, connection))
        {
            AddFilters(cmd, search, specialization);
            cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
            await using var reader = await cmd.ExecuteReaderAsync(); while (await reader.ReadAsync()) items.Add(Map(reader));
        }
        return new PagedResult<Doctor> { Items=items, Page=page, PageSize=pageSize, TotalItems=total };
    }

    public async Task<List<string>> GetSpecializationsAsync()
    {
        var list = new List<string>();
        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd = new SqlCommand("SELECT DISTINCT Specialization FROM Doctors WHERE Specialization IS NOT NULL ORDER BY Specialization;", connection);
        await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) list.Add(r.GetString(0));
        return list;
    }

    public async Task<List<Doctor>> GetActiveAsync()
    {
        var list = new List<Doctor>();
        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd = new SqlCommand("SELECT DoctorID, FullName, Specialization, ContactNumber, Email, AvailableDays, ConsultationFee, Status FROM Doctors WHERE Status='Active' ORDER BY FullName;", connection);
        await using var r = await cmd.ExecuteReaderAsync(); while (await r.ReadAsync()) list.Add(Map(r)); return list;
    }

    public async Task<Doctor?> GetByIdAsync(int id)
    {
        await using var connection = _factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd = new SqlCommand("SELECT DoctorID, FullName, Specialization, ContactNumber, Email, AvailableDays, ConsultationFee, Status FROM Doctors WHERE DoctorID=@Id;", connection);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value=id; await using var r=await cmd.ExecuteReaderAsync(); return await r.ReadAsync()?Map(r):null;
    }

    public async Task<int> CreateAsync(Doctor d)
    {
        const string sql=@"INSERT INTO Doctors(FullName,Specialization,ContactNumber,Email,AvailableDays,ConsultationFee,Status)
                           VALUES(@FullName,@Specialization,@ContactNumber,@Email,@AvailableDays,@ConsultationFee,@Status); SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var connection=_factory.CreateConnection(); await connection.OpenAsync(); await using var cmd=new SqlCommand(sql,connection); AddEntity(cmd,d); return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateAsync(Doctor d)
    {
        const string sql=@"UPDATE Doctors SET FullName=@FullName,Specialization=@Specialization,ContactNumber=@ContactNumber,Email=@Email,AvailableDays=@AvailableDays,ConsultationFee=@ConsultationFee,Status=@Status WHERE DoctorID=@DoctorID;";
        await using var connection=_factory.CreateConnection(); await connection.OpenAsync(); await using var cmd=new SqlCommand(sql,connection); AddEntity(cmd,d); cmd.Parameters.Add("@DoctorID",SqlDbType.Int).Value=d.DoctorID; return await cmd.ExecuteNonQueryAsync()>0;
    }

    public async Task<bool> DeactivateAsync(int id)
    {
        await using var connection=_factory.CreateConnection(); await connection.OpenAsync(); await using var cmd=new SqlCommand("UPDATE Doctors SET Status='Inactive' WHERE DoctorID=@Id;",connection); cmd.Parameters.Add("@Id",SqlDbType.Int).Value=id; return await cmd.ExecuteNonQueryAsync()>0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection=_factory.CreateConnection(); await connection.OpenAsync();
        await using var cmd=new SqlCommand("DELETE FROM Doctors WHERE DoctorID=@Id;",connection); cmd.Parameters.Add("@Id",SqlDbType.Int).Value=id;
        try { return await cmd.ExecuteNonQueryAsync()>0; }
        catch (SqlException ex) when (ex.Number == 547) { return false; }
    }

    private static void AddFilters(SqlCommand cmd,string? search,string? specialization)
    {
        cmd.Parameters.Add("@Search",SqlDbType.NVarChar,100).Value=string.IsNullOrWhiteSpace(search)?DBNull.Value:search.Trim();
        cmd.Parameters.Add("@Specialization",SqlDbType.NVarChar,100).Value=string.IsNullOrWhiteSpace(specialization)?DBNull.Value:specialization;
    }
    private static void AddEntity(SqlCommand cmd,Doctor d)
    {
        cmd.Parameters.Add("@FullName",SqlDbType.NVarChar,100).Value=d.FullName.Trim();
        cmd.Parameters.Add("@Specialization",SqlDbType.NVarChar,100).Value=d.Specialization.Trim();
        cmd.Parameters.Add("@ContactNumber",SqlDbType.NVarChar,15).Value=d.ContactNumber.Trim();
        cmd.Parameters.Add("@Email",SqlDbType.NVarChar,100).Value=(object?)d.Email?.Trim()??DBNull.Value;
        cmd.Parameters.Add("@AvailableDays",SqlDbType.NVarChar,50).Value=(object?)d.AvailableDays?.Trim()??DBNull.Value;
        var p=cmd.Parameters.Add("@ConsultationFee",SqlDbType.Decimal); p.Precision=10;p.Scale=2;p.Value=d.ConsultationFee;
        cmd.Parameters.Add("@Status",SqlDbType.NVarChar,20).Value=d.Status;
    }
    private static Doctor Map(SqlDataReader r)=>new(){DoctorID=r.GetInt32(0),FullName=r.GetString(1),Specialization=r.GetString(2),ContactNumber=r.GetString(3),Email=r.IsDBNull(4)?null:r.GetString(4),AvailableDays=r.IsDBNull(5)?null:r.GetString(5),ConsultationFee=r.GetDecimal(6),Status=r.GetString(7)};
}
