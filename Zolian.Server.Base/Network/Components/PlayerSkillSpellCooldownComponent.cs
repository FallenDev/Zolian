using Darkages.Network.Server;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;

namespace Darkages.Network.Components;

public class PlayerSkillSpellCooldownComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdatePlayerSkillSpellCooldowns);
    }

    private static void UpdatePlayerSkillSpellCooldowns()
    {
        if (!ServerSetup.Instance.Running || !Server.Aislings.Any()) return;

        try
        {
            Parallel.ForEach(Server.Aislings, (player) =>
            {
                if (player?.Client == null) return;
                if (!player.LoggedIn) return;

                if (!player.Client.SkillControl.IsRunning)
                {
                    player.Client.SkillControl.Start();
                }

                if (player.Client.SkillControl.Elapsed.TotalMilliseconds < player.Client.SkillSpellTimer.Delay.TotalMilliseconds) return;

                var haste = Haste(player);

                foreach (var skill in player.SkillBook.Skills.Values)
                {
                    if (skill == null) continue;
                    if (skill.CurrentCooldown == 0) continue;
                    if (skill.CurrentCooldown == skill.Template.Cooldown)
                    {
                        if (player.Overburden)
                        {
                            var overburdened = skill.CurrentCooldown * 2;
                            player.Client.SendCooldown(true, skill.Slot, overburdened);
                        }
                        else
                        {
                            player.Client.SendCooldown(true, skill.Slot, (int)(skill.CurrentCooldown * haste));
                        }
                    }

                    skill.CurrentCooldown--;
                    if (skill.CurrentCooldown < 0)
                        skill.CurrentCooldown = 0;
                }

                foreach (var spell in player.SpellBook.Spells.Values)
                {
                    if (spell == null) continue;
                    if (spell.CurrentCooldown == 0) continue;
                    if (spell.CurrentCooldown == spell.Template.Cooldown)
                    {
                        if (player.Overburden)
                        {
                            var overburdened = spell.CurrentCooldown * 2;
                            player.Client.SendCooldown(false, spell.Slot, overburdened);
                        }
                        else
                            player.Client.SendCooldown(false, spell.Slot, (int)(spell.CurrentCooldown * haste));
                    }

                    spell.CurrentCooldown--;
                    if (spell.CurrentCooldown < 0)
                        spell.CurrentCooldown = 0;
                }

                player.Client.SkillControl.Restart();
            });
        }
        catch (Exception ex)
        {
            Crashes.TrackError(ex);
        }
    }

    private static double Haste(Aisling player)
    {
        if (!player.Hastened) return 1;
        return player.Client.SkillSpellTimer.Delay.TotalMilliseconds switch
        {
            500 => 0.50,
            750 => 0.75,
            _ => 1
        };
    }
}