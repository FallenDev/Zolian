using Darkages.Network.Client;
using Darkages.Object;
using Darkages.Templates;

using ServiceStack;

using System.Collections.Concurrent;

namespace Darkages.Types;

public class SpellBook : ObjectManager
{
    private const int SpellLength = 88;
    private readonly int[] _invalidSlots = [0, 36, 72, 89];
    public readonly ConcurrentDictionary<int, Spell> Spells = [];

    public SpellBook()
    {
        for (var i = 0; i < SpellLength; i++) Spells[i + 1] = null;
    }

    private bool IsValidSlot(byte slot) => slot is > 0 and < SpellLength && !_invalidSlots.Contains(slot);

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

    public void Remove(WorldClient client, byte movingFrom)
    {
        if (!Spells.TryGetValue(movingFrom, out var copy)) return;
        if (Spells.TryUpdate(movingFrom, null, copy))
            client.SendRemoveSpellFromPane(movingFrom);
        client.DeleteSpellFromDb(copy);
    }

    public void Set(byte slot, Spell newSpell, Spell oldSpell) => Spells.TryUpdate(slot, newSpell, oldSpell);

    public bool AttemptSwap(WorldClient client, byte fromSlot, byte toSlot)
    {
        // Mark the player's save as dirty
        client?.Aisling?.PlayerSaveDirty = true;

        if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot)) return false;
        if (fromSlot == toSlot) return true;

        // Swap to advanced pane
        if (toSlot == 35)
        {
            var spellSlot = FindEmpty(36);
            toSlot = (byte)spellSlot;
        }

        if (toSlot == 71)
        {
            var spellSlot = FindEmpty();
            toSlot = (byte)spellSlot;
        }

        var spell1 = FindInSlot(fromSlot);
        var spell2 = FindInSlot(toSlot);

        if (spell1 != null)
            client.SendRemoveSpellFromPane(spell1.Slot);
        if (spell2 != null)
            client.SendRemoveSpellFromPane(spell2.Slot);

        if (spell1 != null && spell2 != null)
        {
            spell1.Slot = toSlot;
            spell2.Slot = fromSlot;
            Spells.TryUpdate(fromSlot, spell2, spell1);
            Spells.TryUpdate(toSlot, spell1, spell2);
            client.SendAddSpellToPane(spell1);
            client.SendAddSpellToPane(spell2);
            return true;
        }

        switch (spell1)
        {
            case null when spell2 != null:
                spell2.Slot = fromSlot;
                Spells.TryUpdate(fromSlot, spell2, null);
                Spells.TryUpdate(toSlot, null, spell2);
                client.SendAddSpellToPane(spell2);
                return true;
            case null:
                return true;
        }

        spell1.Slot = toSlot;
        Spells.TryUpdate(fromSlot, null, spell1);
        Spells.TryUpdate(toSlot, spell1, null);
        client.SendAddSpellToPane(spell1);
        return true;
    }
}