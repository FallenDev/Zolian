using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Sprites;
using Darkages.Types;
using System.Security.Cryptography;

namespace Darkages.GameScripts.Affects;

#region Afflictions

public class Lycanisim : Debuff
{
    private static int DexModifier => 30;
    private static byte DmgModifier => 50;
    public override byte Icon => 183;
    public override int Length => int.MaxValue;
    public override string Name => "Lycanisim";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        var vamp = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Vampirisim);

        if (vamp)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bThey do not realize who they've bitten");
            return;
        }

        aisling.BonusDex += DexModifier;
        aisling.BonusDmg += DmgModifier;
        aisling.Afflictions |= Afflictions.Lycanisim;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusDex -= DexModifier;
        aisling.BonusDmg -= DmgModifier;
        aisling.Afflictions &= ~Afflictions.Lycanisim;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusDex += DexModifier;
        affected.BonusDmg += DmgModifier;
    }
}

public class Vampirisim : Debuff
{
    private static int DexModifier => 30;
    private static byte HitModifier => 50;
    public override byte Icon => 172;
    public override int Length => int.MaxValue;
    public override string Name => "Vampirisim";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        var lycan = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Lycanisim);

        if (lycan)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bClawing me? Hah!");
            return;
        }

        aisling.BonusDex += DexModifier;
        aisling.BonusDmg += HitModifier;
        aisling.Afflictions |= Afflictions.Vampirisim;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusDex -= DexModifier;
        aisling.BonusDmg -= HitModifier;
        aisling.Afflictions &= ~Afflictions.Vampirisim;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusDex += DexModifier;
        affected.BonusDmg += HitModifier;
    }
}

public class Plagued : Debuff
{
    private static int HpModifier => 500;
    private static int MpModifier => 500;
    private static int StatModifier => 5;
    public override byte Icon => 211;
    public override int Length => int.MaxValue;
    public override string Name => "Plagued";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        aisling.BonusHp -= HpModifier;
        aisling.BonusMp -= MpModifier;
        aisling.BonusStr -= StatModifier;
        aisling.BonusInt -= StatModifier;
        aisling.BonusWis -= StatModifier;
        aisling.BonusCon -= StatModifier;
        aisling.BonusDex -= StatModifier;
        aisling.Afflictions |= Afflictions.Plagued;
        aisling.Afflictions &= ~Afflictions.Normal;

        var diseased = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.TheShakes);
        if (diseased)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=b*cough* *cough*... *falls to knees*");
            var diseasedDebuff = new Diseased();
            aisling.Client.EnqueueDebuffAppliedEvent(affected, diseasedDebuff, TimeSpan.FromSeconds(diseasedDebuff.Length));
        }

        var hallowed = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Stricken);
        if (hallowed)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=b*wheezing*");
            var hallowedDebuff = new Hallowed();
            aisling.Client.EnqueueDebuffAppliedEvent(affected, hallowedDebuff, TimeSpan.FromSeconds(hallowedDebuff.Length));
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(49, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusHp += HpModifier;
        aisling.BonusMp += MpModifier;
        aisling.BonusStr += StatModifier;
        aisling.BonusInt += StatModifier;
        aisling.BonusWis += StatModifier;
        aisling.BonusCon += StatModifier;
        aisling.BonusDex += StatModifier;
        aisling.Afflictions &= ~Afflictions.Plagued;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusHp -= HpModifier;
        affected.BonusMp -= MpModifier;
        affected.BonusStr -= StatModifier;
        affected.BonusInt -= StatModifier;
        affected.BonusWis -= StatModifier;
        affected.BonusCon -= StatModifier;
        affected.BonusDex -= StatModifier;
    }
}

public class TheShakes : Debuff
{
    private static int ConModifier => 5;
    private static int DexModifier => 5;
    private static byte DmgModifier => 50;
    public override byte Icon => 177;
    public override int Length => int.MaxValue;
    public override string Name => "The Shakes";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        aisling.BonusCon -= ConModifier;
        aisling.BonusDex -= DexModifier;
        aisling.BonusDmg -= DmgModifier;
        aisling.Afflictions |= Afflictions.TheShakes;
        aisling.Afflictions &= ~Afflictions.Normal;

