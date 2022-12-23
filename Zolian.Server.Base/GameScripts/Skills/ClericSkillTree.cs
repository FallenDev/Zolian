using Darkages.Enums;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Skills;

// Cleric Skills
// Blink = Teleport 
[Script("Blink")]
public class Blink : SkillScript
{
    private readonly Skill _skill;
    private readonly GlobalSkillMethods _skillMethod;

    public Blink(Skill skill) : base(skill)
    {
        _skill = skill;
        _skillMethod = new GlobalSkillMethods();
    }

    public override void OnFailed(Sprite sprite)
    {
        if (sprite is Aisling aisling)
            aisling.Client.SendMessage(0x02, "No suitable targets nearby.");
    }

    public override void OnSuccess(Sprite sprite)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        damageDealingSprite.Client.Aisling.Show(Scope.NearbyAislings, new ServerFormat29(76, damageDealingSprite.Pos));

        _skillMethod.Train(client, _skill);

        damageDealingSprite.Show(Scope.NearbyAislings, new ServerFormat19(_skill.Template.Sound));
    }

    public override void OnUse(Sprite sprite)
    {
        if (!_skill.CanUse()) return;
        if (sprite is not Aisling aisling) return;

        aisling.Client.SendMessage(0x02, "Use the Cleric's Feather (Drag & Drop on map)");
    }

    public override void ItemOnDropped(Sprite sprite, Position pos, Area map)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;
        damageDealingSprite.ActionUsed = "Blink";
        if (map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
        {
            client.SendMessage(0x03, "This does not work here");
            return;
        }

        damageDealingSprite.FacingFarAway(pos.X, pos.Y, out var direction);
        damageDealingSprite.Direction = (byte)direction;
        SendPortAnimation(damageDealingSprite, pos);
        client.WarpTo(pos, false);
        OnSuccess(damageDealingSprite);
    }

    public void SendPortAnimation(Sprite sprite, Position pos)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var orgPos = sprite.Pos;
        var xDiff = orgPos.X - pos.X;
        var yDiff = orgPos.Y - pos.Y;
        var xGap = Math.Abs(xDiff);
        var yGap = Math.Abs(yDiff);
        var xDiffHold = 0;
        var yDiffHold = 0;

        for (var i = 0; i < yGap; i++)
        {
            switch (yDiff)
            {
                case < 0:
                    yDiffHold++;
                    break;
                case > 0:
                    yDiffHold--;
                    break;
            }

            var newPos = orgPos with { Y = orgPos.Y + yDiffHold };
            var action = new ServerFormat29(197, newPos);
            damageDealingSprite.Show(Scope.NearbyAislings, action);
        }

        for (var i = 0; i < xGap; i++)
        {
            switch (xDiff)
            {
                case < 0:
                    xDiffHold++;
                    break;
                case > 0:
                    xDiffHold--;
                    break;
            }

            var newPos = orgPos with { X = orgPos.X + xDiffHold };
            var action = new ServerFormat29(197, newPos);
            damageDealingSprite.Show(Scope.NearbyAislings, action);
        }


    }
}
// Almighty Strike 
// Consecrated Strike
// Divine Strike
// Soaking Hands = Increase your healing power for a short duration
// Remedy = remove all debuffs on self
// Wrath = Devastates target with massive dark elemental aligned attack
// Recite Scripture = Damage enemy with opposite element of their defense