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

    public SqlCommand ConnectToDatabaseSqlCommand(string command, SqlConnection conn)
    {
        var sConn = new SqlCommand(command, conn);
        sConn.CommandType = CommandType.StoredProcedure;
        sConn.CommandTimeout = 5;
        return sConn;
    }

    public void ExecuteAndCloseConnection(SqlCommand command, SqlConnection conn)
    {
        command.ExecuteNonQuery();
        conn.Close();
    }
}
