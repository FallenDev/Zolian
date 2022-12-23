using Darkages.Enums;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;

using ServiceStack;

namespace Darkages.Templates
{
    public class MonsterTemplate : Template
    {
        public readonly List<string> Drops = new();
        public int AreaID { get; set; }
        public int AttackSpeed { get; set; }
        public string BaseName { get; set; }
        public int CastSpeed { get; set; }
        public ElementManager.Element DefenseElement { get; set; }
        public ushort DefinedX { get; set; }
        public ushort DefinedY { get; set; }
        public ElementQualifer ElementType { get; set; }
        public int EngagedWalkingSpeed { get; set; }
        public bool IgnoreCollision { get; set; }
        public ushort Image { get; set; }
        public int ImageVarience { get; set; }
        public uint Level { get; set; }
        public LootQualifer LootType { get; set; }
        public MonsterRace MonsterRace { get; set; }
        public MoodQualifer MoodType { get; set; }
        public int MovementSpeed { get; set; }
        public DateTime NextAvailableSpawn { get; set; }
        public ElementManager.Element OffenseElement { get; set; }
        public PathQualifer PathQualifer { get; set; }
        public string ScriptName { get; set; }
        public List<string> SkillScripts { get; set; }
        public int SpawnMax { get; set; }
        public int SpawnRate { get; set; }
        public int SpawnSize { get; set; }
        public SpawnQualifer SpawnType { get; set; }
        public MonsterType MonsterType { get; set; }
        public List<string> SpellScripts { get; set; }
        public List<string> AbilityScripts { get; set; }
        public List<Position> Waypoints { get; set; }
        private bool Ready
        {
            get
            {
                var readyTime = DateTime.Now;
                return readyTime > NextAvailableSpawn;
            }
        }

        public bool ReadyToSpawn()
        {
            if (!Ready) return false;
            var readyTime = DateTime.Now;
            NextAvailableSpawn = readyTime.AddSeconds(SpawnRate);
            return true;
        }
    }

