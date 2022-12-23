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
    public class SpellBook : ObjectManager
    {
        private const int SpellLength = 86;
        public readonly ConcurrentDictionary<int, Spell> Spells = new();

        public SpellBook()
        {
            for (var i = 0; i < SpellLength; i++) Spells[i + 1] = null;
        }

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

            if (Spells.ContainsKey(slot))
                ret = Spells[slot];

            return ret is { Template: { } } ? ret : null;
        }

        public IEnumerable<Spell> GetSpells(Predicate<Spell> predicate) => Spells.Values.Where(i => i != null && predicate(i)).ToArray();

        public bool HasSpell(string s)
        {
            if (Spells == null || Spells.Count == 0) return false;

            return Spells.Values.Where(spell => spell is not null).Where(spell => !spell.Template.Name.IsNullOrEmpty()).Any(spell => spell.Template.Name.ToLower().Equals(s.ToLower()));
        }

        public bool Has(SpellTemplate s) => Spells.Where(i => i.Value?.Template != null).Select(i => i.Value.Template).FirstOrDefault(i => i.Name.Equals(s.Name)) != null;

        public Spell Remove(byte movingFrom, bool spellDelete = false)
        {
            if (!Spells.ContainsKey(movingFrom)) return null;
            var copy = Spells[movingFrom];
            if (spellDelete)
            {
                DeleteFromAislingDb(copy);
            }

            Spells[movingFrom] = null;
            return copy;
        }

        public void Set(Spell s) => Spells[s.Slot] = s;

        private static void DeleteFromAislingDb(Spell spell)
        {
            try
            {
                using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                const string cmd = "DELETE FROM ZolianPlayers.dbo.PlayersSpellBook WHERE SpellId = @SpellId";
                sConn.Execute(cmd, new { spell.SpellId });
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