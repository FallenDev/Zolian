using Microsoft.Data.SqlClient;

using System.Data;

namespace Darkages.Database;

public abstract record Sql
{
    protected static SqlConnection ConnectToDatabase(string conn)
    {
        var sConn = new SqlConnection(conn);
        sConn.Open();
        return sConn;
    }

    protected static SqlCommand ConnectToDatabaseSqlCommandWithProcedure(string command, SqlConnection conn)
    {
        return new SqlCommand(command, conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 5
        };
    }

    protected static void ExecuteAndCloseConnection(SqlCommand command, SqlConnection conn)
    {
        command.ExecuteNonQuery();
        conn.Close();
    }
}
