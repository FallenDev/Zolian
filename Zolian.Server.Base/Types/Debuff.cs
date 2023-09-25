using System.Data;
using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Dapper;
using Darkages.Common;
using Darkages.Database;
using Darkages.GameScripts.Affects;
using Darkages.Interfaces;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Darkages.Types;

public class Debuff : IDebuff
{
    public bool Cancelled { get; set; }
    public virtual byte Icon { get; set; }
    public virtual int Length { get; set; }
    public virtual string Name { get; set; }
    public int TimeLeft { get; set; }
    public WorldServerTimer Timer { get; set; } = new (TimeSpan.FromSeconds(1));
    public Debuff DebuffSpell { get; set; }
    private readonly object _debuffLock = new();

    public virtual void OnApplied(Sprite affected, Debuff debuff) { }
    public virtual void OnDurationUpdate(Sprite affected, Debuff debuff) { }
    public virtual void OnEnded(Sprite affected, Debuff debuff) { }

    public Debuff ObtainDebuffName(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling) return null;

        DebuffSpell = debuff.Name switch
        {
            "Ard Cradh" => new DebuffArdcradh(),
            "Mor Cradh" => new DebuffMorcradh(),
            "Cradh" => new DebuffCradh(),
            "Beag Cradh" => new DebuffBeagcradh(),
            "Rending" => new DebuffRending(),
            "Rend" => new DebuffRend(),
            "Shield Bash" => new DebuffRend(),
            "Hurricane" => new DebuffHurricane(),
            "Beag Suain" => new DebuffBeagsuain(),
            "Entice" => new DebuffCharmed(),
            "Frozen" => new DebuffFrozen(),
            "Halt" => new DebuffHalt(),
            "Sleep" => new DebuffSleep(),
            "Bleeding" => new DebuffBleeding(),
            "Ard Puinsein" => new DebuffArdPoison(),
            "Mor Puinsein" => new DebuffMorPoison(),
            "Puinsein" => new DebuffPoison(),
            "Beag Puinsein" => new DebuffBeagPoison(),
            "Blind" => new DebuffBlind(),
            "Ard Fas Nadur" => new DebuffArdfasnadur(),
            "Mor Fas Nadur" => new DebuffMorfasnadur(),
            "Fas Nadur" => new DebuffFasnadur(),
            "Skulled" => new DebuffReaping(),
            "Decay" => new DebuffDecay(),
            "Fas Spiorad" => new DebuffFasspiorad(),
            "Dark Chain" => new DebuffDarkChain(),
            "Silence" => new DebuffSilence(),
            "Lycanisim" => new Lycanisim(),
            "Vampirisim" => new Vampirisim(),
            "Plagued" => new Plagued(),
            "The Shakes" => new TheShakes(),
            "Stricken" => new Stricken(),
            "Rabies" => new Rabies(),
            "Lock Joint" => new LockJoint(),
            "Numb Fall" => new NumbFall(),
            "Diseased" => new Diseased(),
            "Hallowed" => new Hallowed(),
            "Petrified" => new Petrified(),
            _ => DebuffSpell
        };

        return DebuffSpell;
    }

    public void Update(Sprite affected, TimeSpan elapsedTime)
    {
        lock (_debuffLock)
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

            var deBuffId = EphemeralRandomIdGenerator<uint>.Shared.NextId;
            var debuffNameReplaced = debuff.Name;

            cmd.Parameters.Add("@DebuffId", SqlDbType.Int).Value = deBuffId;
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)aisling.Serial;
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
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue saving debuff. Error: Duct Tape");
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

    public async void DeleteDebuff(Aisling aisling, Debuff debuff)
    {
        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string playerDeBuffs = "DELETE FROM ZolianPlayers.dbo.PlayersDebuffs WHERE Serial = @AislingSerial AND Name = @Name";
            await sConn.ExecuteAsync(playerDeBuffs, new
            {
                AislingSerial = (long)aisling.Serial,
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

    public async Task<bool> CheckOnDebuffAsync(IWorldClient client, string name)
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
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)client.Aisling.Serial;
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