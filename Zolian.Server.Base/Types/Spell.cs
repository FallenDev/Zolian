using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.ScriptingBase;
using Darkages.Sprites;
using Darkages.Templates;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Data;

namespace Darkages.Types;

public class Spell
{
    public byte Icon { get; set; }
    public bool InUse { get; set; }
    public byte Level { get; set; }
    public int Lines { get; set; }
    [Browsable(false)] public string Name => $"{Template.Name} (Lev:{Level}/{Template.MaxLevel})";
    public string SpellName { get; init; }
    public int CurrentCooldown { get; set; }

    private bool Ready
    {
        get
        {
            var readyTime = DateTime.UtcNow;
            return readyTime.Subtract(LastUsedSpell).TotalSeconds >= Template.Cooldown;
        }
    }

    public ConcurrentDictionary<string, SpellScript> Scripts { get; set; }

    public byte Slot { get; set; }
    public SpellTemplate Template { get; set; }
    public int Casts { get; set; }
    public DateTime LastUsedSpell { get; set; }


    public static void AttachScript(Spell spell)
    {
        spell.Scripts = ScriptManager.Load<SpellScript>(spell.Template.ScriptName, spell);
    }

    public static Spell Create(int slot, SpellTemplate spellTemplate)
    {
        var obj = new Spell
        {
            Template = spellTemplate,
            Level = 1,
            Slot = (byte)slot,
            Icon = spellTemplate.Icon,
            Lines = spellTemplate.BaseLines
        };

        return obj;
    }

    public static bool GiveTo(WorldClient client, string args)
    {
        if (!client.Aisling.LoggedIn) return false;
        if (!ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey(args)) return false;

        var spellTemplate = ServerSetup.Instance.GlobalSpellTemplateCache[args];

        if (spellTemplate == null) return false;
        if (client.Aisling.SpellBook.Has(spellTemplate)) return false;
        var slot = client.Aisling.SpellBook.FindEmpty(spellTemplate.Pane == Pane.Spells ? 0 : 72);

        if (slot <= 0) return false;

        var spell = Create(slot, spellTemplate);
        {
            AttachScript(spell);
            {
                client.Aisling.SpellBook.Set((byte)slot, spell, null);
                client.SendAddSpellToPane(spell);
                client.Aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(22, null, client.Aisling.Serial));
            }
        }

        try
        {
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("SpellToPlayer", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = client.Aisling.Serial;
            cmd.Parameters.Add("@Level", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = spell.Slot;
            cmd.Parameters.Add("@SpellName", SqlDbType.VarChar).Value = spell.Template.Name;
            cmd.Parameters.Add("@Casts", SqlDbType.Int).Value = 0;
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
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue saving spell on issue. Contact GM");
                Crashes.TrackError(e);
                return false;
            }

            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return true;
    }

    public static bool GiveTo(Aisling aisling, string spellName, int level = 100)
    {
        if (!aisling.LoggedIn) return false;
        if (!ServerSetup.Instance.GlobalSpellTemplateCache.ContainsKey(spellName)) return false;

        var spellTemplate = ServerSetup.Instance.GlobalSpellTemplateCache[spellName];

        if (spellTemplate == null) return false;
        if (aisling.SpellBook.Has(spellTemplate)) return false;

        var slot = aisling.SpellBook.FindEmpty(spellTemplate.Pane == Pane.Spells ? 0 : 72);

        if (slot <= 0) return false;

        var spell = Create(slot, spellTemplate);
        {
            AttachScript(spell);
            {
                aisling.SpellBook.Set((byte)slot, spell, null);
                aisling.Client.SendAddSpellToPane(spell);
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(22, null, aisling.Serial));
            }
        }

        try
        {
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("SpellToPlayer", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
            cmd.Parameters.Add("@Level", SqlDbType.Int).Value = 0;
            cmd.Parameters.Add("@Slot", SqlDbType.Int).Value = spell.Slot;
            cmd.Parameters.Add("@SpellName", SqlDbType.VarChar).Value = spell.Template.Name;
            cmd.Parameters.Add("@Casts", SqlDbType.Int).Value = 0;
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
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue saving spell on issue. Contact GM");
                Crashes.TrackError(e);
                return false;
            }

            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return true;
    }

    public bool CanUse()
    {
        return Ready;
    }

    internal static Sprite SpellReflect(Sprite enemy, Sprite damageDealingSprite)
    {
        if (enemy == null) return null;
        if (!enemy.SpellReflect) return enemy;

        var reflect = Generator.RandNumGen100();

        if (reflect > 60) return enemy;
        (_, enemy) = (enemy, damageDealingSprite);

        return enemy;
    }
}