using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Network.Security;
using Darkages.Scripting;
using Darkages.Types;

using Microsoft.Data.SqlClient;

namespace Darkages.Database;

public record AreaStorage : IAreaStorage
{
    private AreaStorage() { }

    public static AreaStorage Instance { get; } = new();

    private SemaphoreSlim LoadLock { get; } = new(1, 1);

    public async void CacheFromDatabase()
    {
        await LoadLock.WaitAsync().ConfigureAwait(false);

        try
        {
            const string conn = "Data Source=.;Initial Catalog=Zolian;Integrated Security=True;Encrypt=False";
            var sConn = new SqlConnection(conn);
            const string sql = "SELECT * FROM ZolianMaps.dbo.Maps";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var flags = ServiceStack.AutoMappingUtils.ConvertTo<MapFlags>(reader["Flags"]);
                var rows = (int)reader["mRows"];
                var cols = (int)reader["mCols"];
                var temp = new Area
                {
                    ID = (int)reader["MapId"],
                    Flags = flags,
                    Music = (int)reader["Music"],
                    Rows = (ushort)rows,
                    Cols = (ushort)cols,
                    ScriptKey = reader["ScriptKey"].ToString(),
                    Name = reader["Name"].ToString()
                };

                var mapFile = Directory.GetFiles($@"{ServerSetup.Instance.StoragePath}\maps", $"lod{temp.ID}.map", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (!(mapFile != null && File.Exists(mapFile))) continue;

                if (!LoadMap(temp, mapFile))
                {
                    ServerSetup.Logger($"Map Load Unsuccessful: {temp.ID}_{temp.Name}");
                    continue;
                }

                if (!string.IsNullOrEmpty(temp.ScriptKey))
                {
                    temp.Scripts = ScriptManager.Load<AreaScript>(temp.ScriptKey, temp);
                }

                ServerSetup.Instance.GlobalMapCache[temp.ID] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
        }
        finally
        {
            LoadLock.Release();
        }

        ServerSetup.Logger($"Maps: {ServerSetup.Instance.GlobalMapCache.Count}");
    }

    public bool LoadMap(Area mapObj, string mapFile)
    {
        mapObj.FilePath = mapFile;
        mapObj.Data = File.ReadAllBytes(mapFile);
        mapObj.Hash = Crc16Provider.Generate16(mapObj.Data);

        return mapObj.OnLoaded();
    }
}