using System.Collections.Frozen;

using Chaos.Cryptography;

using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.ScriptingBase;
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
        await LoadLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

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
                var miningNodes = ServiceStack.AutoMappingUtils.ConvertTo<MiningNodes>(reader["MiningNodes"]);
                var rows = (int)reader["mRows"];
                var cols = (int)reader["mCols"];
                var temp = new Area
                {
                    ID = (int)reader["MapId"],
                    Flags = flags,
                    Music = (int)reader["Music"],
                    Height = (ushort)rows,
                    Width = (ushort)cols,
                    ScriptKey = reader["ScriptKey"].ToString(),
                    MiningNodes = miningNodes,
                    Name = reader["Name"].ToString()
                };

                var mapFile = Directory.GetFiles($@"{ServerSetup.Instance.StoragePath}\maps", $"lod{temp.ID}.map", SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (!(mapFile != null && File.Exists(mapFile))) continue;

                if (!LoadMap(temp, mapFile))
                {
                    ServerSetup.EventsLogger($"Map Load Unsuccessful: {temp.ID}_{temp.Name}");
                    continue;
                }

                if (!string.IsNullOrEmpty(temp.ScriptKey))
                {
                    var scriptToType = ScriptManager.Load<AreaScript>(temp.ScriptKey, temp);
                    var scriptFoundGetValue = scriptToType.TryGetValue(temp.ScriptKey, out var script);
                    if (scriptFoundGetValue)
                        temp.Script = new Tuple<string, AreaScript>(temp.ScriptKey, script);
                }

                ServerSetup.Instance.TempGlobalMapCache[temp.ID] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
        }
        finally
        {
            LoadLock.Release();
        }

        ServerSetup.Instance.GlobalMapCache = ServerSetup.Instance.TempGlobalMapCache.ToFrozenDictionary();
        ServerSetup.EventsLogger($"Maps: {ServerSetup.Instance.GlobalMapCache.Count}");
    }

    public bool LoadMap(Area mapObj, string mapFile)
    {
        mapObj.FilePath = mapFile;
        mapObj.Data = File.ReadAllBytes(mapFile);
        mapObj.Hash = Crc.Generate16(mapObj.Data);

        return mapObj.OnLoaded();
    }
}