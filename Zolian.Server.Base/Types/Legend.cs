using Dapper;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Data;

namespace Darkages.Types;

public class Legend
{
    public readonly List<LegendItem> LegendMarks = new();

    public void AddLegend(LegendItem legend, GameClient client)
    {
        if (legend == null) return;
        if (client.Aisling == null) return;
        if (LegendMarks.Contains(legend)) return;
        LegendMarks.Add(legend);
        AddToAislingDb(client.Aisling, legend);
    }

    public bool Has(string lpVal)
    {
        return LegendMarks.Any(i => i.Value.Equals(lpVal));
    }

    public void Remove(LegendItem legend, GameClient client)
    {
        if (legend == null) return;
        if (client.Aisling == null) return;
        LegendMarks.Remove(legend);
        DeleteFromAislingDb(legend);
    }

    public class LegendItem
    {
        public int LegendId { get; init; }
        public string Category { get; init; }
        public DateTime Time { get; init; }
        public LegendColor Color { get; init; }
        public byte Icon { get; init; }
        public string Value { get; init; }
    }

    private static async void AddToAislingDb(Aisling aisling, LegendItem legend)
    {
        try
        {
            await using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("AddLegendMark", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            var legendId = EphemeralRandomIdGenerator<uint>.Shared.NextId;

            cmd.Parameters.Add("@LegendId", SqlDbType.Int).Value = legendId;
            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
            cmd.Parameters.Add("@Category", SqlDbType.VarChar).Value = legend.Category;
            cmd.Parameters.Add("@Time", SqlDbType.DateTime).Value = legend.Time;
            cmd.Parameters.Add("@Color", SqlDbType.VarChar).Value = legend.Color;
            cmd.Parameters.Add("@Icon", SqlDbType.Int).Value = legend.Icon;
            cmd.Parameters.Add("@Value", SqlDbType.VarChar).Value = legend.Value;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                aisling.aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue saving legend mark. Contact GM");
                Crashes.TrackError(e);
                return;
            }

            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }

    private static async void DeleteFromAislingDb(LegendItem legend)
    {
        if (legend.LegendId == 0) return;

        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersLegend WHERE LegendId = @LegendId";
            await sConn.ExecuteAsync(cmd, new { legend.LegendId });
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }
}