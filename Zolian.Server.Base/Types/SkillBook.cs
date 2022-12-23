using System.Collections.Concurrent;
using Dapper;

using Darkages.Database;
using Darkages.Object;
using Darkages.Templates;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ServiceStack;

namespace Darkages.Types
{
    public class SkillBook : ObjectManager
    {
        private const int SkillLength = 86;
        public readonly ConcurrentDictionary<int, Skill> Skills = new();

        public SkillBook()
        {
            for (var i = 0; i < SkillLength; i++) Skills[i + 1] = null;
        }

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

            if (Skills.ContainsKey(slot))
                ret = Skills[slot];

            return ret is { Template: { } } ? ret : null;
        }

        public IEnumerable<Skill> GetSkills(Predicate<Skill> predicate) => Skills.Values.Where(i => i != null && predicate(i)).ToArray();

        public bool HasSkill(string s)
        {
            if (Skills == null || Skills.Count == 0) return false;

            return Skills.Values.Where(skill => skill is not null).Where(skill => !skill.Template.Name.IsNullOrEmpty()).Any(skill => skill.Template.Name.ToLower().Equals(s.ToLower()));
        }

        public bool Has(SkillTemplate s) => Skills.Where(i => i.Value?.Template != null).Select(i => i.Value.Template).FirstOrDefault(i => i.Name.Equals(s.Name)) != null;

        public Skill Remove(byte movingFrom, bool skillDelete = false)
        {
            if (!Skills.ContainsKey(movingFrom)) return null;
            var copy = Skills[movingFrom];
            if (skillDelete)
            {
                DeleteFromAislingDb(copy);
            }

            Skills[movingFrom] = null;
            return copy;
        }

        public void Set(Skill s) => Skills[s.Slot] = s;

        private static void DeleteFromAislingDb(Skill skill)
        {
            try
            {
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSkillBook WHERE SkillId = @SkillId";
                sConn.Execute(cmd, new { skill.SkillId });
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
}