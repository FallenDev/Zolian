﻿using Chaos.Common.Definitions;
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

using System.Data;

namespace Darkages.Types;

public class Buff : IBuff
{
    public bool Cancelled { get; set; }
    public virtual byte Icon { get; set; }
    public virtual int Length { get; set; }
    public virtual string Name { get; set; }
    public int TimeLeft { get; set; }
    public WorldServerTimer Timer { get; set; } = new(TimeSpan.FromSeconds(1));
    public Buff BuffSpell { get; set; }
    private readonly object _buffLock = new();

    public virtual void OnApplied(Sprite affected, Buff buff) { }
    public virtual void OnDurationUpdate(Sprite affected, Buff buff) { }
    public virtual void OnEnded(Sprite affected, Buff buff) { }

    public Buff ObtainBuffName(Sprite affected, Buff buff)
    {
        if (affected is not Aisling) return null;

        BuffSpell = buff.Name switch
        {
            "Dia Aite" => new buff_DiaAite(),
            "Aite" => new buff_aite(),
            "Claw Fist" => new buff_clawfist(),
            "Ard Dion" => new buff_ArdDion(),
            "Mor Dion" => new buff_MorDion(),
            "Dion" => new buff_dion(),
            "Stone Skin" => new buff_StoneSkin(),
            "Iron Skin" => new buff_IronSkin(),
            "Wings of Protection" => new buff_wingsOfProtect(),
            "Perfect Defense" => new buff_PerfectDefense(),
            "Shadow Step" => new buff_hide(),
            "Asgall" => new buff_skill_reflect(),
            "Deireas Faileas" => new buff_spell_reflect(),
            "Spectral Shield" => new buff_SpectralShield(),
            "Defensive Stance" => new buff_DefenseUp(),
            "Adrenaline" => new buff_DexUp(),
            "Atlantean Weapon" => new buff_randWeaponElement(),
            "Elemental Bane" => new buff_ElementalBane(),
            "Hasten" => new buff_Hasten(),
            "Hide" => new buff_hide(),
            "Shadowfade" => new buff_ShadowFade(),
            "Gryphons Grace" => new buff_GryphonsGrace(),
            "Orcish Strength" => new buff_OrcishStrength(),
            "Feywild Nectar" => new buff_FeywildNectar(),
            "Ninth Gate Released" => new buff_ninthGate(),
            "Berserker Rage" => new buff_berserk(),
            "Briarthorn Aura" => new aura_BriarThorn(),
            "Laws of Aosda" => new aura_LawsOfAosda(),
            _ => BuffSpell
        };

        return BuffSpell;
    }

    public void Update(Sprite affected, TimeSpan elapsedTime)
    {
        lock (_buffLock)
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

    public async void InsertBuff(Aisling aisling, Buff buff)
    {
        var continueInsert = await CheckOnBuffAsync(aisling.Client, buff.Name);
        if (continueInsert) return;

        try
        {
            await using var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            var cmd = new SqlCommand("InsertBuff", sConn);
            cmd.CommandType = CommandType.StoredProcedure;

            var buffId = EphemeralRandomIdGenerator<uint>.Shared.NextId;
            var buffNameReplaced = buff.Name;

            cmd.Parameters.Add("@BuffId", SqlDbType.Int).Value = buffId;
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)aisling.Serial;
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = buffNameReplaced;
            cmd.Parameters.Add("@TimeLeft", SqlDbType.Int).Value = buff.TimeLeft;

            cmd.CommandTimeout = 5;
            cmd.ExecuteNonQuery();
            sConn.Close();
        }
        catch (SqlException e)
        {
            if (e.Message.Contains("PK__Players"))
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Issue saving buff. Error: Velcro");
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

    public async void DeleteBuff(Aisling aisling, Buff buff)
    {
        try
        {
            var sConn = new SqlConnection(AislingStorage.ConnectionString);
            sConn.Open();
            const string playerBuffs = "DELETE FROM ZolianPlayers.dbo.PlayersBuffs WHERE Serial = @AislingSerial AND Name = @Name";
            await sConn.ExecuteAsync(playerBuffs, new
            {
                AislingSerial = (long)aisling.Serial,
                buff.Name
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

    public async Task<bool> CheckOnBuffAsync(IWorldClient client, string name)
    {
        try
        {
            const string procedure = "[SelectBuffsCheck]";
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