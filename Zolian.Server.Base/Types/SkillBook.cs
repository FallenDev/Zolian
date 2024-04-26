using Darkages.Network.Client;
using Darkages.Object;
using Darkages.Templates;

using ServiceStack;

using System.Collections.Concurrent;

namespace Darkages.Types;

public class SkillBook : ObjectManager
{
    private const int SkillLength = 88;
    private readonly int[] _invalidSlots = [0, 36, 72, 89];
    public readonly ConcurrentDictionary<int, Skill> Skills = new();

    public SkillBook()
    {
        for (var i = 0; i < SkillLength; i++) Skills[i + 1] = null;
    }

    private bool IsValidSlot(byte slot) => slot is > 0 and < SkillLength && !_invalidSlots.Contains(slot);

    public int FindEmpty(int start = 0)
    {
        for (var i = start; i < SkillLength; i++)
        {
            switch (i)
            {
                case 35:
                case 71:
                    continue;
            }

            if (Skills[i + 1] == null) return i + 1;
        }

        return -1;
    }

    private Skill FindInSlot(int slot)
    {
        Skill ret = null;

        if (Skills.TryGetValue(slot, out var skill))
            ret = skill;

        return ret is { Template: not null } ? ret : null;
    }

    public IEnumerable<Skill> GetSkills(Predicate<Skill> predicate) => Skills.Values.Where(i => i != null && predicate(i)).ToArray();

    public bool HasSkill(string s)
    {
        if (Skills == null || Skills.IsEmpty) return false;

        return Skills.Values.Where(skill => skill is not null).Where(skill => !skill.Template.Name.IsNullOrEmpty()).Any(skill => skill.Template.Name.ToLower().Equals(s.ToLower()));
    }

    public bool Has(SkillTemplate s) => Skills.Where(i => i.Value?.Template != null).Select(i => i.Value.Template).FirstOrDefault(i => i.Name.Equals(s.Name)) != null;

    public void Remove(WorldClient client, byte movingFrom)
    {
        if (!Skills.TryGetValue(movingFrom, out var copy)) return;
        if (Skills.TryUpdate(movingFrom, null, copy))
            client.SendRemoveSkillFromPane(movingFrom);
        client.DeleteSkillFromDb(copy);
    }

    public void Set(byte slot, Skill newSkill, Skill oldSkill) => Skills.TryUpdate(slot, newSkill, oldSkill);

    public bool AttemptSwap(WorldClient client, byte fromSlot, byte toSlot)
    {
        if (!IsValidSlot(fromSlot) || !IsValidSlot(toSlot)) return false;
        if (fromSlot == toSlot) return true;

        // Swap to advanced pane
        if (toSlot == 35)
        {
            var skillSlot = FindEmpty(36);
            toSlot = (byte)skillSlot;
        }

        if (toSlot == 71)
        {
            var skillSlot = FindEmpty();
            toSlot = (byte)skillSlot;
        }

        var skill1 = FindInSlot(fromSlot);
        var skill2 = FindInSlot(toSlot);

        if (skill1 != null)
            client.SendRemoveSkillFromPane(skill1.Slot);
        if (skill2 != null)
            client.SendRemoveSkillFromPane(skill2.Slot);

        if (skill1 != null && skill2 != null)
        {
            skill1.Slot = toSlot;
            skill2.Slot = fromSlot;
            Skills.TryUpdate(fromSlot, skill2, skill1);
            Skills.TryUpdate(toSlot, skill1, skill2);
            client.SendAddSkillToPane(skill1);
            client.SendAddSkillToPane(skill2);
            return true;
        }

        switch (skill1)
        {
            case null when skill2 != null:
                skill2.Slot = fromSlot;
                Skills.TryUpdate(fromSlot, skill2, null);
                Skills.TryUpdate(toSlot, null, skill2);
                client.SendAddSkillToPane(skill2);
                return true;
            case null:
                return true;
        }

        skill1.Slot = toSlot;
        Skills.TryUpdate(fromSlot, null, skill1);
        Skills.TryUpdate(toSlot, skill1, null);
        client.SendAddSkillToPane(skill1);
        return true;
    }
}