    public static class MonsterStorage
    {
        public static void CacheFromDatabase(string conn, string type)
        {
            try
            {
                var sConn = new SqlConnection(conn);
                var sql = $"SELECT * FROM ZolianMonsters.dbo.{type}";

                sConn.Open();

                var cmd = new SqlCommand(sql, sConn);
                cmd.CommandTimeout = 5;

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var temp = new MonsterTemplate();
                    var drop1 = reader["DropOne"].ToString();
                    var drop2 = reader["DropTwo"].ToString();
                    var drop3 = reader["DropThree"].ToString();
                    var drop4 = reader["DropFour"].ToString();
                    var image = (int)reader["Image"];
                    var x = (int)reader["DefinedX"];
                    var y = (int)reader["DefinedY"];
                    var eleType = reader["ElementType"].ConvertTo<ElementQualifer>();
                    var monsterType = reader["Type"].ConvertTo<MonsterType>();
                    var monsterRace = reader["MonType"].ConvertTo<MonsterRace>();
                    var pathFind = reader["PathFinder"].ConvertTo<PathQualifer>();
                    var spawnType = reader["SpawnType"].ConvertTo<SpawnQualifer>();
                    var mood = reader["Mood"].ConvertTo<MoodQualifer>();
                    var way1 = WayPointConvert(reader["WayPointOne"].ToString());
                    var way2 = WayPointConvert(reader["WayPointTwo"].ToString());
                    var way3 = WayPointConvert(reader["WayPointThree"].ToString());
                    var way4 = WayPointConvert(reader["WayPointFour"].ToString());
                    var way5 = WayPointConvert(reader["WayPointFive"].ToString());
                    var loot = reader["LootType"].ConvertTo<LootQualifer>();
                    var offEle = reader["OffenseElement"].ConvertTo<ElementManager.Element>();
                    var defEle = reader["DefenseElement"].ConvertTo<ElementManager.Element>();
                    var skill1 = reader["AssailOne"].ToString();
                    var skill2 = reader["AssailTwo"].ToString();
                    var skill3 = reader["AssailThree"].ToString();
                    var skill4 = reader["AssailFour"].ToString();
                    var skill5 = reader["AssailFive"].ToString();
                    var spell1 = reader["SpellOne"].ToString();
                    var spell2 = reader["SpellTwo"].ToString();
                    var spell3 = reader["SpellThree"].ToString();
                    var spell4 = reader["SpellFour"].ToString();
                    var spell5 = reader["SpellFive"].ToString();
                    var ability1 = reader["AbilityOne"].ToString();
                    var ability2 = reader["AbilityTwo"].ToString();
                    var ability3 = reader["AbilityThree"].ToString();
                    var ability4 = reader["AbilityFour"].ToString();
                    var ability5 = reader["AbilityFive"].ToString();

                    if (drop1 != null)
                        temp.Drops.Add(drop1);
                    if (drop2 != null)
                        temp.Drops.Add(drop2);
                    if (drop3 != null)
                        temp.Drops.Add(drop3);
                    if (drop4 != null)
                        temp.Drops.Add(drop4);
                    temp.ScriptName = reader["ScriptName"].ToString();
                    temp.BaseName = reader["BaseName"].ToString();
                    temp.Name = reader["Name"].ToString();
                    temp.AreaID = (int)reader["AreaId"];
                    temp.Image = (ushort)image;
                    temp.ImageVarience = (int)reader["ImageVariance"];
                    temp.DefinedX = (ushort)x;
                    temp.DefinedY = (ushort)y;
                    temp.ElementType = eleType;
                    temp.PathQualifer = pathFind;
                    temp.SpawnType = spawnType;
                    temp.SpawnSize = (int)reader["SpawnSize"];
                    temp.SpawnMax = (int)reader["SpawnMax"];
                    temp.SpawnRate = (int)reader["SpawnRate"];
                    temp.MoodType = mood;
                    temp.MonsterType = monsterType;
                    temp.MonsterRace = monsterRace;
                    temp.IgnoreCollision = (bool)reader["IgnoreCollision"];
                    temp.MovementSpeed = (int)reader["MovementSpeed"];
                    temp.EngagedWalkingSpeed = (int)reader["EngagedSpeed"];
                    temp.Waypoints = new List<Position>();
                    if (way1 != null)
                        temp.Waypoints.Add(way1);
                    if (way2 != null)
                        temp.Waypoints.Add(way2);
                    if (way3 != null)
                        temp.Waypoints.Add(way3);
                    if (way4 != null)
                        temp.Waypoints.Add(way4);
                    if (way5 != null)
                        temp.Waypoints.Add(way5);
                    temp.AttackSpeed = (int)reader["AttackSpeed"];
                    temp.CastSpeed = (int)reader["CastSpeed"];
                    temp.LootType = loot;
                    temp.OffenseElement = offEle;
                    temp.DefenseElement = defEle;
                    var level = (int)reader["Level"];
                    temp.Level = (uint)level;
                    temp.SkillScripts = new List<string>();
                    temp.SpellScripts = new List<string>();
                    temp.AbilityScripts = new List<string>();
                    if (skill1 != null)
                        temp.SkillScripts.Add(skill1);
                    if (skill2 != null)
                        temp.SkillScripts.Add(skill2);
                    if (skill3 != null)
                        temp.SkillScripts.Add(skill3);
                    if (skill4 != null)
                        temp.SkillScripts.Add(skill4);
                    if (skill5 != null)
                        temp.SkillScripts.Add(skill5);
                    if (spell1 != null)
                        temp.SpellScripts.Add(spell1);
                    if (spell2 != null)
                        temp.SpellScripts.Add(spell2);
                    if (spell3 != null)
                        temp.SpellScripts.Add(spell3);
                    if (spell4 != null)
                        temp.SpellScripts.Add(spell4);
                    if (spell5 != null)
                        temp.SpellScripts.Add(spell5);
                    if (ability1 != null)
                        temp.AbilityScripts.Add(ability1);
                    if (ability2 != null)
                        temp.AbilityScripts.Add(ability2);
                    if (ability3 != null)
                        temp.AbilityScripts.Add(ability3);
                    if (ability4 != null)
                        temp.AbilityScripts.Add(ability4);
                    if (ability5 != null)
                        temp.AbilityScripts.Add(ability5);

                    if (temp.Name == null) continue;
                    ServerSetup.Instance.GlobalMonsterTemplateCache[temp.Name] = temp;
                }

                reader.Close();
                sConn.Close();
            }
            catch (SqlException e)
            {
                ServerSetup.Logger(e.ToString());
                Crashes.TrackError(e);
            }
        }

        private static Position WayPointConvert(string wayPointString)
        {
            if (wayPointString.IsNullOrEmpty()) return null;
            const char delim = ',';
            var cords = wayPointString.Split(delim);
            var x = 0;
            var y = 0;

            if (cords.Length == 0) return new Position(0, 0);

            foreach (var cord in cords)
            {
                if (cord == cords.FirstOrDefault())
                {
                    x = cord.ToInt();
                }

                if (cord == cords.LastOrDefault())
                {
                    y = cord.ToInt();
                }
            }

            return new Position(x, y);
        }
    }
}