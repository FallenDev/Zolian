using System.Data;
using Darkages.Interfaces;
using Microsoft.Data.SqlClient;

namespace Darkages.Database;

public abstract record Sql : ISql
{
    public SqlConnection ConnectToDatabase(string conn)
    {
        var sConn = new SqlConnection(conn);
        sConn.Open();
        return sConn;
    }

    public SqlCommand ConnectToDatabaseSqlCommandWithProcedure(string command, SqlConnection conn)
    {
        return new SqlCommand(command, conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 5
        };
    }

    public void ExecuteAndCloseConnection(SqlCommand command, SqlConnection conn)
    {
        command.ExecuteNonQuery();
        conn.Close();
    }
}
