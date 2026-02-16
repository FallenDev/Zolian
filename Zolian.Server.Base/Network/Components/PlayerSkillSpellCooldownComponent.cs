using System.Diagnostics;

using Darkages.Network.Server;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Network.Components;

public class PlayerSkillSpellCooldownComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 100;

    protected internal override async Task Update()
    {
        var sw = Stopwatch.StartNew();
        var target = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            var elapsed = sw.Elapsed.TotalMilliseconds;
            if (elapsed < target)
            {
                var remaining = (int)(target - elapsed);

                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, 10));

                continue;
            }

            UpdatePlayerSkillSpellCooldowns();

            var postElapsed = sw.Elapsed.TotalMilliseconds;
            var overshoot = postElapsed - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    /// <summary>
    /// Updates cooldowns based on normal or haste conditions.
    /// If haste is active, sends current cooldowns for all skills & spells.
    /// </summary>
    private static void UpdatePlayerSkillSpellCooldowns()
    {
        if (!ServerSetup.Instance.Running) return;

        Server.ForEachLoggedInAisling(static player =>
        {
            try
            {
                var client = player.Client;

                if (!client.CooldownControl.IsRunning)
                    client.CooldownControl.Start();

                if (client.CooldownControl.Elapsed.TotalMilliseconds <
                    client.SkillSpellTimer.Delay.TotalMilliseconds)
                {
                    return;
                }

                var hasteFlag = player.HasteFlag;

                // Skills
                foreach (var skill in player.SkillBook.Skills.Values)
                    ProcessSkills(player, skill, hasteFlag);

                // Spells
                foreach (var spell in player.SpellBook.Spells.Values)
                    ProcessSpells(player, spell, hasteFlag);

                // Remove haste flag after processing, buff readds it if still active
                player.HasteFlag = false;
                client.CooldownControl.Restart();
            }
            catch { }
        });
    }

    private static void ProcessSkills(Aisling player, Skill skill, bool hasteFlag)
    {
        if (skill == null || skill.InUse) return;

        if (skill.CurrentCooldown > 0)
        {
            skill.CurrentCooldown--;
            skill.Refreshed = false;

            if (hasteFlag)
                player.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);

            if (skill.CurrentCooldown > 0)
                return;
        }

        if (skill.CurrentCooldown < 0)
            skill.CurrentCooldown = 0;

        if (skill.Refreshed)
            return;

        skill.Refreshed = true;
        player.Client.SendCooldown(true, skill.Slot, 0);
    }

    private static void ProcessSpells(Aisling player, Spell spell, bool hasteFlag)
    {
        if (spell == null || spell.InUse) return;

        if (spell.CurrentCooldown > 0)
        {
            spell.CurrentCooldown--;
            spell.Refreshed = false;

            if (hasteFlag)
                player.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);

            if (spell.CurrentCooldown > 0)
                return;
        }

        if (spell.CurrentCooldown < 0)
            spell.CurrentCooldown = 0;

        if (spell.Refreshed)
            return;

        spell.Refreshed = true;
        player.Client.SendCooldown(false, spell.Slot, 0);
    }
}