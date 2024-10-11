using System.Diagnostics;
using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PlayerRegenerationComponent(WorldServer server) : WorldServerComponent(server)
{
    private static readonly object PlayerRegenLock = new();
    private static readonly Stopwatch PlayerRegenControl = new();

    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdatePlayerRegeneration);
    }

    private static void UpdatePlayerRegeneration()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;

        try
        {
            lock (PlayerRegenLock)
            {
                if (!PlayerRegenControl.IsRunning)
                    PlayerRegenControl.Start();

                if (PlayerRegenControl.Elapsed.TotalMilliseconds < 1000) return;

                Parallel.ForEach(Server.Aislings, (player) =>
                {
                    if (player?.Client == null) return;
                    if (!player.LoggedIn) return;
                    if (player.IsPoisoned || player.Skulled || player.IsDead())
                    {
                        player.RegenTimerDisabled = true;
                    }
                    else
                    {
                        player.RegenTimerDisabled = false;
                    }

                    if (player.RegenTimerDisabled) return;
                    if (player.CurrentHp == player.MaximumHp &&
                        player.CurrentMp == player.MaximumMp) return;
                    
                    if (player.CurrentHp > player.MaximumHp || player.CurrentMp > player.MaximumMp)
                    {
                        player.CurrentHp = player.MaximumHp;
                        player.CurrentMp = player.MaximumMp;
                        player.Client.SendAttributes(StatUpdateType.Vitality);
                    }

                    if (player.Path == Class.Peasant | player.GameMaster)
                    {
                        player.Recover();
                    }

                    if (player.CurrentMp < 1) player.CurrentMp = 1;

                    var hpRegenSeed = HpRegenSoftCap(player.Client);
                    var mpRegenSeed = MpRegenSoftCap(player.Client);
                    var hpHardCap = Math.Abs(player.BaseHp / 3.00);
                    var mpHardCap = Math.Abs(player.BaseMp / 3.00);
                    var performedRegen = false;

                    if (player.CurrentHp < player.MaximumHp)
                    {
                        RegenHpCalculator(player.Client, hpRegenSeed, hpHardCap);
                        performedRegen = true;
                    }

                    if (player.CurrentMp < player.MaximumMp)
                    {
                        RegenMpCalculator(player.Client, mpRegenSeed, mpHardCap);
                        performedRegen = true;
                    }

                    if (performedRegen)
                        player.Client.SendAttributes(StatUpdateType.Vitality);
                });
            }

        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }


    private static void RegenHpCalculator(WorldClient client, double seed, double cap)
    {
        var currentHp = client.Aisling.CurrentHp;

        try
        {
            currentHp += (long)Math.Clamp(seed, 5, cap);
            client.Aisling.CurrentHp = currentHp;

            if (client.Aisling.CurrentHp > client.Aisling.MaximumHp)
                client.Aisling.CurrentHp = client.Aisling.MaximumHp;
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"{ex}\nUnknown exception in RegenHPCalculator method.");
            SentrySdk.CaptureException(ex);
        }
    }

    private static void RegenMpCalculator(WorldClient client, double seed, double cap)
    {
        var currentMp = client.Aisling.CurrentMp;

        try
        {
            currentMp += (long)Math.Clamp(seed, 5, cap);
            client.Aisling.CurrentMp = currentMp;

            if (client.Aisling.CurrentMp > client.Aisling.MaximumMp)
                client.Aisling.CurrentMp = client.Aisling.MaximumMp;
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger($"{ex}\nUnknown exception in RegenHPCalculator method.");
            SentrySdk.CaptureException(ex);
        }
    }


    private static double HpRegenSoftCap(WorldClient client)
    {
        var conMod = Math.Abs(client.Aisling.Con / 3.00);
        var hpRegenSeed = client.Aisling.Regen switch
        {
            >= 0 and <= 9 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.00,
            <= 19 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.10,
            <= 29 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.20,
            <= 39 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.30,
            <= 49 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.40,
            <= 59 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.50,
            <= 69 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.60,
            <= 79 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.70,
            <= 89 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.80,
            <= 99 => Math.Abs(conMod + client.Aisling.ExpLevel) * 1.90,
            <= 109 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.00,
            <= 119 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.10,
            <= 129 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.20,
            <= 139 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.30,
            <= 149 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.40,
            <= 159 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.50,
            <= 169 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.60,
            <= 179 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.70,
            <= 189 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.80,
            <= 199 => Math.Abs(conMod + client.Aisling.ExpLevel) * 2.90,
            _ => Math.Abs(conMod + client.Aisling.ExpLevel) * 3.00
        };

        var healthMod = client.Aisling.BaseHp * 0.005;
        return Math.Abs(healthMod + hpRegenSeed);
    }

    private static double MpRegenSoftCap(WorldClient client)
    {
        var wisMod = Math.Abs(client.Aisling.Wis / 3.00);
        var mpRegenSeed = client.Aisling.Regen switch
        {
            >= 0 and <= 9 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.00,
            <= 19 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.10,
            <= 29 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.20,
            <= 39 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.30,
            <= 49 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.40,
            <= 59 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.50,
            <= 69 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.60,
            <= 79 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.70,
            <= 89 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.80,
            <= 99 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 1.90,
            <= 109 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.00,
            <= 119 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.10,
            <= 129 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.20,
            <= 139 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.30,
            <= 149 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.40,
            <= 159 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.50,
            <= 169 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.60,
            <= 179 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.70,
            <= 189 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.80,
            <= 199 => Math.Abs(wisMod + client.Aisling.ExpLevel) * 2.90,
            _ => Math.Abs(wisMod + client.Aisling.ExpLevel) * 3.00
        };

        var manaMod = client.Aisling.BaseMp * 0.005;
        return Math.Abs(manaMod + mpRegenSeed);
    }
}