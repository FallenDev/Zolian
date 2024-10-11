using System.Collections.Frozen;

using Chaos.Cryptography;

using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Types;

using Microsoft.Data.SqlClient;
using MapFlags = Darkages.Enums.MapFlags;

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
                var wildFlowers = ServiceStack.AutoMappingUtils.ConvertTo<WildFlowers>(reader["WildFlowers"]);
                var height = (int)reader["Height"];
                var width = (int)reader["Width"];
                var temp = new Area
                {
                    ID = (int)reader["Id"],
                    Flags = flags,
                    Music = (int)reader["Music"],
                    Height = (byte)height,
                    Width = (byte)width,
                    ScriptKey = reader["ScriptKey"].ToString(),
                    MiningNodes = miningNodes,
                    WildFlowers = wildFlowers,
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
        CreateMapCacheContainers();
    }

    public bool LoadMap(Area mapObj, string mapFile)
    {
        mapObj.FilePath = mapFile;
        mapObj.Data = File.ReadAllBytes(mapFile);
        mapObj.Hash = Crc.Generate16(mapObj.Data);

        return mapObj.OnLoaded();
    }

    public void CreateMapCacheContainers()
    {
        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
        {
            map.SpriteCollections.TryAdd(Tuple.Create(map.ID, typeof(Monster)), new SpriteCollection<Monster>());
            map.SpriteCollections.TryAdd(Tuple.Create(map.ID, typeof(Aisling)), new SpriteCollection<Aisling>());
            map.SpriteCollections.TryAdd(Tuple.Create(map.ID, typeof(Mundane)), new SpriteCollection<Mundane>());
            map.SpriteCollections.TryAdd(Tuple.Create(map.ID, typeof(Item)), new SpriteCollection<Item>());
            map.SpriteCollections.TryAdd(Tuple.Create(map.ID, typeof(Money)), new SpriteCollection<Money>());
        }
    }
}