        var diseased = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Plagued);
        if (diseased)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=b*cough* *cough*... *falls to knees*");
            var diseasedDebuff = new Diseased();
            aisling.Client.EnqueueDebuffAppliedEvent(affected, diseasedDebuff, TimeSpan.FromSeconds(diseasedDebuff.Length));
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(49, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusCon += ConModifier;
        aisling.BonusDex += DexModifier;
        aisling.BonusDmg += DmgModifier;
        aisling.Afflictions &= ~Afflictions.TheShakes;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusCon -= ConModifier;
        affected.BonusDex -= DexModifier;
        affected.BonusDmg -= DmgModifier;
    }
}

public class Stricken : Debuff
{
    private static int MpModifier => 1500;
    private static int WisModifier => 10;
    private static byte RegenModifier => 10;
    public override byte Icon => 162;
    public override int Length => int.MaxValue;
    public override string Name => "Stricken";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        aisling.BonusMp -= MpModifier;
        aisling.BonusWis -= WisModifier;
        aisling.BonusRegen -= RegenModifier;
        aisling.Afflictions |= Afflictions.Stricken;
        aisling.Afflictions &= ~Afflictions.Normal;

        var hallowed = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Plagued);
        if (hallowed)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=b*wheezing*");
            var hallowedDebuff = new Hallowed();
            aisling.Client.EnqueueDebuffAppliedEvent(affected, hallowedDebuff, TimeSpan.FromSeconds(hallowedDebuff.Length));
        }

        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(49, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusMp += MpModifier;
        aisling.BonusWis += WisModifier;
        aisling.BonusRegen += RegenModifier;
        aisling.Afflictions &= ~Afflictions.Stricken;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusMp -= MpModifier;
        affected.BonusWis -= WisModifier;
        affected.BonusRegen -= RegenModifier;
    }
}

public class Rabies : Debuff
{
    public override byte Icon => 122;
    public override int Length => 300;
    public override string Name => "Rabies";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;

        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        aisling.Afflictions |= Afflictions.Rabies;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(24, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;

        if (aisling.GameMaster)
        {
            if (aisling.Debuffs.TryRemove(debuff.Name, out _))
                aisling.Client.SendEffect(byte.MinValue, Icon);
            return;
        }

        // ToDo: Will need to move this to a more perm cached location so it retains through relog
        aisling.RabiesCountDown++;
        if (aisling.RabiesCountDown >= Length) OnEnded(affected, debuff);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(24, null, aisling.Serial));
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        var death = new DebuffReaping();
        aisling.Client.EnqueueDebuffAppliedEvent(aisling, death, TimeSpan.FromSeconds(death.Length));
        DeleteDebuff(aisling, debuff);
    }
}

public class LockJoint : Debuff
{
    private static int DmgModifier => 30;
    public override byte Icon => 176;
    public override int Length => int.MaxValue;
    public override string Name => "Lock Joint";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        var petrified = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.NumbFall);

        if (petrified)
        {
            var diseasedDebuff = new Petrified();
            aisling.Client.EnqueueDebuffAppliedEvent(affected, diseasedDebuff, TimeSpan.FromSeconds(diseasedDebuff.Length));
        }

        aisling.BonusDmg -= DmgModifier;
        aisling.Afflictions |= Afflictions.LockJoint;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        var rand = Generator.RandomNumPercentGen();
        if (rand >= 0.02) return;
        var diseasedDebuff = new DebuffBeagsuain();
        aisling.Client.EnqueueDebuffAppliedEvent(affected, diseasedDebuff, TimeSpan.FromSeconds(diseasedDebuff.Length));
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusDmg += DmgModifier;
        aisling.Afflictions &= ~Afflictions.LockJoint;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusDmg -= DmgModifier;
    }
}

