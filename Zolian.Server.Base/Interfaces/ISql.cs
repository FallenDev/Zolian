using Microsoft.Data.SqlClient;

namespace Darkages.Interfaces;

public interface ISql
{
    SqlConnection ConnectToDatabase(string conn);
    SqlCommand ConnectToDatabaseSqlCommandWithProcedure(string command, SqlConnection conn);
    void ExecuteAndCloseConnection(SqlCommand command, SqlConnection conn);
}
