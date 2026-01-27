using System.Data;

using Microsoft.Data.SqlClient;

namespace Darkages.Database;

public abstract record Sql
{
    public const string ConnectionString = "Data Source=.;Initial Catalog=ZolianPlayers;Integrated Security=True;Encrypt=False;MultipleActiveResultSets=True;";
    public const string PersonalMailString = "Data Source=.;Initial Catalog=ZolianBoardsMail;Integrated Security=True;Encrypt=False;MultipleActiveResultSets=True;";
    public const string EncryptedConnectionString = "Data Source=.;Initial Catalog=ZolianPlayers;Integrated Security=True;Column Encryption Setting=enabled;TrustServerCertificate=True;MultipleActiveResultSets=True;";

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

    public static async Task<bool> ExecuteWithRetryAsync(Func<CancellationToken, Task> action, CancellationToken ct, int maxAttempts = 4, int baseDelayMs = 40)
    {
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await action(ct).ConfigureAwait(false);
                return true;
            }
            catch (SqlException ex) when (IsTransient(ex) && attempt < maxAttempts)
            {
                var delayMs = Math.Min(500, baseDelayMs * (1 << (attempt - 1)));
                // Randomized exponential backoff
                delayMs += Random.Shared.Next(0, 25);

                await Task.Delay(delayMs, ct).ConfigureAwait(false);
            }
        }

        return false;
    }

    private static bool IsTransient(SqlException ex)
    {
        foreach (SqlError err in ex.Errors)
        {
            // -2   = Command timeout
            // 1205 = Deadlock victim
            // 1222 = Lock request timeout
            if (err.Number is -2 or 1205 or 1222)
                return true;
        }

        return false;
    }
}
