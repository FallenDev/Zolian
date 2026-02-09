using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.ScriptingBase;

public abstract class MonsterScript(Monster monster, Area map) : ObjectManager
{
    protected readonly Area Map = map;
    protected readonly Monster Monster = monster;

    public virtual void Update(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var update = monster.ObjectUpdateTimer.Update(elapsedTime);

        try
        {
            if (update)
            {
                monster.ObjectUpdateEnabled = true;
                monster.UpdateTarget();
            }

            monster.ObjectUpdateEnabled = false;

            if (monster.IsConfused || monster.IsFrozen || monster.IsStopped || monster.IsSleeping) return;

            MonsterState(elapsedTime);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger($"{e}\nUnhandled exception in {GetType().Name}.{nameof(Update)}");
            SentrySdk.CaptureException(e);
        }
    }

    public virtual void OnClick(WorldClient client)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        var colorA = "";
        var colorB = "";
        var colorLvl = LevelColor(client);
        var halfHp = $"{{=s{monster.CurrentHp}";
        var halfGone = monster.MaximumHp * .5;

        colorA = monster.OffenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorA
        };

        colorB = monster.DefenseElement switch
        {
            ElementManager.Element.Void => "{=n",
            ElementManager.Element.Holy => "{=g",
            ElementManager.Element.None => "{=s",
            ElementManager.Element.Fire => "{=b",
            ElementManager.Element.Water => "{=e",
            ElementManager.Element.Wind => "{=c",
            ElementManager.Element.Earth => "{=d",
            ElementManager.Element.Terror => "{=j",
            ElementManager.Element.Rage => "{=j",
            ElementManager.Element.Sorrow => "{=j",
            _ => colorB
        };

        if (monster.CurrentHp < halfGone)
        {
            halfHp = $"{{=b{monster.CurrentHp}{{=s";
        }

        if (monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
        {
            client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp}");
            client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=c{monster.Template.BaseName}: {{=aLv: {colorLvl}");
            return;
        }

        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=c{monster.Template.BaseName}: {{=aLv: {colorLvl} {{=aHP: {halfHp}/{monster.MaximumHp}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aSize: {{=s{monster.Size} {{=aAC: {{=s{monster.SealedAc} {{=aWill: {{=s{monster.Will}");
        client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}");
        client.SendServerMessage(ServerMessageType.PersistentMessage, $"{{=aLv: {colorLvl} {{=aO: {colorA}{monster.OffenseElement} {{=aD: {colorB}{monster.DefenseElement}");
    }

    public virtual void OnDeath(WorldClient client = null)
    {
        var monster = Monster;
        if (monster is null) return;

        var bank = monster.MonsterBank;
        for (var i = 0; i < bank.Count; i++)
        {
            var item = bank[i];
            if (item is null)
                continue;

            item.Release(monster, monster.Position);
            AddObject(item);

            foreach (var player in item.AislingsNearby())
                item.ShowTo(player);
        }

        if (monster.Target is null)
        {
            Aisling found = null;

            foreach (var kvp in monster.TargetRecord.TaggedAislings)
            {
                var p = kvp.Value;
                if (p?.Map == monster.Map)
                {
                    found = p;
                    break;
                }
            }

            monster.Target = found;
        }

        if (monster.Target is Aisling aisling)
        {
            monster.GenerateRewards(aisling);
            Monster.UpdateKillCounters(monster);
        }
        else
        {
            var level = monster.Template.Level;
            var sum = (uint)Random.Shared.Next(level * 13, level * 200);

            if (sum > 0)
                Money.Create(monster, sum, new Position(monster.Pos.X, monster.Pos.Y));
        }

        monster.Remove();
    }

    public virtual void MonsterState(TimeSpan elapsedTime)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return;

        if (monster.Target is not null && monster.TargetRecord.TaggedAislings.IsEmpty &&
            monster.Template.EngagedWalkingSpeed > 0)
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.EngagedWalkingSpeed);
        else
            monster.WalkTimer.Delay = TimeSpan.FromMilliseconds(monster.Template.MovementSpeed);

        var assail = monster.BashTimer.Update(elapsedTime);
        var ability = monster.AbilityTimer.Update(elapsedTime);
        var cast = monster.CastTimer.Update(elapsedTime);
        var walk = monster.WalkTimer.Update(elapsedTime);

        if (monster.Target is Aisling aisling)
        {
            if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral) && monster.Target.IsWeakened)
            {
                monster.Aggressive = false;
                monster.ClearTarget();
            }

            if (aisling.IsInvisible || aisling.Skulled || aisling.Dead || !aisling.LoggedIn)
            {
                monster.ClearTarget();

                if (monster.Template.MoodType.MoodFlagIsSet(MoodQualifer.Neutral))
                    monster.Aggressive = false;

                if (monster.CantMove || !monster.WalkEnabled) return;
                if (walk) monster.PreWalkChecks();

                return;
            }

            if (monster.BashEnabled && assail && !monster.CantAttack)
                monster.Bash();

            if (monster.AbilityEnabled && ability && !monster.CantAttack)
                monster.Abilities();

            if (monster.CastEnabled && cast && !monster.CantCast)
                monster.CastSpell();
        }

        if (monster.WalkEnabled && walk && !monster.CantMove)
            monster.PreWalkChecks();

        monster.UpdateTarget();
    }

    public virtual void OnApproach(WorldClient client) { }
    public virtual bool OnGossip(WorldClient client) => false;
    public virtual bool OnDispelled(WorldClient client) => false;
    public virtual void OnSkulled(WorldClient client) { }
    public virtual void OnGoldDropped(WorldClient client, uint gold) { }

    public virtual void OnItemDropped(WorldClient client, Item item)
    {
        if (item == null) return;
        if (client == null) return;
        var monster = Monster;
        if (monster is null || !monster.IsAlive) return;
        client.Aisling.Inventory.RemoveFromInventory(client.Aisling.Client, item);
        monster.MonsterBank.Add(item);
    }

    public virtual void OnLeave(WorldClient client)
    {
        try
        {
            lock (Monster.TaggedAislingsLock)
            {
                Monster.TargetRecord.TaggedAislings.TryRemove(client.Aisling.Serial, out _);
                if (Monster.Target == client.Aisling && Monster.TargetRecord.TaggedAislings.IsEmpty) Monster.ClearTarget();
            }
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }

    public virtual void OnDamaged(WorldClient client, long dmg, Sprite source)
    {
        try
        {
            lock (Monster.TaggedAislingsLock)
            {
                var tagged = Monster.TargetRecord.TaggedAislings.TryGetValue(client.Aisling.Serial, out var player);

                if (!tagged)
                    Monster.TryAddPlayerAndHisGroup(client.Aisling);
                else
                    Monster.TargetRecord.TaggedAislings.TryUpdate(client.Aisling.Serial, client.Aisling, client.Aisling);

                Monster.Aggressive = true;
            }
        }
        catch (Exception ex)
        {
            ServerSetup.EventsLogger(ex.ToString());
            SentrySdk.CaptureException(ex);
        }
    }

    internal string LevelColor(WorldClient client)
    {
        var monster = Monster;
        if (monster is null || !monster.IsAlive)
            return default;

        if (monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 30)
            return "{=n???{=s";
        if (monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 15)
            return $"{{=b{monster.Template.Level}{{=s";
        if (monster.Template.Level >= client.Aisling.Level + client.Aisling.AbpLevel + 10)
            return $"{{=c{monster.Template.Level}{{=s";
        if (monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 30)
            return $"{{=k{monster.Template.Level}{{=s";
        if (monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 15)
            return $"{{=j{monster.Template.Level}{{=s";
        return monster.Template.Level <= client.Aisling.Level + client.Aisling.AbpLevel - 10 ? $"{{=i{monster.Template.Level}{{=s" : $"{{=q{monster.Template.Level}{{=s";
    }
}