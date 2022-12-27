using Darkages.Network.Client;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Monsters;

[Script("Tower Defense")]
public class TowerDefense : MonsterScript
{
    public TowerDefense(Monster monster, Area map)
        : base(monster, map)
    {
        Monster.Template.SpawnSize = 100;
        Monster.Template.SpawnMax = 100;
        Monster.Template.SpawnRate = 1;
    }

    public override void OnApproach(GameClient client)
    {
        Monster.Target = client.Aisling;
    }

    public override void OnClick(GameClient client) { }

    public override void OnDeath(GameClient client)
    {
        var remaining = GetObjects<Monster>(client.Aisling.Map, i => i.CurrentMapId == client.Aisling.AreaId
                                                                     && i.Template.Name == Monster.Template.Name)
            .Count();

        if (remaining <= 1)
        {
            var temp = Monster.Template;
            temp.Image += 2;
            temp.MovementSpeed -= 50;
            temp.SpawnMax += 2;
            temp.SpawnRate--;
            temp.SpawnSize++;

            if (temp.MovementSpeed <= 50)
                temp.MovementSpeed = 50;

            Monster.Template = temp;

            client.SendMessage(0x02, $"[Difficulty: {temp.Level}] Creeps get stronger ...");
        }

        if (GetObject<Monster>(client.Aisling.Map, i => i.Serial == Monster.Serial) != null)
            DelObject(Monster);
    }

    public override void OnLeave(GameClient client)
    {
        Monster.Target = null;
    }

    public override void Update(TimeSpan elapsedTime)
    {
        if (!Monster.IsAlive)
            return;

        Monster.WalkTimer.Update(elapsedTime);

        if (Monster.WalkTimer.Elapsed)
        {
            Monster.WalkTimer.Reset();

            if (Monster.WalkEnabled) Walk();
        }
    }

    public override void MonsterState(TimeSpan elapsedTime)
    {
        throw new NotImplementedException();
    }

    private void Walk()
    {
        if (!Monster.CanMove)
            return;

        if (Monster.Template.Waypoints.Count > 0)
            Monster.Patrol();

        if (Monster.CurrentMapId == 510 && Monster.Position.DistanceFrom(new Position(0, 12)) <= 1)
        {
            Monster.CurrentHp = 0;

            if (Monster.Target != null)
                Monster.Target.ApplyDamage(Monster, Monster.Target.MaximumHp / 10, null);
        }
    }
}