using Darkages.Network.Server;

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

                // Checks in real-time if a player is overburdened
                if (player.Overburden)
                {
                    player.Client.SkillSpellTimer.Delay = TimeSpan.FromMicroseconds(2000);
                    // If overburdened, set the trigger to remove it when not
                    player.OverburdenDelayed = true;
                }
                else
                {
                    // When not overburdened, check if the player was, and return the delay to normal
                    if (player.OverburdenDelayed)
                    {
                        player.Client.SkillSpellTimer.Delay = TimeSpan.FromMicroseconds(1000);
                        player.OverburdenDelayed = false;
                    }
                }

                if (player.Client.SkillControl.Elapsed.TotalMilliseconds <
                    player.Client.SkillSpellTimer.Delay.TotalMilliseconds) return;

                foreach (var skill in player.SkillBook.Skills.Values)
                {
                    if (skill == null) continue;
                    skill.CurrentCooldown--;
                    if (skill.CurrentCooldown < 0)
                        skill.CurrentCooldown = 0;
                }

                foreach (var spell in player.SpellBook.Spells.Values)
                {
                    if (spell == null) continue;
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
}