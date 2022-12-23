using Darkages.Enums;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;

using ServiceStack;

namespace Darkages.Templates
{
    public class SpellTemplate : Template
    {
        public SpellTemplate() { }
        public string ScriptName { get; set; }
        public byte Icon { get; set; }
        public Pane Pane { get; set; }
        public int Cooldown { get; set; }
        public byte Sound { get; set; }
        public PostQualifier PostQualifiers { get; set; }
        public double LevelRate { get; set; }
        public LearningPredicate Prerequisites { get; set; }
        public List<LearningPredicate> LearningRequirements { get; } = new();
        public ElementManager.Element ElementalProperty { get; set; }
        public ushort TargetAnimation { get; set; }
        public ushort Animation { get; set; }
        public int BaseLines { get; set; }
        public int MinLines { get; set; }
        public int MaxLines { get; set; }
        public byte MaxLevel { get; set; }
        public int ManaCost { get; set; }
        public string NpcKey { get; set; }
        public SpellUseType TargetType { get; set; }

        public string[] GetMetaData()
        {
            return Prerequisites?.MetaData;
        }

        public enum SpellUseType : byte
        {
            Unusable = 0,
            Prompt = 1,
            ChooseTarget = 2,
            FourDigit = 3,
            ThreeDigit = 4,
            NoTarget = 5,
            TwoDigit = 6,
            OneDigit = 7
        }

    }

    public static class SpellStorage
    {
        public static void CacheFromDatabase(string conn)
        {
            try
            {
                var sConn = new SqlConnection(conn);
                const string sql = "SELECT * FROM ZolianAbilities.dbo.Spells";

                sConn.Open();

                var cmd = new SqlCommand(sql, sConn);
                cmd.CommandTimeout = 5;

                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var temp = new SpellTemplate();
                    var icon = (int)reader["Icon"];
                    var pane = reader["Pane"].ConvertTo<Pane>();
                    var sound = (int)reader["Sound"];
                    var post = reader["PostAttribute"].ConvertTo<PostQualifier>();
                    var levelRate = (decimal)reader["LevelRate"];
                    var predicateId = (int)reader["PredicateId"];
                    var element = reader["Element"].ConvertTo<ElementManager.Element>();
                    var targetAnim = (int)reader["TargetAnimation"];
                    var anim = (int)reader["Animation"];
                    var level = (int)reader["MaxLevel"];
                    var spellScope = reader["SpellScope"].ConvertTo<SpellTemplate.SpellUseType>();

                    temp.Icon = (byte)icon;
                    temp.ScriptName = reader["ScriptName"].ToString();
                    temp.Pane = pane;
                    temp.Cooldown = (int)reader["Cooldown"];
                    temp.Sound = (byte)sound;
                    temp.PostQualifiers = post;
                    temp.LevelRate = (double)levelRate;

                    #region LearningPredicate

                    var sConn2 = new SqlConnection(conn);
                    var sql2 = $"SELECT * FROM ZolianAbilities.dbo.SpellsPrerequisites WHERE PredicateId={predicateId.ToString()}";
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
                    }

                    reader2.Close();
                    sConn2.Close();

                    #endregion

                    temp.ElementalProperty = element;
                    temp.BaseLines = (int)reader["BaseLines"];
                    temp.MinLines = (int)reader["MinLines"];
                    temp.MaxLines = (int)reader["MaxLines"];
                    temp.TargetAnimation = (ushort)targetAnim;
                    temp.Animation = (ushort)anim;
                    temp.MaxLevel = (byte)level;
                    temp.ManaCost = (int)reader["ManaCost"];
                    temp.NpcKey = reader["NpcKey"].ToString();
                    temp.TargetType = spellScope;
                    temp.Description = reader["Description"].ToString();
                    temp.DamageMod = reader["DamageMod"].ToString();

                    if (temp.Name == null) continue;
                    ServerSetup.Instance.GlobalSpellTemplateCache[temp.Name] = temp;
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
    }
}