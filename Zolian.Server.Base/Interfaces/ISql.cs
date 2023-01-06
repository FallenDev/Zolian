using Microsoft.Data.SqlClient;

namespace Darkages.Interfaces;

public interface ISql
{
    SqlConnection ConnectToDatabase(string conn);
    SqlCommand ConnectToDatabaseSqlCommand(string command, SqlConnection conn);
    void ExecuteAndCloseConnection(SqlCommand command, SqlConnection conn);
}
