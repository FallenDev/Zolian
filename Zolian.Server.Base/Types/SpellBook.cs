using System.Collections.Concurrent;
using Dapper;

using Darkages.Database;
using Darkages.Network.Client;
using Darkages.Object;
using Darkages.Templates;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ServiceStack;

using static ServiceStack.Diagnostics.Events;

namespace Darkages.Types;

public class SpellBook : ObjectManager
{
    private const int SpellLength = 88;
    private readonly int[] _invalidSlots = { 0, 36, 72, 89};
    public readonly ConcurrentDictionary<int, Spell> Spells = new();

    public SpellBook()
    {
        for (var i = 0; i < SpellLength; i++) Spells[i + 1] = null;
    }

    public bool IsValidSlot(byte slot) => slot is > 0 and < SpellLength && !_invalidSlots.Contains(slot);

    public int FindEmpty(int start = 0)
    {
        for (var i = start; i < SpellLength; i++)
        {
            switch (i)
            {
                case 35:
                case 71:
                    continue;
            }

            if (Spells[i + 1] == null) return i + 1;
        }

        return -1;
    }

    public Spell FindInSlot(int slot)
    {
        Spell ret = null;

        if (Spells.TryGetValue(slot, out var spell))
            ret = spell;

        return ret is { Template: not null } ? ret : null;
    }

    public IEnumerable<Spell> TryGetSpells(Predicate<Spell> predicate) => Spells.Values.Where(i => i != null && predicate(i)).ToArray();

    public bool HasSpell(string s)
    {
        if (Spells == null || Spells.Count == 0) return false;

        return Spells.Values.Where(spell => spell is not null).Where(spell => !spell.Template.Name.IsNullOrEmpty()).Any(spell => spell.Template.Name.ToLower().Equals(s.ToLower()));
    }

    public bool Has(SpellTemplate s) => Spells.Where(i => i.Value?.Template != null).Select(i => i.Value.Template).FirstOrDefault(i => i.Name.Equals(s.Name)) != null;

    public Spell Remove(WorldClient client, byte movingFrom, bool spellDelete = false)
    {
        if (!Spells.ContainsKey(movingFrom)) return null;
        var copy = Spells[movingFrom];
        if (spellDelete)
        {
            DeleteFromAislingDb(client, copy);
        }

        Spells[movingFrom] = null;
        return copy;
    }

    public void Set(Spell s) => Spells[s.Slot] = s;

    public bool AttemptSwap(WorldClient client, byte item1, byte item2)
    {
        if (!IsValidSlot(item1) || !IsValidSlot(item2)) return false;

        // Swap to advanced pane
        if (item2 == 35)
        {
            var spellSlot = FindEmpty(36);
            item2 = (byte)spellSlot;
        }

        if (item2 == 71)
        {
            var spellSlot = FindEmpty();
            item2 = (byte)spellSlot;
        }

        lock (Spells)
        {
            var obj1 = FindInSlot(item1);
            var obj2 = FindInSlot(item2);

            if (obj1 != null)
                client.SendRemoveSpellFromPane(obj1.Slot);
            if (obj2 != null)
                client.SendRemoveSpellFromPane(obj2.Slot);

            if (obj1 != null)
            {
                obj1.Slot = item2;
                Set(obj1);
                client.SendAddSpellToPane(obj1);
            }

            if (obj2 == null) return true;
            obj2.Slot = item1;
            Set(obj2);
            client.SendAddSpellToPane(obj2);

            return true;
        }
    }

    private static void DeleteFromAislingDb(WorldClient client, Spell spell)
    {
        try
        {
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSpellBook WHERE Serial = @Serial AND SpellName = @SpellName";
            sConn.Execute(cmd, new { client.Aisling.Serial, spell.SpellName });
            sConn.Close();
        }
        catch (SqlException e)
        {
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
    }
}