public class NumbFall : Debuff
{
    private static int DmgModifier => 30;
    private static byte HitModifier => 50;
    public override byte Icon => 147;
    public override int Length => int.MaxValue;
    public override string Name => "Numb Fall";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        var petrified = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.LockJoint);

        if (petrified)
        {
            var diseasedDebuff = new Petrified();
            aisling.Client.EnqueueDebuffAppliedEvent(affected, diseasedDebuff, TimeSpan.FromSeconds(diseasedDebuff.Length));
        }

        aisling.BonusHit -= HitModifier;
        aisling.BonusDmg -= DmgModifier;
        aisling.Afflictions |= Afflictions.NumbFall;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusHit += HitModifier;
        aisling.BonusDmg += DmgModifier;
        aisling.Afflictions &= ~Afflictions.NumbFall;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusHit -= HitModifier;
        affected.BonusDmg -= DmgModifier;
    }
}

public class Diseased : Debuff
{
    private static int StatModifier => 10;
    private static byte RegenModifier => 50;
    public override byte Icon => 209;
    public override int Length => int.MaxValue;
    public override string Name => "Diseased";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        aisling.BonusRegen -= RegenModifier;
        aisling.BonusStr -= StatModifier;
        aisling.BonusInt -= StatModifier;
        aisling.BonusWis -= StatModifier;
        aisling.BonusCon -= StatModifier;
        aisling.BonusDex -= StatModifier;
        aisling.Afflictions |= Afflictions.Diseased;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusRegen += RegenModifier;
        aisling.BonusStr += StatModifier;
        aisling.BonusInt += StatModifier;
        aisling.BonusWis += StatModifier;
        aisling.BonusCon += StatModifier;
        aisling.BonusDex += StatModifier;
        aisling.Afflictions &= ~Afflictions.Diseased;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusRegen -= RegenModifier;
        affected.BonusStr -= StatModifier;
        affected.BonusInt -= StatModifier;
        affected.BonusWis -= StatModifier;
        affected.BonusCon -= StatModifier;
        affected.BonusDex -= StatModifier;
    }
}

public class Hallowed : Debuff
{
    private static int WillModifier => 80;
    public override byte Icon => 200;
    public override int Length => int.MaxValue;
    public override string Name => "Hallowed";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        aisling.BonusMr -= WillModifier;
        aisling.Afflictions |= Afflictions.Hallowed;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.BonusMr += WillModifier;
        aisling.Afflictions &= ~Afflictions.Hallowed;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusMr -= WillModifier;
    }
}

public class Petrified : Debuff
{
    public override byte Icon => 201;
    public override int Length => int.MaxValue;
    public override string Name => "Petrified";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        InsertDebuff(aisling, debuff);
        aisling.Afflictions |= Afflictions.Petrified;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        var rand = Generator.RandomNumPercentGen();
        if (rand >= 0.05) return;
        var diseasedDebuff = new DebuffHalt();
        aisling.Client.EnqueueDebuffAppliedEvent(affected, diseasedDebuff, TimeSpan.FromSeconds(diseasedDebuff.Length));
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Afflictions &= ~Afflictions.Petrified;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The disease that gripped me, has passed");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteDebuff(aisling, debuff);
    }
}

#endregion

#region Armor

public class DebuffWrathConsequences : Debuff
{
    private static StatusOperator WillModifer => new(Operator.Remove, 60);
    public override byte Icon => 27;
    public override int Length => 100;
    public override string Name => "Wrath Consequences";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusMr -= WillModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your magical resistance has been tampered");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(200, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(200, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusMr += WillModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Magical resistances return");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusMr -= WillModifer.Value;
    }
}

public class DebuffSunSeal : Debuff
{
    private static double AcModifer => 0.25; // 75% (Armor * Modifier)
    public override byte Icon => 226;
    public override int Length => 400;
    public override string Name => "Sun Seal";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.SealedModifier = AcModifer;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.SealedModifier = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.SealedModifier = AcModifer;
    }
}

