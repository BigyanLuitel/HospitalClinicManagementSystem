using Microsoft.Data.SqlClient;

namespace HospitalClinicManagementSystem.Data;

public interface IDbConnectionFactory
{
    SqlConnection CreateConnection();
}
