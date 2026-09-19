using System.Data;
using HospitalClinicManagementSystem.Data;
using HospitalClinicManagementSystem.Models;
using HospitalClinicManagementSystem.ViewModels;
using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Repositories;

public class PatientRepository
{
    private readonly IDbConnectionFactory _factory;
    public PatientRepository(IDbConnectionFactory factory) => _factory = factory;

    public async Task<PagedResult<Patient>> GetPagedAsync(string? search, string? sortBy, string? sortOrder, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 5, 100);
        var sortMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "PatientID",
            ["name"] = "FirstName",
            ["dob"] = "DateOfBirth",
            ["contact"] = "ContactNumber",
            ["registered"] = "RegistrationDate"
        };
        var sortColumn = sortMap.TryGetValue(sortBy ?? "", out var mapped) ? mapped : "RegistrationDate";
        var direction = string.Equals(sortOrder, "asc", StringComparison.OrdinalIgnoreCase) ? "ASC" : "DESC";

        const string where = "WHERE (@Search IS NULL OR FirstName LIKE '%' + @Search + '%' OR LastName LIKE '%' + @Search + '%' OR ContactNumber LIKE '%' + @Search + '%' OR CAST(PatientID AS NVARCHAR(20)) = @Search)";

        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();

        int total;
        await using (var count = new SqlCommand($"SELECT COUNT(*) FROM Patients {where};", connection))
        {
            count.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim();
            total = Convert.ToInt32(await count.ExecuteScalarAsync());
        }

        var items = new List<Patient>();
        var sql = $@"SELECT PatientID, FirstName, LastName, DateOfBirth, Gender, ContactNumber, Email, Address, BloodGroup, RegistrationDate
                     FROM Patients {where}
                     ORDER BY {sortColumn} {direction}, PatientID DESC
                     OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        await using (var cmd = new SqlCommand(sql, connection))
        {
            cmd.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(search) ? DBNull.Value : search.Trim();
            cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (page - 1) * pageSize;
            cmd.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync()) items.Add(Map(reader));
        }

        return new PagedResult<Patient> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    public async Task<List<Patient>> GetAllAsync()
    {
        var items = new List<Patient>();
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();
        await using var cmd = new SqlCommand("SELECT PatientID, FirstName, LastName, DateOfBirth, Gender, ContactNumber, Email, Address, BloodGroup, RegistrationDate FROM Patients ORDER BY FirstName, LastName;", connection);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) items.Add(Map(reader));
        return items;
    }

    public async Task<Patient?> GetByIdAsync(int id)
    {
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();
        await using var cmd = new SqlCommand("SELECT PatientID, FirstName, LastName, DateOfBirth, Gender, ContactNumber, Email, Address, BloodGroup, RegistrationDate FROM Patients WHERE PatientID=@Id;", connection);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<int> CreateAsync(Patient patient)
    {
        const string sql = @"INSERT INTO Patients (FirstName, LastName, DateOfBirth, Gender, ContactNumber, Email, Address, BloodGroup)
                             VALUES (@FirstName,@LastName,@DateOfBirth,@Gender,@ContactNumber,@Email,@Address,@BloodGroup);
                             SELECT CAST(SCOPE_IDENTITY() AS INT);";
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection);
        AddParameters(cmd, patient);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    public async Task<bool> UpdateAsync(Patient patient)
    {
        const string sql = @"UPDATE Patients SET FirstName=@FirstName, LastName=@LastName, DateOfBirth=@DateOfBirth, Gender=@Gender,
                             ContactNumber=@ContactNumber, Email=@Email, Address=@Address, BloodGroup=@BloodGroup WHERE PatientID=@PatientID;";
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection);
        AddParameters(cmd, patient);
        cmd.Parameters.Add("@PatientID", SqlDbType.Int).Value = patient.PatientID;
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();
        await using var cmd = new SqlCommand("DELETE FROM Patients WHERE PatientID=@Id;", connection);
        cmd.Parameters.Add("@Id", SqlDbType.Int).Value = id;
        try { return await cmd.ExecuteNonQueryAsync() > 0; }
        catch (SqlException ex) when (ex.Number == 547) { return false; }
    }

    public async Task<List<PatientSearchResult>> SearchAsync(string query)
    {
        var results = new List<PatientSearchResult>();
        const string sql = @"SELECT TOP 100 PatientID, FirstName + ' ' + LastName AS FullName, DateOfBirth, ContactNumber, Email, BloodGroup
                             FROM Patients
                             WHERE FirstName LIKE '%' + @Q + '%' OR LastName LIKE '%' + @Q + '%' OR ContactNumber LIKE '%' + @Q + '%'
                                   OR CAST(PatientID AS NVARCHAR(20)) = @Q
                             ORDER BY FirstName, LastName;";
        await using var connection = _factory.CreateConnection();
        await connection.OpenAsync();
        await using var cmd = new SqlCommand(sql, connection);
        cmd.Parameters.Add("@Q", SqlDbType.NVarChar, 100).Value = query.Trim();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new PatientSearchResult
            {
                PatientID = reader.GetInt32(0),
                FullName = reader.GetString(1),
                DateOfBirth = reader.GetDateTime(2),
                ContactNumber = reader.GetString(3),
                Email = reader.IsDBNull(4) ? null : reader.GetString(4),
                BloodGroup = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return results;
    }

    private static void AddParameters(SqlCommand cmd, Patient p)
    {
        cmd.Parameters.Add("@FirstName", SqlDbType.NVarChar, 50).Value = p.FirstName.Trim();
        cmd.Parameters.Add("@LastName", SqlDbType.NVarChar, 50).Value = p.LastName.Trim();
        cmd.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = p.DateOfBirth.Date;
        cmd.Parameters.Add("@Gender", SqlDbType.NVarChar, 10).Value = p.Gender;
        cmd.Parameters.Add("@ContactNumber", SqlDbType.NVarChar, 15).Value = p.ContactNumber.Trim();
        cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 100).Value = (object?)p.Email?.Trim() ?? DBNull.Value;
        cmd.Parameters.Add("@Address", SqlDbType.NVarChar, 200).Value = (object?)p.Address?.Trim() ?? DBNull.Value;
        cmd.Parameters.Add("@BloodGroup", SqlDbType.NVarChar, 5).Value = (object?)p.BloodGroup?.Trim() ?? DBNull.Value;
    }

    private static Patient Map(SqlDataReader r) => new()
    {
        PatientID = r.GetInt32(0), FirstName = r.GetString(1), LastName = r.GetString(2), DateOfBirth = r.GetDateTime(3),
        Gender = r.GetString(4), ContactNumber = r.GetString(5), Email = r.IsDBNull(6) ? null : r.GetString(6),
        Address = r.IsDBNull(7) ? null : r.GetString(7), BloodGroup = r.IsDBNull(8) ? null : r.GetString(8), RegistrationDate = r.GetDateTime(9)
    };
}