public class DebuffPentaSeal : Debuff
{
    private static double AcModifer => 0.30; // 70% (Armor * Modifier)
    public override byte Icon => 225;
    public override int Length => 350;
    public override string Name => "Penta Seal";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.SealedModifier = AcModifer;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.SealedModifier = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.SealedModifier = AcModifer;
    }
}

public class DebuffMoonSeal : Debuff
{
    private static double AcModifer => 0.35; // 65% (Armor * Modifier)
    public override byte Icon => 2;
    public override int Length => 300;
    public override string Name => "Moon Seal";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.SealedModifier = AcModifer;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.SealedModifier = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.SealedModifier = AcModifer;
    }
}

public class DebuffDarkSeal : Debuff
{
    private static double AcModifer => 0.45; // 55% (Armor * Modifier)
    public override byte Icon => 133;
    public override int Length => 240;
    public override string Name => "Dark Seal";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.SealedModifier = AcModifer;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.SealedModifier = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.SealedModifier = AcModifer;
    }
}

public class DebuffCriochArdCradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 80);
    public override byte Icon => 193;
    public override int Length => 350;
    public override string Name => "Croich Ard Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffCriochMorCradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 70);
    public override byte Icon => 192;
    public override int Length => 330;
    public override string Name => "Croich Mor Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}

