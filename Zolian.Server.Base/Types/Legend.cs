using Chaos.Common.Identity;

using Dapper;

using Darkages.Database;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using ServiceStack;

using System.Data;

namespace Darkages.Types;

public class Legend
{
    public readonly List<LegendItem> LegendMarks = new();

    public void AddLegend(LegendItem legend, WorldClient client)
    {
        if (legend == null) return;
        if (client.Aisling == null) return;
        if (LegendMarks.Contains(legend)) return;
        LegendMarks.Add(legend);
        AddToAislingDb(client.Aisling, legend);
    }

    public bool Has(string lpVal)
    {
        return LegendMarks.Any(i => i.Text != null && i.Text.Equals(lpVal));
    }

    public void Remove(LegendItem legend, WorldClient client)
    {
        if (legend == null) return;
        if (client.Aisling == null) return;
        LegendMarks.Remove(legend);
        DeleteFromAislingDb(legend);
    }

    public class LegendItem
    {
        public uint LegendId { get; init; }
        public string Key { get; init; }
        public DateTime Time { get; init; }
        public LegendColor Color { get; init; }
        public byte Icon { get; init; }
        public string Text { get; init; }
    }

    private static void AddToAislingDb(Aisling aisling, LegendItem legend)
    {
        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("AddLegendMark", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            var legendId = EphemeralRandomIdGenerator<uint>.Shared.NextId;

            cmd.Parameters.Add("@LegendId", SqlDbType.Int).Value = legendId;
            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
            cmd.Parameters.Add("@Key", SqlDbType.VarChar).Value = legend.Key;
            cmd.Parameters.Add("@Time", SqlDbType.DateTime).Value = legend.Time;
            cmd.Parameters.Add("@Color", SqlDbType.VarChar).Value = legend.Color;
            cmd.Parameters.Add("@Icon", SqlDbType.Int).Value = legend.Icon;
            if (!legend.Text.IsNullOrEmpty())
                cmd.Parameters.Add("@Text", SqlDbType.VarChar).Value = legend.Text;
            else
                cmd.Parameters.Add("@Text", SqlDbType.VarChar).Value = DBNull.Value;
            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }

    private static void DeleteFromAislingDb(LegendItem legend)
    {
        if (legend.LegendId == 0) return;

        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersLegend WHERE LegendId = @LegendId";
            sConn.Execute(cmd, new { legend.LegendId });
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }
}