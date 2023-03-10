using System.Numerics;

using Darkages.Enums;
using Darkages.Models;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;

using ServiceStack;

namespace Darkages.Database;

public class DatabaseLoad<T> where T : Template, new()
{
    private readonly string _storagePath;
    private const string ConnectionString = "Data Source=.;Initial Catalog=Zolian;Integrated Security=True;Encrypt=False";

    public DatabaseLoad()
    {
        var tmp = new T();
        _storagePath = $@"{ServerSetup.Instance.StoragePath}\templates";
        _storagePath = Path.Combine(_storagePath, "%");

        switch (tmp)
        {
            case WorldMapTemplate:
                // Z:\Zolian\Data\templates\WorldMaps
                _storagePath = _storagePath.Replace("%", "WorldMaps");
                break;
        }

        _storagePath = _storagePath.ToLower();
    }

    public void CacheFromStorage()
    {
        var tmp = new T();
        var assetNames = Directory.GetFiles(_storagePath, "*.json", SearchOption.TopDirectoryOnly);

        if (assetNames.Length == 0) return;

        foreach (var obj in assetNames)
        {
            switch (tmp)
            {
                case WorldMapTemplate _:
                {
                    var template = Load<WorldMapTemplate>(Path.GetFileNameWithoutExtension(obj));
                    if (template != null)
                        ServerSetup.Instance.GlobalWorldMapTemplateCache[template.FieldNumber] = template;
                    break;
                }
            }
        }
    }

    private TD Load<TD>(string name, string fixedPath = null) where TD : class, new()
    {
        var path = fixedPath ?? Path.Combine(_storagePath, $"{name.ToLower()}.json");

        if (!File.Exists(path)) return null;

        using var openStream = File.OpenRead(path);
        using var s = File.OpenRead(path);
        using var f = new StreamReader(s);
        var content = f.ReadToEnd();
        var obj = StorageManager.Deserialize<TD>(content);

        return obj;
    }

    public void CacheFromDatabase(Template temp)
    {
        switch (temp)
        {
            case NationTemplate _:
                Nations(ConnectionString);
                break;
            case WarpTemplate _:
                Warps(ConnectionString);
                break;
            case ItemTemplate _:
                Items(ConnectionString);
                break;
            case MonsterTemplate _:
                Monsters(ConnectionString);
                break;
            case MundaneTemplate _:
                Mundanes(ConnectionString);
                break;
            case SkillTemplate _:
                Abilities(ConnectionString, 1);
                break;
            case SpellTemplate _:
                Abilities(ConnectionString, 2);
                break;
        }
    }

    private static void Nations(string conn)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            const string sql = "SELECT * FROM ZolianMaps.dbo.Nations";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var tempX = (int)reader["MapPositionX"];
                var tempY = (int)reader["MapPositionY"];
                var natId = (int)reader["NationId"];
                var pos = new Position { X = (ushort)tempX, Y = (ushort)tempY };
                var temp = new NationTemplate
                {
                    AreaId = (int)reader["AreaId"],
                    MapPosition = pos,
                    NationId = (byte)natId,
                    Name = reader["Name"].ToString()
                };