public class DebuffCriochCradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 65);
    public override byte Icon => 191;
    public override int Length => 300;
    public override string Name => "Croich Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffCriochBeagCradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 60);
    public override byte Icon => 190;
    public override int Length => 280;
    public override string Name => "Croich Beag Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffArdcradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 50);
    public override byte Icon => 84;
    public override int Length => 240;
    public override string Name => "Ard Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffMorcradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 40);
    public override byte Icon => 83;
    public override int Length => 180;
    public override string Name => "Mor Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffDecay : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 55);
    public override byte Icon => 110;
    public override int Length => 60;
    public override string Name => "Decay";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your body begins to decay");
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bYour body is decaying..");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = false;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Body begins to heal");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffCradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 30);
    public override byte Icon => 82;
    public override int Length => 120;
    public override string Name => "Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffBeagcradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 20);
    public override byte Icon => 5;
    public override int Length => 60;
    public override string Name => "Beag Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff) { }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffRending : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 15);
    public override byte Icon => 189;
    public override int Length => 30;
    public override string Name => "Rending";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(85, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(85, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your armor's integrity has weakened");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(85, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(85, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Integrity restored");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffCorrosiveTouch : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 30);
    public override byte Icon => 31;
    public override int Length => 30;
    public override string Name => "Corrosive Touch";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your armor began corroding");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(65, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(65, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(65, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(65, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your armor stopped corroding");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffShieldBash : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 25);
    public override byte Icon => 101;
    public override int Length => 10;
    public override string Name => "ShieldBash";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(280, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(280, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Straps to your armor have loosened");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(280, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(280, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Integrity restored");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffTitansCleave : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 50);
    public override byte Icon => 54;
    public override int Length => 12;
    public override string Name => "Titan's Cleave";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(383, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(383, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're suffering from a concussion");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(383, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(383, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Things begin to clear up");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffRetribution : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 60);
    public override byte Icon => 133;
    public override int Length => 15;
    public override string Name => "Retribution";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(277, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(277, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Retribution is at hand!");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(277, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(277, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The target on your back has dissipated");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffStabnTwist : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 50);
    public override byte Icon => 46;
    public override int Length => 15;
    public override string Name => "Stab'n Twist";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(160, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(160, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(72, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're now vulnerable");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(160, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(160, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The feeling of vulnerability has passed");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

public class DebuffHurricane : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 45);
    public override byte Icon => 116;
    public override int Length => 12;
    public override string Name => "Hurricane";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The storm rages!!");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(58, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(65, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(58, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(65, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(58, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(58, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The storm has passed");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value;
    }
}

#endregion

#region Movement
public class DebuffBeagsuain : Debuff
{
    public override byte Icon => 38;
    public override int Length => 7;
    public override string Name => "Beag Suain";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(14, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(14, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been incapacitated");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can feel your limbs again");
        DeleteDebuff(aisling, debuff);
    }
}

public class DebuffDarkChain : Debuff
{
    public override byte Icon => 50;
    public override int Length => 18;
    public override string Name => "Dark Chain";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(129, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(108, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(129, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(108, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been incapacitated");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(117, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(117, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can feel your limbs again");
        DeleteDebuff(aisling, debuff);
    }
}

public class DebuffSilence : Debuff
{
    public override byte Icon => 112;
    public override int Length => 21;
    public override string Name => "Silence";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(94, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(108, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(94, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(108, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "...");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(94, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(94, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteDebuff(aisling, debuff);
    }
}

public class DebuffHalt : Debuff
{
    public override byte Icon => 106;
    public override int Length => 10;
    public override string Name => "Halt";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(128, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(108, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(128, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(108, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Time is at a still");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(116, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(116, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Things begin to move");
        DeleteDebuff(aisling, debuff);
    }
}

public class DebuffBeagsuaingar : Debuff
{
    public override byte Icon => 38;
    public override int Length => 10;
    public override string Name => "Beag Suain";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(64, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(64, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been incapacitated");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(41, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can feel your limbs again");
        DeleteDebuff(aisling, debuff);
    }
}

public class DebuffCharmed : Debuff
{
    public override byte Icon => 71;
    public override int Length => 7;
    public override string Name => "Entice";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(118, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(6, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(118, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(6, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Wow.. I can't harm you");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(118, null, affected.Serial));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(118, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "What was I thinking?!");
        DeleteDebuff(aisling, debuff);
    }
}

public class DebuffFrozen : Debuff
{
    public override byte Icon => 50;
    public override int Length => 8;
    public override string Name => "Frozen";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(15, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(15, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your body is frozen. Brrrrr...");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(123, false));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(123, false));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can move again");
        DeleteDebuff(aisling, debuff);
    }
}

public class DebuffAdvFrozen : Debuff
{
    public override byte Icon => 50;
    public override int Length => 16;
    public override string Name => "Adv Frozen";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(15, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(15, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your body is semi-perm frozen. Brrrrr...");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(123, false));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(40, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(123, false));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can move again");
        DeleteDebuff(aisling, debuff);
    }
}


public class DebuffSleep : Debuff
{
    public override byte Icon => 90;
    public override int Length => 12;
    public override string Name => "Sleep";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(32, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(29, false));
            InsertDebuff(aisling, debuff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(32, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(29, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been placed in a dream like state");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(32, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(65, false));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(32, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(65, false));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Was that a dream?");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}
#endregion

#region Reaping
public class DebuffReaping : Debuff
{
    public override byte Icon => 89;
    public override int Length => _afflicted is Aisling ? 15 : 5;
    public override string Name => "Skulled";
    private static string[] Messages => ServerSetup.Instance.Config.ReapMessage.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private static int Count => Messages.Length;
    private Sprite _afflicted;

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling { GameMaster: true }) return;
        _afflicted = affected;

        if (affected is Monster)
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(374, null, affected.Serial));
        }

        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;

        if (aisling.CurrentMapId == ServerSetup.Instance.Config.DeathMap)
        {
            debuff.OnEnded(aisling, debuff);
            return;
        }

        aisling.Resting = Enums.RestPosition.MaximumChill;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(24, null, aisling.Serial));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(6, false));
        aisling.CurrentHp = 0;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
        aisling.Client.UpdateDisplay();
        aisling.Client.SendDisplayAisling(aisling);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected.CurrentMapId == ServerSetup.Instance.Config.DeathMap)
        {
            debuff.OnEnded(affected, debuff);
            return;
        }

        if (affected is Aisling aisling)
        {
            if (aisling.GameMaster)
            {
                if (aisling.Debuffs.TryRemove(debuff.Name, out _))
                    aisling.Client.SendEffect(byte.MinValue, Icon);
                return;
            }

            var randCheck = Generator.RandNumGen100();

            if (randCheck <= 50)
            {
                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{Messages[RandomNumberGenerator.GetInt32(Count + 1) % Messages.Length]}");
            }

            aisling.RegenTimerDisabled = true;
            aisling.CurrentHp = 1;
            aisling.Client.SendAttributes(StatUpdateType.Full);
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(24, null, aisling.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(6, false));
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(374, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Map.ID is 192 or 290 or 14758)
        {
            affected.Debuffs.TryRemove(debuff.Name, out _);
            if (affected is not Aisling playerAffected) return;
            playerAffected.Client.SendEffect(byte.MinValue, Icon);
            playerAffected.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You cannot die here, saved");
            DeleteDebuff(playerAffected, debuff);
            return;
        }

        switch (affected)
        {
            case Aisling aisling when !debuff.Cancelled:
                foreach (var (_, value) in aisling.Debuffs)
                {
                    if (!aisling.Debuffs.TryRemove(value.Name, out var debuffs)) continue;
                    debuffs.DeleteDebuff(aisling, value);
                    aisling.Client.SendEffect(byte.MinValue, value.Icon);
                }
                foreach (var (_, value) in aisling.Buffs)
                {
                    if (!aisling.Buffs.TryRemove(value.Name, out var buffs)) continue;
                    buffs.DeleteBuff(aisling, value);
                    aisling.Client.SendEffect(byte.MinValue, value.Icon);
                }

                aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your soul has been ripped from your mortal coil.");
                aisling.SendTargetedClientMethod(Scope.AislingsOnSameMap, client => client.SendSound(5, false));

                aisling.PrepareForHell();
                aisling.CastDeath();
                aisling.Resting = Enums.RestPosition.Standing;
                aisling.Client.SendAttributes(StatUpdateType.Full);
                aisling.Client.UpdateDisplay();
                aisling.Client.SendDisplayAisling(aisling);
                break;
            case Aisling savedAffected when debuff.Cancelled:
                if (savedAffected.Debuffs.TryRemove(debuff.Name, out var saved))
                {
                    saved.DeleteDebuff(savedAffected, saved);
                    savedAffected.Client.SendEffect(byte.MinValue, saved.Icon);
                }

                savedAffected.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You were saved.");
                savedAffected.Client.Recover();
                savedAffected.Resting = Enums.RestPosition.Standing;
                savedAffected.Client.SendAttributes(StatUpdateType.Full);
                savedAffected.Client.UpdateDisplay();
                savedAffected.Client.SendDisplayAisling(savedAffected);
                break;
        }

        if (affected is not Monster monster) return;
        monster.Scripts?.Values.First(_ => monster.IsAlive).OnDeath();
    }
}
#endregion

#region DoT
public class DebuffBleeding : Debuff
{
    private static double Modifier => .07;
    public override byte Icon => 111;
    public override int Length => 7;
    public override string Name => "Bleeding";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(310, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(106, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(310, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(106, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        ApplyBleeding(affected);

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Quick! You're bleeding out");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(310, null, affected.Serial));
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(310, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Bleeding slowed");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
    }

    private static void ApplyBleeding(Sprite affected)
    {
        var tourniquet = affected.MaximumHp * .10;

        if (tourniquet <= 500) return;

        var cap = (int)(affected.CurrentHp - affected.CurrentHp * Modifier);
        if (cap > 0) affected.CurrentHp = cap;
    }
}

public class DebuffArdPoison : Debuff
{
    private static double Modifier => 0.08;
    public override byte Icon => 35;
    public override int Length => 250;
    public override string Name => "Ard Puinsein";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling { PoisonImmunity: true } immuneCheck)
        {
            immuneCheck.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou are immune to Poison");
            return;
        }

        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.RegenTimerDisabled = true;
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        ApplyPoison(affected, debuff);

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = false;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're starting to feel better");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
    }

    private static void ApplyPoison(Sprite affected, Debuff debuff)
    {
        if (affected.CurrentHp <= affected.MaximumHp * 0.04)
        {
            debuff.OnEnded(affected, debuff);
            return;
        }

        var cap = (int)(affected.CurrentHp * Modifier);

        if (affected is Monster monster)
        {
            if (cap > 5000) cap = 5000;
            monster.CurrentHp -= cap;
            return;
        }

        if (cap > 0) affected.CurrentHp -= cap;
    }
}

public class DebuffMorPoison : Debuff
{
    private static double Modifier => 0.05;
    public override byte Icon => 35;
    public override int Length => 220;
    public override string Name => "Mor Puinsein";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling { PoisonImmunity: true } immuneCheck)
        {
            immuneCheck.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou are immune to Poison");
            return;
        }

        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.RegenTimerDisabled = true;
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        ApplyPoison(affected, debuff);

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = false;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're starting to feel better");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    private static void ApplyPoison(Sprite affected, Debuff debuff)
    {
        if (affected.CurrentHp <= affected.MaximumHp * 0.06)
        {
            debuff.OnEnded(affected, debuff);
            return;
        }

        var cap = (int)(affected.CurrentHp * Modifier);

        if (affected is Monster monster)
        {
            if (cap > 2500) cap = 2500;
            monster.CurrentHp -= cap;
            return;
        }

        if (cap > 0) affected.CurrentHp -= cap;
    }
}

public class DebuffPoison : Debuff
{
    private static double Modifier => 0.04;
    public override byte Icon => 35;
    public override int Length => 200;
    public override string Name => "Puinsein";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling { PoisonImmunity: true } immuneCheck)
        {
            immuneCheck.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou are immune to Poison");
            return;
        }

        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.RegenTimerDisabled = true;
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        ApplyPoison(affected, debuff);

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = false;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're starting to feel better");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    private static void ApplyPoison(Sprite affected, Debuff debuff)
    {
        if (affected.CurrentHp <= affected.MaximumHp * 0.08)
        {
            debuff.OnEnded(affected, debuff);
            return;
        }

        var cap = (int)(affected.CurrentHp * Modifier);

        if (affected is Monster monster)
        {
            if (cap > 1000) cap = 1000;
            monster.CurrentHp -= cap;
            return;
        }

        if (cap > 0) affected.CurrentHp -= cap;
    }
}

public class DebuffBeagPoison : Debuff
{
    private static double Modifier => 0.03;
    public override byte Icon => 35;
    public override int Length => 180;
    public override string Name => "Beag Puinsein";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling { PoisonImmunity: true } immuneCheck)
        {
            immuneCheck.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=qYou are immune to Poison");
            return;
        }

        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.RegenTimerDisabled = true;
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(34, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        ApplyPoison(affected, debuff);

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = false;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're starting to feel better");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    private static void ApplyPoison(Sprite affected, Debuff debuff)
    {
        if (affected.CurrentHp <= affected.MaximumHp * 0.12)
        {
            debuff.OnEnded(affected, debuff);
            return;
        }

        var cap = (int)(affected.CurrentHp * Modifier);

        if (affected is Monster monster)
        {
            if (cap > 500) cap = 500;
            monster.CurrentHp -= cap;
            return;
        }

        if (cap > 0) affected.CurrentHp -= cap;
    }
}

public class DebuffBlind : Debuff
{
    public override byte Icon => 114;
    public override int Length => 25;
    public override string Name => "Blind";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(25, null, affected.Serial));
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Blinded!");
            InsertDebuff(aisling, debuff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(114, null, affected.Serial));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(42, null, affected.Serial));
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(42, null, affected.Serial));
        }
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(379, null, affected.Serial));
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can see again.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}
#endregion