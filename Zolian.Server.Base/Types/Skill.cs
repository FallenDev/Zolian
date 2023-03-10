using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;
using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Types;

public class Skill
{
    public int SkillId { get; init; }
    public byte Icon { get; init; }
    public bool InUse { get; set; }
    public int Level { get; set; }
    [Browsable(false)] public string Name => $"{Template.Name} (Lev:{Level}/{Template.MaxLevel})";

    public string SkillName { get; init; }
    public int CurrentCooldown { get; set; }
    public bool Ready => CurrentCooldown == 0;

    public ConcurrentDictionary<string, SkillScript> Scripts { get; set; }

    public byte Slot { get; set; }
    public SkillTemplate Template { get; set; }
    public int Uses { get; set; }

    // For zero-line skill control
    public readonly GameServerTimer ZeroLineTimer = new (TimeSpan.FromMilliseconds(2000));
    public DateTime LastUsedSkill { get; set; }
    public bool CanUseZeroLineAbility
    {
        get
        {
            var readyTime = DateTime.Now;
            return readyTime - LastUsedSkill > new TimeSpan(0, 0, 0, 0, 500);
        }
    }

    // Used for Monster Scripts Only
    public DateTime NextAvailableUse { get; set; }

    public static void AttachScript(Skill skill)
    {
        skill.Scripts = ScriptManager.Load<SkillScript>(skill.Template.ScriptName, skill);
    }

    public static Skill Create(int slot, SkillTemplate skillTemplate)
    {
        var skillID = Generator.GenerateNumber();
        var obj = new Skill
        {
            Template = skillTemplate,
            SkillId = skillID,
            Level = 1,
            Slot = (byte)slot,
            Icon = skillTemplate.Icon
        };

        return obj;
    }

    public static bool GiveTo(GameClient client, string args)
    {
        if (!ServerSetup.Instance.GlobalSkillTemplateCache.ContainsKey(args)) return false;

        var skillTemplate = ServerSetup.Instance.GlobalSkillTemplateCache[args];

        if (skillTemplate == null) return false;
        if (client.Aisling.SkillBook.Has(skillTemplate)) return false;

        var slot = client.Aisling.SkillBook.FindEmpty(skillTemplate.Pane == Pane.Skills ? 0 : 72);

        if (slot <= 0) return false;

        var skill = Create(slot, skillTemplate);
        {
            AttachScript(skill);
            {
                client.Aisling.SkillBook.Set(skill);

                client.Send(new ServerFormat2C(skill.Slot, skill.Icon, skill.Name));
                client.Aisling.SendAnimation(22, client.Aisling, client.Aisling);
            }
        }

        try
        {
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("SkillToPlayer", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            var skillId = Generator.GenerateNumber();
            var skillNameReplaced = skill.Template.ScriptName;

            cmd.Parameters.Add("@SkillId", SqlDbType.Int).Value = skillId;
            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = client.Aisling.Serial;
            cmd.Parameters.Add("@Level", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = skill.Slot;
            cmd.Parameters.Add("@SkillName", SqlDbType.VarChar).Value = skillNameReplaced;
            cmd.Parameters.Add("@Uses", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@CurrentCooldown", SqlDbType.Int).Value = 0;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                if (!e.Message.Contains(client.Aisling.Serial.ToString())) return false;
                client.SendMessage(0x03, "Issue saving skill on issue. Contact GM");
                Crashes.TrackError(e);
                return false;
            }

            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return true;
    }

    public static bool GiveTo(Aisling aisling, string args, int level = 100)
    {
        if (!ServerSetup.Instance.GlobalSkillTemplateCache.ContainsKey(args)) return false;

        var skillTemplate = ServerSetup.Instance.GlobalSkillTemplateCache[args];

        if (skillTemplate == null) return false;
        if (aisling.SkillBook.Has(skillTemplate)) return false;

        var slot = aisling.SkillBook.FindEmpty(skillTemplate.Pane == Pane.Skills ? 0 : 72);

        if (slot <= 0) return false;

        var skill = Create(slot, skillTemplate);
        {
            AttachScript(skill);
            {
                aisling.SkillBook.Set(skill);

                aisling.Show(Scope.Self, new ServerFormat2C(skill.Slot, skill.Icon, skill.Name));
                aisling.SendAnimation(22, aisling, aisling);
            }
        }

        try
        {
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("SkillToPlayer", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            var skillId = Generator.GenerateNumber();
            var skillNameReplaced = skill.Template.ScriptName;

            cmd.Parameters.Add("@SkillId", SqlDbType.Int).Value = skillId;
            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
            cmd.Parameters.Add("@Level", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = skill.Slot;
            cmd.Parameters.Add("@SkillName", SqlDbType.VarChar).Value = skillNameReplaced;
            cmd.Parameters.Add("@Uses", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@CurrentCooldown", SqlDbType.Int).Value = 0;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                if (!e.Message.Contains(aisling.Serial.ToString())) return false;
                aisling.Client.SendMessage(0x03, "Issue saving skill on issue. Contact GM");
                Crashes.TrackError(e);
                return false;
            }

            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        catch (Exception e)
        {
            ServerSetup.Logger(e.Message, LogLevel.Error);
            ServerSetup.Logger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return true;
    }

    public bool CanUse()
    {
        return Ready;
    }

    internal static Sprite Reflect(Sprite enemy, Sprite damageDealingSprite, Skill skill)
    {
        if (enemy == null) return null;
        if (!enemy.SkillReflect) return enemy;

        var reflect = Generator.RandNumGen100();

        if (reflect > 45) return enemy;

        // Swap sprites reversing damage 
        (damageDealingSprite, enemy) = (enemy, damageDealingSprite);
        enemy.Animate(27);
        if (damageDealingSprite is Aisling)
            damageDealingSprite.Client.SendMessage(0x03, $"You deflected {skill.Template.Name}.");

        return enemy;
    }
}