                ServerSetup.Instance.GlobalNationTemplateCache[temp.Name!] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
        }

        ServerSetup.Logger($"Nation Templates: {ServerSetup.Instance.GlobalNationTemplateCache.Count.ToString()}");
    }

    private static void Warps(string conn)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            const string sql = "SELECT * FROM ZolianMaps.dbo.Warps";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();
                
            while (reader.Read())
            {
                var activationsList = new List<Warp>();
                var temp = new WarpTemplate();
                var to = new Warp();
                var warp1 = new Warp();
                var warp2 = new Warp();
                var warp3 = new Warp();
                var warp4 = new Warp();
                var warp5 = new Warp();
                var warp6 = new Warp();
                var warp7 = new Warp();
                var id = (int)reader["Id"];
                var currentArea = (int)reader["CurrentArea"];
                var nextArea = (int)reader["NextArea"];
                var caX1 = (int)reader["CALocationX_1"];
                var caY1 = (int)reader["CALocationY_1"];
                var ca1 = new Position(caX1, caY1);
                var caX2 = (int)reader["CALocationX_2"];
                var caY2 = (int)reader["CALocationY_2"];
                var ca2 = new Position(caX2, caY2);
                var caX3 = (int)reader["CALocationX_3"];
                var caY3 = (int)reader["CALocationY_3"];
                var ca3 = new Position(caX3, caY3);
                var caX4 = (int)reader["CALocationX_4"];
                var caY4 = (int)reader["CALocationY_4"];
                var ca4 = new Position(caX4, caY4);
                var caX5 = (int)reader["CALocationX_5"];
                var caY5 = (int)reader["CALocationY_5"];
                var ca5 = new Position(caX5, caY5);
                var caX6 = (int)reader["CALocationX_6"];
                var caY6 = (int)reader["CALocationY_6"];
                var ca6 = new Position(caX6, caY6);
                var caX7 = (int)reader["CALocationX_7"];
                var caY7 = (int)reader["CALocationY_7"];
                var ca7 = new Position(caX7, caY7);
                var naX1 = (int)reader["NALocationX_1"];
                var naY1 = (int)reader["NALocationY_1"];
                var na1 = new Position(naX1, naY1);
                var level = (int)reader["LevelRequired"];
                var warpType = reader["WarpType"].ConvertTo<WarpType>();
                var portalKey = (int)reader["PortalKey"];
                var positionCheck = new Vector2(0, 0);

                temp.ActivationMapId = currentArea;
                temp.WarpType = warpType;
                temp.LevelRequired = level;

                #region Activations

                warp1.AreaID = currentArea;
                warp1.PortalKey = portalKey;
                warp1.Location = ca1;
                activationsList.Add(warp1);

                var vect2 = new Vector2(caX2, caY2);
                if (!vect2.Equals(positionCheck))
                {
                    warp2.AreaID = currentArea;
                    warp2.PortalKey = portalKey;
                    warp2.Location = ca2;
                    activationsList.Add(warp2);
                }

                var vect3 = new Vector2(caX3, caY3);
                if (!vect3.Equals(positionCheck))
                {
                    warp3.AreaID = currentArea;
                    warp3.PortalKey = portalKey;
                    warp3.Location = ca3;
                    activationsList.Add(warp3);
                }

                var vect4 = new Vector2(caX4, caY4);
                if (!vect4.Equals(positionCheck))
                {
                    warp4.AreaID = currentArea;
                    warp4.PortalKey = portalKey;
                    warp4.Location = ca4;
                    activationsList.Add(warp4);
                }

                var vect5 = new Vector2(caX5, caY5);
                if (!vect5.Equals(positionCheck))
                {
                    warp5.AreaID = currentArea;
                    warp5.PortalKey = portalKey;
                    warp5.Location = ca5;
                    activationsList.Add(warp5);
                }

                var vect6 = new Vector2(caX6, caY6);
                if (!vect6.Equals(positionCheck))
                {
                    warp6.AreaID = currentArea;
                    warp6.PortalKey = portalKey;
                    warp6.Location = ca6;
                    activationsList.Add(warp6);
                }

                var vect7 = new Vector2(caX7, caY7);
                if (!vect7.Equals(positionCheck))
                {
                    warp7.AreaID = currentArea;
                    warp7.PortalKey = portalKey;
                    warp7.Location = ca7;
                    activationsList.Add(warp7);
                }

                to.AreaID = nextArea;
                to.PortalKey = portalKey;
                to.Location = na1;

                #endregion

                temp.Activations = activationsList;
                temp.To = to;
                ServerSetup.Instance.GlobalWarpTemplateCache[id!] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.Logger(e.ToString());
        }

        ServerSetup.Logger($"Warp Templates: {ServerSetup.Instance.GlobalWarpTemplateCache.Count.ToString()}");
    }

    private static void Items(string conn)
    {
        try
        {
            string[] dbTables = { "Consumables", "EnemyDrops", "Gems", "Potions", "Quest", "Scrolls" };
            string[] dbTables1 = { "ArmorArcanus", "ArmorAssassin", "ArmorCleric", "ArmorMonk", "ArmorDefender", "ArmorBerserker", "ArmorGeneric" };
            string[] dbTables2 = { "Belts", "Boots", "Earrings", "Hands", "Greaves", "Necklaces", "Rings" };
            string[] dbTables3 = { "WeaponPeasant", "WeaponBerserker", "WeaponDefender", "WeaponAssassin", "WeaponCleric", "WeaponArcanus", "WeaponMonk", "Shields", "Sources" };

            foreach (var table in dbTables)
            {
                ItemStorage.CacheFromDatabaseConsumables(conn, table);
            }

            foreach (var table in dbTables1)
            {
                ItemStorage.CacheFromDatabaseArmor(conn, table);
            }

            foreach (var table in dbTables2)
            {
                ItemStorage.CacheFromDatabaseEquipment(conn, table);
            }

            foreach (var table in dbTables3)
            {
                ItemStorage.CacheFromDatabaseOffense(conn, table);
            }
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }

        ServerSetup.Logger($"Item Templates: {ServerSetup.Instance.GlobalItemTemplateCache.Count.ToString()}");
    }

    private static void Monsters(string conn)
    {
        try
        {
            string[] dbTables = { "EasternWoods", "WesternWoodlands", "EnchantedGarden", "GMIsland", "Mileth", "Abel", "Piet", "Tutorial", "Wastelands", "Tagor" };

            foreach (var table in dbTables)
            {
                MonsterStorage.CacheFromDatabase(conn, table);
            }
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }

        ServerSetup.Logger($"Monster Templates: {ServerSetup.Instance.GlobalMonsterTemplateCache.Count.ToString()}");
    }

    private static void Mundanes(string conn)
    {
        try
        {
            string[] dbTables = { "Mileth", "Abel", "Arena", "Piet", "Rucesion", "Generic", "TempleofLight", "Tutorial", "Tagor", "Mehadi", "Rionnag" };

            foreach (var table in dbTables)
            {
                MundaneStorage.CacheFromDatabase(conn, table);
            }
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }

        ServerSetup.Logger($"Mundane Templates: {ServerSetup.Instance.GlobalMundaneTemplateCache.Count.ToString()}");
    }

    private static void Abilities(string conn, int num)
    {
        try
        {
            if (num == 1)
            {
                SkillStorage.CacheFromDatabase(conn);
                ServerSetup.Logger($"Skill Templates: {ServerSetup.Instance.GlobalSkillTemplateCache.Count.ToString()}");
            }
            else
            {
                SpellStorage.CacheFromDatabase(conn);
                ServerSetup.Logger($"Spell Templates: {ServerSetup.Instance.GlobalSpellTemplateCache.Count.ToString()}");
            }
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.ToString());
            Crashes.TrackError(e);
        }
    }
}