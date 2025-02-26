using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Types;

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

    /// <summary>
    /// Updates cooldowns based on normal or haste conditions, if a haste is cast. It refreshes the cooldown of every skill & spell
    /// </summary>
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
            var hasteFlag = player.HasteFlag;

            var skills = player.SkillBook.Skills.Values;
            skills.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(skill => ProcessSkills(player, skill, hasteFlag));

            var spells = player.SpellBook.Spells.Values;
            spells.AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(spell => ProcessSpells(player, spell, hasteFlag));

            player.HasteFlag = false;
            player.Client.CooldownControl.Restart();
        }
    }

    private static void ProcessSkills(Aisling player, Skill skill, bool hasteFlag)
    {
        if (skill == null || skill.InUse) return;

        if (skill.CurrentCooldown >= 1)
        {
            skill.CurrentCooldown--;
            skill.Refreshed = false;
        }        

        if (hasteFlag)
            player.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);

        if (skill.CurrentCooldown >= 0) return;
        skill.CurrentCooldown = 0;
        if (skill.Refreshed) return;
        skill.Refreshed = true;
        player.Client.SendCooldown(true, skill.Slot, 0);
    }

    private static void ProcessSpells(Aisling player, Spell spell, bool hasteFlag)
    {
        if (spell == null || spell.InUse) return;

        if (spell.CurrentCooldown >= 1)
        {
            spell.CurrentCooldown--;
            spell.Refreshed = false;
        }

        if (hasteFlag)
            player.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);

        if (spell.CurrentCooldown >= 0) return;
        spell.CurrentCooldown = 0;
        if (spell.Refreshed) return;
        spell.Refreshed = true;
        player.Client.SendCooldown(false, spell.Slot, 0);
    }
}