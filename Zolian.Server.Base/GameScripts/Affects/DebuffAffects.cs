using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using System.Security.Cryptography;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.GameScripts.Affects;

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
            affected.BonusMr -= WillModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(200, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your magical resistance has been tampered");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusMr += WillModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Magical resistances return");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusMr -= WillModifer.Value.Item2;
    }
}

public class DebuffEclipseSeal : Debuff
{
    private static double AcModifer => 0.15; // 85% (Armor * Modifier)
    public override byte Icon => 226;
    public override int Length => 600;
    public override string Name => "Eclipse Seal";

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

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.SealedModifier = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The eclipse has ended");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.SealedModifier = AcModifer;
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

public class DebuffUasCradh : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 130);
    public override byte Icon => 211;
    public override int Length => 500;
    public override string Name => "Uas Cradh";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your body begins to decay");
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bYour body is decaying..");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = false;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Body begins to heal");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The curse lifted.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(85, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(72, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(85, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your armor's integrity has weakened");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Integrity restored");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(65, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(72, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your armor began corroding");
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(65, null, affected.Serial);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your armor stopped corroding");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(280, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(72, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(280, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Straps to your armor have loosened");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Integrity restored");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(383, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(72, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(383, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're suffering from a concussion");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Things begin to clear up");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(277, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(72, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(277, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Retribution is at hand!");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The target on your back has dissipated");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(160, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(72, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(160, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're now vulnerable");
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The feeling of vulnerability has passed");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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
            affected.BonusAc -= AcModifer.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(58, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(65, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The storm rages!!");
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(58, null, affected.Serial);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryRemove(debuff.Name, out _))
            affected.BonusAc += AcModifer.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The storm has passed");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Debuff debuff)
    {
        affected.BonusAc -= AcModifer.Value.Item2;
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(41, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(14, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(41, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been incapacitated");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(129, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(108, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(117, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been incapacitated");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(94, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(108, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(94, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "...");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(128, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(108, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(116, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Time is at a still");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(41, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(64, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(41, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been incapacitated");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(118, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(6, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(118, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Wow.. I can't harm you");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(40, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(15, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(40, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(123, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your body is frozen. Brrrrr...");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(40, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(15, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(40, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(123, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your body is semi-perm frozen. Brrrrr...");
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(32, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(29, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(32, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(65, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You've been placed in a dream like state");
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
    public override int Length => 15;
    public override string Name => "Skulled";
    private static string[] Messages => ServerSetup.Instance.Config.ReapMessage.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
    private static int Count => Messages.Length;

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling { GameMaster: true }) return;
        
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            if (affected is Monster monster)
            {
                DebuffSpell.TimeLeft = 5;
                monster.SendAnimationNearby(374, null, affected.Serial);
            }
        }

        if (affected is not Aisling aisling) return;

        if (aisling.CurrentMapId == ServerSetup.Instance.Config.DeathMap)
        {
            debuff.OnEnded(aisling, debuff);
            return;
        }

        aisling.Resting = Enums.RestPosition.MaximumChill;
        aisling.SendAnimationNearby(24, null, aisling.Serial);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(6, false));
        aisling.CurrentHp = 0;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
        aisling.Client.UpdateDisplay();
        aisling.Client.SendDisplayAisling(aisling);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

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

            // Prevent Dojo / Training deaths
            if (aisling.Map.Flags.MapFlagIsSet(MapFlags.SafeMap))
            {
                debuff.Cancelled = true;
                debuff.OnEnded(aisling, debuff);
                aisling.Client.Revive();
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
            aisling.SendAnimationNearby(24, null, aisling.Serial);
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(6, false));
        }

        if (affected is not Monster monster) return;
        monster.SendAnimationNearby(374, null, affected.Serial);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        if (affected is Aisling mapCheck && mapCheck.Map.ID is >= 800 and <= 810)
        {
            if (mapCheck.Debuffs.TryRemove(debuff.Name, out var saved))
            {
                saved.DeleteDebuff(mapCheck, saved);
                mapCheck.Client.SendEffect(byte.MinValue, saved.Icon);
            }
            mapCheck.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You were knocked out of the rift.");
            mapCheck.Client.TransitionToMap(188, new Position(12, 22));
            mapCheck.Client.Recover();
            mapCheck.Resting = Enums.RestPosition.Standing;
            mapCheck.Client.SendAttributes(StatUpdateType.Full);
            mapCheck.Client.UpdateDisplay();
            mapCheck.Client.SendDisplayAisling(mapCheck);
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
                aisling.SendTargetedClientMethod(PlayerScope.AislingsOnSameMap, client => client.SendSound(5, false));
                
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
        monster.Scripts?.Values.FirstOrDefault(_ => monster.IsAlive)?.OnDeath();
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(310, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(106, false));
        }

        if (affected is not Aisling aisling) return;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        ApplyBleeding(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(310, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Quick! You're bleeding out");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
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

    private static void ApplyBleeding(Sprite affected, Debuff debuff)
    {
        if (affected.CurrentHp <= affected.MaximumHp * 0.07)
        {
            debuff.OnEnded(affected, debuff);
            return;
        }

        var cap = (int)(affected.CurrentHp * Modifier);

        if (affected is Monster monster)
        {
            if (cap > 75000) cap = 75000;
            monster.CurrentHp -= cap;
            return;
        }

        if (cap > 0) affected.CurrentHp -= cap;
    }
}

public class DebuffDeadlyPoison : Debuff
{
    private static double Modifier => 0.12;
    public override byte Icon => 153;
    public override int Length => 300;
    public override string Name => "Deadly Poison";

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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(132, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(34, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        ApplyPoison(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(132, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
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
        if (affected.CurrentHp <= affected.MaximumHp * 0.08)
        {
            debuff.OnEnded(affected, debuff);
            return;
        }

        var cap = (int)(affected.CurrentHp * Modifier);

        if (affected is Monster monster)
        {
            if (cap > 90000) cap = 90000;
            monster.CurrentHp -= cap;
            return;
        }

        if (cap > 0) affected.CurrentHp -= cap;
    }
}

public class DebuffUasPoison : Debuff
{
    private static double Modifier => 0.13;
    public override byte Icon => 201;
    public override int Length => 400;
    public override string Name => "Uas Puinsein";

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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(25, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(34, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        ApplyPoison(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(25, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
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
            if (cap > 65000) cap = 65000;
            monster.CurrentHp -= cap;
            return;
        }

        if (cap > 0) affected.CurrentHp -= cap;
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(25, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(34, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        ApplyPoison(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(25, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
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
            if (cap > 25000) cap = 25000;
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(25, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(34, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        ApplyPoison(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(25, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
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
            if (cap > 15000) cap = 15000;
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(25, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(34, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        ApplyPoison(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(25, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
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
            if (cap > 5000) cap = 5000;
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

        if (affected is Damageable damageable)
        {
            damageable.SendAnimationNearby(25, null, affected.Serial);
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(34, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = true;
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);
        ApplyPoison(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(25, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Poisoned");
        aisling.Client.SendAttributes(StatUpdateType.Vitality);
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
            if (cap > 1000) cap = 1000;
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

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(25, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Blinded!");
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        base.OnDurationUpdate(affected, debuff);

        if (affected is Damageable damageable)
            damageable.SendAnimationNearby(42, null, affected.Serial);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        if (affected is not Aisling aisling) return;
        aisling.SendAnimationNearby(379, null, affected.Serial);
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You can see again.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}

#endregion