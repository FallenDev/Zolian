using Chaos.Common.Identity;
using Dapper;

using Darkages.Database;
using Darkages.GameScripts.Affects;
using Darkages.Network.Client.Abstractions;
using Darkages.Network.Server;
using Darkages.Sprites;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Data;

namespace Darkages.Types;

public class Debuff
{
    public bool Cancelled { get; set; }
    public virtual byte Icon { get; set; }
    public virtual int Length { get; set; }
    public virtual string Name { get; set; }
    public virtual bool Affliction { get; set; }
    public int TimeLeft { get; set; }
    public Debuff DebuffSpell { get; set; }

    public virtual void OnApplied(Sprite affected, Debuff debuff) { }
    public virtual void OnDurationUpdate(Sprite affected, Debuff debuff) => debuff.TimeLeft--;
    public virtual void OnEnded(Sprite affected, Debuff debuff) { }
    public virtual void OnItemChange(Aisling affected, Debuff debuff) { }

    public Debuff ObtainDebuffName(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling) return null;

        DebuffSpell = debuff.Name switch
        {
            "Sun Seal" => new DebuffSunSeal(),
            "Penta Seal" => new DebuffPentaSeal(),
            "Moon Seal" => new DebuffMoonSeal(),
            "Dark Seal" => new DebuffDarkSeal(),
            "Croich Ard Cradh" => new DebuffCriochArdCradh(),
            "Croich Mor Cradh" => new DebuffCriochMorCradh(),
            "Croich Cradh" => new DebuffCriochCradh(),
            "Croich Beag Cradh" => new DebuffCriochBeagCradh(),
            "Ard Cradh" => new DebuffArdcradh(),
            "Mor Cradh" => new DebuffMorcradh(),
            "Cradh" => new DebuffCradh(),
            "Beag Cradh" => new DebuffBeagcradh(),
            "Rending" => new DebuffRending(),
            "Corrosive Touch" => new DebuffCorrosiveTouch(),
            "Shield Bash" => new DebuffShieldBash(),
            "Titan's Cleave" => new DebuffTitansCleave(),
            "Retribution" => new DebuffRetribution(),
            "Stab'n Twist" => new DebuffStabnTwist(),
            "Hurricane" => new DebuffHurricane(),
            "Beag Suain" => new DebuffBeagsuain(),
            "Entice" => new DebuffCharmed(),
            "Frozen" => new DebuffFrozen(),
            "Adv Frozen" => new DebuffAdvFrozen(),
            "Halt" => new DebuffHalt(),
            "Sleep" => new DebuffSleep(),
            "Bleeding" => new DebuffBleeding(),
            "Ard Puinsein" => new DebuffArdPoison(),
            "Mor Puinsein" => new DebuffMorPoison(),
            "Puinsein" => new DebuffPoison(),
            "Beag Puinsein" => new DebuffBeagPoison(),
            "Blind" => new DebuffBlind(),
            "Skulled" => new DebuffReaping(),
            "Decay" => new DebuffDecay(),
            "Dark Chain" => new DebuffDarkChain(),
            "Silence" => new DebuffSilence(),
            "Plagued" => new Plagued(),
            "The Shakes" => new TheShakes(),
            "Stricken" => new Stricken(),
            "Rabies" => new Rabies(),
            "Lock Joint" => new LockJoint(),
            "Numb Fall" => new NumbFall(),
            "Diseased" => new Diseased(),
            "Hallowed" => new Hallowed(),
            "Petrified" => new Petrified(),
            "Wrath Consequences" => new DebuffWrathConsequences(),
            _ => DebuffSpell
        };

        return DebuffSpell;
    }

    public void Update(Sprite affected)
    {
        if (TimeLeft > 0)
            OnDurationUpdate(affected, this);
        else
            OnEnded(affected, this);
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
                SentrySdk.CaptureException(e);
                return;
            }

            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
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
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
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
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            SentrySdk.CaptureException(e);
        }

        return false;
    }
}