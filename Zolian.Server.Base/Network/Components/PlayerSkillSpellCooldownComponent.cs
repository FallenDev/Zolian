﻿using System.Diagnostics;
using Darkages.Network.Server;

namespace Darkages.Network.Components;

public class PlayerSkillSpellCooldownComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 100;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(1);
                continue;
            }

            UpdatePlayerSkillSpellCooldowns();
            var awaiter = (int)(ComponentSpeed - componentStopWatch.Elapsed.TotalMilliseconds);

            if (awaiter < 0)
            {
                variableGameSpeed = ComponentSpeed + awaiter;
                componentStopWatch.Restart();
                continue;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(awaiter));
            variableGameSpeed = ComponentSpeed;
            componentStopWatch.Restart();
        }
    }

    private static void UpdatePlayerSkillSpellCooldowns()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;

        foreach (var player in Server.Aislings)
        {
            if (player?.Client == null) continue;
            if (!player.LoggedIn) continue;
            if (!player.Client.CooldownControl.IsRunning)
                player.Client.CooldownControl.Start();
            
            if (player.Client.CooldownControl.Elapsed.TotalMilliseconds < player.Client.SkillSpellTimer.Delay.TotalMilliseconds) continue;

            Parallel.ForEach(player.SkillBook.Skills.Values, (skill) =>
            {
                if (skill == null) return;
                if (skill.CurrentCooldown == 0) return;

                skill.CurrentCooldown--;

                if (skill.CurrentCooldown < 0)
                    skill.CurrentCooldown = 0;
            });

            Parallel.ForEach(player.SpellBook.Spells.Values, (spell) =>
            {
                if (spell == null) return;
                if (spell.CurrentCooldown == 0) return;

                spell.CurrentCooldown--;

                if (spell.CurrentCooldown < 0)
                    spell.CurrentCooldown = 0;
            });

            player.Client.CooldownControl.Restart();
        }
    }
}