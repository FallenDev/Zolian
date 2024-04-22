using Darkages.Enums;
using Darkages.Types;
using Microsoft.Data.SqlClient;

using ServiceStack;

namespace Darkages.Templates;

public class SkillTemplate : Template
{
    public string ScriptName { get; set; }
    public byte Icon { get; set; }
    public Pane Pane { get; set; }
    public int Cooldown { get; set; }
    public byte Sound { get; set; }
    public PostQualifier PostQualifiers { get; set; }
    public LearningPredicate Prerequisites { get; set; }
    public List<LearningPredicate> LearningRequirements { get; } = [];
    public ushort TargetAnimation { get; set; }
    public ushort MissAnimation { get; set; }
    public int MaxLevel { get; set; }
    public string FailMessage { get; set; }
    public string NpcKey { get; set; }
    public SkillScope SkillType { get; set; }

    public string[] GetMetaData()
    {
        return Prerequisites?.MetaData;
    }
}

public static class SkillStorage
{
    public static void CacheFromDatabase(string conn)
    {
        try
        {
            var sConn = new SqlConnection(conn);
            const string sql = "SELECT * FROM ZolianAbilities.dbo.Skills";

            sConn.Open();

            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;

            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var temp = new SkillTemplate();
                var icon = (int)reader["Icon"];
                var pane = reader["Pane"].ConvertTo<Pane>();
                var sound = (int)reader["Sound"];
                var post = reader["PostAttribute"].ConvertTo<PostQualifier>();
                var predicateId = (int)reader["PredicateId"];
                var targetAnim = (int)reader["TargetAnimation"];
                var missAnim = (int)reader["MissAnimation"];
                var skillType = reader["SkillScope"].ConvertTo<SkillScope>();

                temp.Icon = (byte)icon;
                temp.ScriptName = reader["ScriptName"].ToString();
                temp.Pane = pane;
                temp.Cooldown = (int)reader["Cooldown"];
                temp.Sound = (byte)sound;
                temp.PostQualifiers = post;

                #region LearningPredicate

                var sConn2 = new SqlConnection(conn);
                var sql2 = $"SELECT * FROM ZolianAbilities.dbo.SkillsPrerequisites WHERE PredicateId={predicateId.ToString()}";
                sConn2.Open();
                var cmd2 = new SqlCommand(sql2, sConn2);
                cmd2.CommandTimeout = 5;

                var reader2 = cmd2.ExecuteReader();

                while (reader2.Read())
                {
                    var learning = new LearningPredicate();
                    var item1 = new ItemPredicate
                    {
                        Item = reader2["Item1Name"].ToString(),
                        AmountRequired = (int)reader2["Item1Qty"]
                    };
                    var item2 = new ItemPredicate
                    {
                        Item = reader2["Item2Name"].ToString(),
                        AmountRequired = (int)reader2["Item2Qty"]
                    };
                    var item3 = new ItemPredicate
                    {
                        Item = reader2["Item3Name"].ToString(),
                        AmountRequired = (int)reader2["Item3Qty"]
                    };
                    var item4 = new ItemPredicate
                    {
                        Item = reader2["Item4Name"].ToString(),
                        AmountRequired = (int)reader2["Item4Qty"]
                    };
                    var item5 = new ItemPredicate
                    {
                        Item = reader2["Item5Name"].ToString(),
                        AmountRequired = (int)reader2["Item5Qty"]
                    };

                    var itemList = new List<ItemPredicate>();

                    if (item1.Item != null && item1.AmountRequired > 0)
                        itemList.Add(item1);
                    if (item2.Item != null && item2.AmountRequired > 0)
                        itemList.Add(item2);
                    if (item3.Item != null && item3.AmountRequired > 0)
                        itemList.Add(item3);
                    if (item4.Item != null && item4.AmountRequired > 0)
                        itemList.Add(item4);
                    if (item5.Item != null && item5.AmountRequired > 0)
                        itemList.Add(item5);

                    var primClass = reader2["PrimaryClass"].ConvertTo<Class>();
                    var secClass = reader2["SecondaryClass"].ConvertTo<Class>();
                    var race = reader2["Race"].ConvertTo<Race>();
                    var stage = reader2["Stage"].ConvertTo<ClassStage>();
                    var job = reader2["Job"].ConvertTo<Job>();
                    temp.Prerequisites = learning;
                    temp.Prerequisites.ItemsRequired = itemList;
                    temp.Prerequisites.DisplayName = reader2["DisplayName"].ToString();
                    temp.Name = temp.Prerequisites.DisplayName;
                    temp.Prerequisites.ClassRequired = primClass;
                    temp.Prerequisites.SecondaryClassRequired = secClass;
                    temp.Prerequisites.RaceRequired = race;
                    temp.Prerequisites.StrRequired = (int)reader2["Strength"];
                    temp.Prerequisites.IntRequired = (int)reader2["Intelligence"];
                    temp.Prerequisites.WisRequired = (int)reader2["Wisdom"];
                    temp.Prerequisites.ConRequired = (int)reader2["Constitution"];
                    temp.Prerequisites.DexRequired = (int)reader2["Dexterity"];
                    temp.Prerequisites.ExpLevelRequired = (int)reader2["Level"];
                    var exp = (long)reader2["Experience"];
                    temp.Prerequisites.ExperienceRequired = (uint)exp;
                    var gold = (int)reader2["Gold"];
                    temp.Prerequisites.GoldRequired = (uint)gold;
                    temp.Prerequisites.SkillLevelRequired = (int)reader2["SkillLevel"];
                    temp.Prerequisites.SkillRequired = reader2["SkillRequired"].ToString();
                    temp.Prerequisites.SpellLevelRequired = (int)reader2["SpellLevel"];
                    temp.Prerequisites.SpellRequired = reader2["SpellRequired"].ToString();
                    temp.Prerequisites.StageRequired = stage;
                    temp.Prerequisites.JobRequired = job;
                }

                reader2.Close();
                sConn2.Close();

                #endregion

                temp.TargetAnimation = (ushort)targetAnim;
                temp.MissAnimation = (ushort)missAnim;
                temp.MaxLevel = (int)reader["MaxLevel"];
                temp.FailMessage = reader["FailMsg"].ToString();
                temp.NpcKey = reader["NpcKey"].ToString();
                temp.SkillType = skillType;
                temp.Description = reader["Description"].ToString();
                temp.DamageMod = reader["DamageMod"].ToString();

                if (temp.Name == null) continue;
                ServerSetup.Instance.TempGlobalSkillTemplateCache[temp.Name] = temp;
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            Crashes.TrackError(e);
        }
    }
}