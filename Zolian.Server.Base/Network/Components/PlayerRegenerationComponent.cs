using Darkages.Enums;
using Darkages.Infrastructure;
using Darkages.Interfaces;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Network.Components;

public class PlayerRegenerationComponent : GameServerComponent
{
    private readonly GameServerTimer _timer = new(TimeSpan.FromSeconds(1));

    public PlayerRegenerationComponent(Server.GameServer server) : base(server) { }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (_timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(UpdatePlayerRegeneration);
    }

    private static void UpdatePlayerRegeneration()
    {
        if (!ServerSetup.Instance.Running || ServerSetup.Instance.Game.Clients == null) return;
        foreach (var client in ServerSetup.Instance.Game.Clients.Values.Where(client => client is { Aisling: not null }))
        {
            if (!client.Aisling.LoggedIn) continue;
            if (client.Aisling.RegenTimerDisabled) continue;
            if (client.Aisling.Poisoned) continue;
            if (client.Aisling.IsDead()) continue;

            if (client.Aisling.CurrentHp == client.Aisling.MaximumHp &&
                client.Aisling.CurrentMp == client.Aisling.MaximumMp) continue;

            lock (client.SyncClient)
            {
                if (client.Aisling.CurrentHp > client.Aisling.MaximumHp)
                {
                    client.Aisling.CurrentHp = client.Aisling.MaximumHp;
                    client.SendStats(StatusFlags.Health);
                }

                if (client.Aisling.CurrentMp > client.Aisling.MaximumMp)
                {
                    client.Aisling.CurrentMp = client.Aisling.MaximumMp;
                    client.SendStats(StatusFlags.Health);
                }

                if (client.Aisling.Path == Class.Peasant | client.Aisling.GameMaster)
                {
                    client.Aisling.Recover();
                }

                if (client.Aisling.CurrentMp < 1) client.Aisling.CurrentMp = 1;

                var hpRegenSeed = HpRegenSoftCap(client);
                var mpRegenSeed = MpRegenSoftCap(client);
                var hpHardCap = Math.Abs(client.Aisling.BaseHp / 3.00);
                var mpHardCap = Math.Abs(client.Aisling.BaseMp / 3.00);
                var performedRegen = false;

                if (client.Aisling.CurrentHp < client.Aisling.MaximumHp)
                {
                    RegenHpCalculator(client, hpRegenSeed, hpHardCap);
                    performedRegen = true;
                }

                if (client.Aisling.CurrentMp < client.Aisling.MaximumMp)
                {
                    RegenMpCalculator(client, mpRegenSeed, mpHardCap);
                    performedRegen = true;
                }

                if (performedRegen)
                    client.SendStats(StatusFlags.Health);
            }
        }
    }


    private static void RegenHpCalculator(IGameClient client, double seed, double cap)
    {
        var currentHp = client.Aisling.CurrentHp;

        try
        {
            currentHp += (int)Math.Clamp(seed, 5, cap);
            client.Aisling.CurrentHp = currentHp;

            if (client.Aisling.CurrentHp > client.Aisling.MaximumHp)
                client.Aisling.CurrentHp = client.Aisling.MaximumHp;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger($"{ex}\nUnknown exception in RegenHPCalculator method.");
            Crashes.TrackError(ex);
        }
    }

    private static void RegenMpCalculator(IGameClient client, double seed, double cap)
    {
        var currentMp = client.Aisling.CurrentMp;

        try
        {
            currentMp += (int)Math.Clamp(seed, 5, cap);
            client.Aisling.CurrentMp = currentMp;

            if (client.Aisling.CurrentMp > client.Aisling.MaximumMp)
                client.Aisling.CurrentMp = client.Aisling.MaximumMp;
        }
        catch (Exception ex)
        {
            ServerSetup.Logger($"{ex}\nUnknown exception in RegenHPCalculator method.");
            Crashes.TrackError(ex);
        }
    }


    private static double HpRegenSoftCap(IGameClient client)
    {
        var conMod = Math.Abs(client.Aisling.Con / 3.00);
        var hpRegenSeed = client.Aisling.Regen switch
        {
            >= 0 and <= 9 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.00,
            >= 10 and <= 19 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.10,
            >= 20 and <= 29 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.20,
            >= 30 and <= 39 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.30,
            >= 40 and <= 49 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.40,
            >= 50 and <= 59 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.50,
            >= 60 and <= 69 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.60,
            >= 70 and <= 79 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.70,
            >= 80 and <= 89 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.80,
            >= 90 and <= 99 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.90,
            >= 100 and <= 109 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.00,
            >= 110 and <= 119 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.10,
            >= 120 and <= 129 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.20,
            >= 130 and <= 139 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.30,
            >= 140 and <= 149 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.40,
            >= 150 and <= 159 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.50,
            >= 160 and <= 169 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.60,
            >= 170 and <= 179 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.70,
            >= 180 and <= 189 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.80,
            >= 190 and <= 199 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.90,
            >= 200 => Math.Abs(conMod + client.Aisling.ExpLevel) * 3.00,
            _ => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.00,
        };

        var healthMod = client.Aisling.BaseHp * 0.005;
        return Math.Abs(healthMod + hpRegenSeed);
    }

    private static double MpRegenSoftCap(IGameClient client)
    {
        var wisMod = Math.Abs(client.Aisling.Wis / 3.00);
        var mpRegenSeed = client.Aisling.Regen switch
        {
            >= 0 and <= 9 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.00,
            >= 10 and <= 19 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.10,
            >= 20 and <= 29 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.20,
            >= 30 and <= 39 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.30,
            >= 40 and <= 49 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.40,
            >= 50 and <= 59 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.50,
            >= 60 and <= 69 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.60,
            >= 70 and <= 79 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.70,
            >= 80 and <= 89 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.80,
            >= 90 and <= 99 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.90,
            >= 100 and <= 109 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.00,
            >= 110 and <= 119 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.10,
            >= 120 and <= 129 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.20,
            >= 130 and <= 139 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.30,
            >= 140 and <= 149 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.40,
            >= 150 and <= 159 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.50,
            >= 160 and <= 169 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.60,
            >= 170 and <= 179 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.70,
            >= 180 and <= 189 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.80,
            >= 190 and <= 199 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.90,
            >= 200 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 3.00,
            _ => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.00,
        };

        var manaMod = client.Aisling.BaseMp * 0.005;
        return Math.Abs(manaMod + mpRegenSeed);
    }
}