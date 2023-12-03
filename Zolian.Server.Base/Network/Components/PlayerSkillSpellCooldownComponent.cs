using Darkages.Network.Server;

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

        Parallel.ForEach(Server.Aislings, (player) =>
        {
            if (player?.Client == null) return;
            if (!player.LoggedIn) return;
            if (!player.Client.SkillControl.IsRunning)
            {
                player.Client.SkillControl.Start();
            }

            if (player.Client.SkillControl.Elapsed.TotalMilliseconds < player.Client.SkillSpellTimer.Delay.TotalMilliseconds) return;

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

            player.Client.SkillControl.Restart();
        });
    }
}