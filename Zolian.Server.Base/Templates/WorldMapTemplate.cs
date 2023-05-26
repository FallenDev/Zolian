using Darkages.Models;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

using Microsoft.Data.SqlClient;

namespace Darkages.Templates;

public class WorldMapTemplate : Template
{
    public List<WorldPortal> Portals { get; set; }
    public int FieldNumber { get; set; }
}

public static class WorldMapStorage
{
    public static void CacheFromDatabase(string conn, string type)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            var sql = $"SELECT * FROM ZolianWorldMaps.dbo.{type}";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();
            var temp = new WorldMapTemplate
            {
                Portals = new List<WorldPortal>()
            };

            while (reader.Read())
            {
                var displayName = reader["DisplayName"].ToString();
                var worldX = (int)reader["WorldX"];
                var worldY = (int)reader["WorldY"];
                var areaId = (int)reader["AreaId"];
                var areaX = (int)reader["AreaX"];
                var areaY = (int)reader["AreaY"];
                const int portalKey = 0;
                var fieldNumber = (int)reader["FieldNumber"];
                
                var destination = new Warp
                {
                    AreaID = areaId,
                    Location = new Position(areaX, areaY),
                    PortalKey = portalKey
                };
                var worldPortal = new WorldPortal
                {
                     Destination = destination,
                     DisplayName = displayName,
                     PointX = (short)worldX,
                     PointY = (short)worldY
                };

                temp.FieldNumber = fieldNumber;
                temp.Portals.Add(worldPortal);
            }
            
            ServerSetup.Instance.GlobalWorldMapTemplateCache[temp.FieldNumber] = temp;

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }
}
