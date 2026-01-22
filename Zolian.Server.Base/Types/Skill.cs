using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Templates;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;

namespace Darkages.Types;

public class Skill
{
    public byte Icon { get; init; }
    public bool InUse { get; set; }
    public int Level { get; set; }
    [Browsable(false)] public string Name => $"{Template.Name} (Lev:{Level}/{Template.MaxLevel})";

    public string SkillName { get; init; }
    public int CurrentCooldown { get; set; }

    public bool Ready => CurrentCooldown <= 0;
    public bool Refreshed { get; set; }

    public ConcurrentDictionary<string, SkillScript> Scripts { get; set; }

    public byte Slot { get; set; }
    public SkillTemplate Template { get; set; }
    public int Uses { get; set; }

    public DateTime LastUsedSkill { get; set; }
    public bool CanUseZeroLineAbility
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime - LastUsedSkill >= new TimeSpan(0, 0, 0, 0, 750);
        }
    }

    // Used for Monster Scripts Only
    public DateTime NextAvailableUse { get; set; } = DateTime.UtcNow;

    public bool CanUse() => Ready;

    public static void AttachScript(Skill skill) => skill.Scripts = ScriptManager.Load<SkillScript>(skill.Template.ScriptName, skill);

    public static Skill Create(int slot, SkillTemplate skillTemplate)
    {
        return new Skill
        {
            Template = skillTemplate,
            Level = 1,
            Slot = (byte)slot,
            Icon = skillTemplate.Icon
        };
    }

    public static bool GiveTo(Aisling aisling, string args)
    {
        if (!ServerSetup.Instance.GlobalSkillTemplateCache.TryGetValue(args, out var skillTemplate)) return false;
        if (skillTemplate == null) return false;
        if (aisling.SkillBook.Has(skillTemplate)) return false;

        var slot = aisling.SkillBook.FindEmpty(skillTemplate.Pane == Pane.Skills ? 0 : 72);
        if (slot <= 0) return false;
        var skill = Create(slot, skillTemplate);
        {
            AttachScript(skill);
            {
                aisling.SkillBook.Set((byte)slot, skill, null);
                aisling.Client.SendAddSkillToPane(skill);
                aisling.SendAnimationNearby(22, null, aisling.Serial);
            }
        }

        try
        {
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("SkillToPlayer", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
            cmd.Parameters.Add("@Level", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = skill.Slot;
            cmd.Parameters.Add("@SkillName", SqlDbType.VarChar).Value = skill.Template.Name;
            cmd.Parameters.Add("@Uses", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@CurrentCooldown", SqlDbType.Int).Value = 0;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        return true;
    }

    internal static Sprite Reflect(Sprite enemy, Sprite damageDealingSprite, Skill skill)
    {
        if (enemy == null) return null;
        if (!enemy.SkillReflect) return enemy;
        Aisling sender = null;

        var reflect = Generator.RandNumGen100();
        if (reflect > 45) return enemy;

        // Swap sprites reversing damage 
        (damageDealingSprite, enemy) = (enemy, damageDealingSprite);

        if (damageDealingSprite is Aisling aisling)
        {
            sender = aisling;
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"You deflected {skill.Template.Name}.");
        }

        if (enemy is Aisling enemyAisling)
            sender = enemyAisling;

        sender?.SendAnimationNearby(27, null, enemy.Serial);

        return enemy;
    }
}