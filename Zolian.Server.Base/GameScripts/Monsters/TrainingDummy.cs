using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Monsters;

[Script("Training Dummy")]
public class TrainingDummy : MonsterScript
{
    private DmgTable _incoming;

    public TrainingDummy(Monster monster, Area map) : base(monster, map)
    {
        Monster.BonusMr = 0;
        Monster.MonsterBank = new List<Item>();
    }

    public override void OnClick(GameClient client)
    {
        var level = Monster.Template.Level.ToString();
        var ac = Monster.Ac.ToString();
        var defEle = ElementManager.ElementValue(Monster.DefenseElement);

        client.SendMessage(0x02, $"Lvl: {level}, AC: {ac}, Def Element: {defEle}");
    }

    public override void OnDamaged(GameClient client, long dmg, Sprite source)
    {
        _incoming.What = client.Aisling.ActionUsed;

        if (dmg > int.MaxValue)
        {
            dmg = int.MaxValue;
        }
            
        var convDmg = (int)dmg;
        _incoming.Damage = convDmg;
        var dmgDisplay = _incoming.Damage.ToString();

        Monster.Show(Scope.NearbyAislings,
            new ServerFormat0D
            {
                Serial = Monster.Serial,
                Text = $"{client.Aisling.Username}'s {_incoming.What}: {dmgDisplay} DMG.\n",
                Type = 0x01
            });

        Monster.Facing((int)source.Pos.X, (int)source.Pos.Y, out var direction);

        if (!Monster.Position.IsNextTo(source.Position)) return;
        Monster.Direction = (byte)direction;
        Monster.Turn();
    }

    public override void OnDeath(GameClient client = null)
    {
        foreach (var debuff in Monster.Debuffs.Values)
        {
            if (debuff != null)
                Monster.Debuffs.TryRemove(debuff.Name, out _);
        }

        foreach (var debuff in Monster.Buffs.Values)
        {
            if (debuff != null)
                Monster.Buffs.TryRemove(debuff.Name, out _);
        }

        Monster.BonusAc = 0;
    }

    public override void OnSkulled(GameClient client) => Monster.Animate(49);

    public override void Update(TimeSpan elapsedTime)
    {
        if (Monster.CurrentHp < Monster.MaximumHp)
            Monster.CurrentHp = Monster.MaximumHp;
    }

    private struct DmgTable
    {
        public int Damage { get; set; }
        public string What { get; set; }
    }
}