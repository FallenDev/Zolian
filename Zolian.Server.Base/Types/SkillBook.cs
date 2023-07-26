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

namespace Darkages.Types;

public class SkillBook : ObjectManager
{
    private const int SkillLength = 88;
    private readonly int[] _invalidSlots = { 0, 36, 72, 89};
    public readonly ConcurrentDictionary<int, Skill> Skills = new();

    public SkillBook()
    {
        for (var i = 0; i < SkillLength; i++) Skills[i + 1] = null;
    }

    public bool IsValidSlot(byte slot) => slot is > 0 and < SkillLength && !_invalidSlots.Contains(slot);

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

    public Skill FindInSlot(int slot)
    {
        Skill ret = null;

        if (Skills.TryGetValue(slot, out var skill))
            ret = skill;

        return ret is { Template: not null } ? ret : null;
    }

    public IEnumerable<Skill> GetSkills(Predicate<Skill> predicate) => Skills.Values.Where(i => i != null && predicate(i)).ToArray();

    public bool HasSkill(string s)
    {
        if (Skills == null || Skills.Count == 0) return false;

        return Skills.Values.Where(skill => skill is not null).Where(skill => !skill.Template.Name.IsNullOrEmpty()).Any(skill => skill.Template.Name.ToLower().Equals(s.ToLower()));
    }

    public bool Has(SkillTemplate s) => Skills.Where(i => i.Value?.Template != null).Select(i => i.Value.Template).FirstOrDefault(i => i.Name.Equals(s.Name)) != null;

    public Skill Remove(WorldClient client, byte movingFrom, bool skillDelete = false)
    {
        if (!Skills.ContainsKey(movingFrom)) return null;
        var copy = Skills[movingFrom];
        if (skillDelete)
        {
            DeleteFromAislingDb(client, copy);
        }

        Skills[movingFrom] = null;
        return copy;
    }

    public void Set(Skill s) => Skills[s.Slot] = s;

    public bool AttemptSwap(WorldClient client, byte item1, byte item2)
    {
        if (!IsValidSlot(item1) || !IsValidSlot(item2)) return false;

        // Swap to advanced pane
        if (item2 == 35)
        {
            var skillSlot = FindEmpty(36);
            item2 = (byte)skillSlot;
        }

        if (item2 == 71)
        {
            var skillSlot = FindEmpty();
            item2 = (byte)skillSlot;
        }


        lock (Skills)
        {
            var obj1 = FindInSlot(item1);
            var obj2 = FindInSlot(item2);

            if (obj1 != null)
                client.SendRemoveSkillFromPane(obj1.Slot);
            if (obj2 != null)
                client.SendRemoveSkillFromPane(obj2.Slot);

            if (obj1 != null)
            {
                obj1.Slot = item2;
                Set(obj1);
                client.SendAddSkillToPane(obj1);
            }

            if (obj2 == null) return true;
            obj2.Slot = item1;
            Set(obj2);
            client.SendAddSkillToPane(obj2);

            return true;
        }
    }

    private static void DeleteFromAislingDb(WorldClient client, Skill skill)
    {
        try
        {
            using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSkillBook WHERE Serial = @Serial AND SkillName = @SkillName";
            sConn.Execute(cmd, new { client.Aisling.Serial, skill.SkillName });
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