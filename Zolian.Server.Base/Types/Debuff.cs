using System.Data;
using Dapper;

using Darkages.Common;
using Darkages.Database;
using Darkages.GameScripts.Affects;
using Darkages.Infrastructure;
using Darkages.Interfaces;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Types
{
    public class Debuff : IDebuff
    {
        public virtual ushort Animation { get; set; }
        public bool Cancelled { get; set; }
        public virtual byte Icon { get; set; }
        public virtual int Length { get; set; }
        public virtual string Name { get; set; }
        public int TimeLeft { get; set; }
        public GameServerTimer Timer { get; set; } = new (TimeSpan.FromSeconds(1));
        public Debuff DebuffSpell { get; set; }

        public virtual void OnApplied(Sprite affected, Debuff debuff) { }
        public virtual void OnDurationUpdate(Sprite affected, Debuff debuff) { }
        public virtual void OnEnded(Sprite affected, Debuff debuff) { }

        public Debuff ObtainDebuffName(Sprite affected, Debuff debuff)
        {
            if (affected is not Aisling) return null;

            DebuffSpell = debuff.Name switch
            {
                "Ard Cradh" => new debuff_ardcradh(),
                "Mor Cradh" => new debuff_morcradh(),
                "Cradh" => new debuff_cradh(),
                "Beag Cradh" => new debuff_beagcradh(),
                "Rending" => new debuff_rending(),
                "Rend" => new debuff_rend(),
                "Shield Bash" => new debuff_rend(),
                "Hurricane" => new debuff_hurricane(),
                "Beag Suain" => new debuff_beagsuain(),
                "Entice" => new debuff_charmed(),
                "Frozen" => new debuff_frozen(),
                "Halt" => new debuff_Halt(),
                "Sleep" => new debuff_sleep(),
                "Bleeding" => new debuff_bleeding(),
                "Ard Puinsein" => new debuff_ArdPoison(),
                "Mor Puinsein" => new debuff_MorPoison(),
                "Puinsein" => new debuff_Poison(),
                "Beag Puinsein" => new debuff_BeagPoison(),
                "Blind" => new debuff_blind(),
                "Ard Fas Nadur" => new debuff_ardfasnadur(),
                "Mor Fas Nadur" => new debuff_morfasnadur(),
                "Fas Nadur" => new debuff_fasnadur(),
                "Skulled" => new debuff_reaping(),
                "Decay" => new debuff_decay(),
                "Fas Spiorad" => new debuff_fasspiorad(),
                "Dark Chain" => new debuff_DarkChain(),
                "Silence" => new debuff_Silence(),
                _ => DebuffSpell
            };

            return DebuffSpell;
        }

        public void Update(Sprite affected, TimeSpan elapsedTime)
        {
            if (Timer.Disabled) return;
            if (!Timer.Update(elapsedTime)) return;
            if (Length - Timer.Tick > 0)
                OnDurationUpdate(affected, this);
            else
            {
                OnEnded(affected, this);
                Timer.Tick = 0;
                return;
            }

            Timer.Tick++;
        }

        public async void InsertDebuff(Aisling aisling, Debuff debuff)
        {
            var continueInsert = await CheckOnDebuffAsync(aisling.Client, debuff.Name);
            if (continueInsert) return;

            try
            {
                await using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var cmd = new SqlCommand("InsertDeBuff", sConn);
                cmd.CommandType = CommandType.StoredProcedure;

                var deBuffId = Generator.GenerateNumber();
                var debuffNameReplaced = debuff.Name;

                cmd.Parameters.Add("@DebuffId", SqlDbType.Int).Value = deBuffId;
                cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = debuffNameReplaced;
                cmd.Parameters.Add("@TimeLeft", SqlDbType.Int).Value = debuff.TimeLeft;

                cmd.CommandTimeout = 5;
                cmd.ExecuteNonQuery();
                sConn.Close();
            }
            catch (SqlException e)
            {
                if (e.Message.Contains("PK__Players"))
                {
                    aisling.Client.SendMessage(0x03, "Issue saving debuff. Error: Duct Tape");
                    Crashes.TrackError(e);
                    return;
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
        }

        public async void UpdateDebuff(Aisling aisling)
        {
            try
            {
                await using var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();

                foreach (var deBuff in aisling.Debuffs.Values.Where(i => i is { Name: { } }))
                {
                    var cmd = new SqlCommand("DeBuffSave", sConn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = aisling.Serial;
                    cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = deBuff.Name;
                    cmd.Parameters.Add("@TimeLeft", SqlDbType.Int).Value = deBuff.TimeLeft;

                    cmd.CommandTimeout = 5;
                    cmd.ExecuteNonQuery();
                }

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

        public async void DeleteDebuff(Aisling aisling, Debuff debuff)
        {
            try
            {
                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                const string playerDeBuffs = "DELETE FROM ZolianPlayers.dbo.PlayersDebuffs WHERE Serial = @Serial AND Name = @Name";
                await sConn.ExecuteAsync(playerDeBuffs, new
                {
                    aisling.Serial,
                    debuff.Name
                });
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

        public async Task<bool> CheckOnDebuffAsync(IGameClient client, string name)
        {
            try
            {
                const string procedure = "[SelectDeBuffsCheck]";
                var sConn = new SqlConnection(AislingStorage.ConnectionString);
                sConn.Open();
                var cmd = new SqlCommand(procedure, sConn)
                {
                    CommandType = CommandType.StoredProcedure
                };
                cmd.CommandTimeout = 5;
                cmd.Parameters.Add("@Serial", SqlDbType.Int).Value = client.Aisling.Serial;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = name;

                var reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    var buffName = reader["Name"].ToString();
                    if (!string.Equals(buffName, name, StringComparison.CurrentCultureIgnoreCase)) continue;
                    return string.Equals(name, buffName, StringComparison.CurrentCultureIgnoreCase);
                }

                reader.Close();
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

            return false;
        }
    }
}