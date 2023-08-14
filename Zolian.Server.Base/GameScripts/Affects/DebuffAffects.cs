using System.Security.Cryptography;

using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Affects;

#region Armor

public class debuff_ardcradh : Debuff
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

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        UpdateDebuff(aisling);
    }

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

public class debuff_morcradh : Debuff
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

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        UpdateDebuff(aisling);
    }

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

public class debuff_decay : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 40);
    public override byte Icon => 110;
    public override int Length => 20;
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
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your body begins to decay");
        InsertDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        UpdateDebuff(aisling);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.BonusAc += AcModifer.Value;

        if (affected is not Aisling aisling) return;
        aisling.RegenTimerDisabled = false;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Body stops decaying");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}

public class debuff_cradh : Debuff
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

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        UpdateDebuff(aisling);
    }

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

public class debuff_beagcradh : Debuff
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

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        UpdateDebuff(aisling);
    }

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

public class debuff_rending : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 10);
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
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your armor's integrity is weakened");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(85, null, affected.Serial));
            UpdateDebuff(aisling);
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
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your armor's integrity has been restored");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}

public class debuff_rend : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 45);
    public override byte Icon => 110;
    public override int Length => 5;
    public override string Name => "Rend";

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
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your armor's integrity is weakened");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(383, null, affected.Serial));
            UpdateDebuff(aisling);
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
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your armor's integrity has been restored");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}

public class debuff_hurricane : Debuff
{
    private static StatusOperator AcModifer => new(Operator.Remove, 30);
    public override byte Icon => 116;
    public override int Length => 5;
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
            aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Your armor feels lighter");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(58, null, affected.Serial));
            UpdateDebuff(aisling);
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
}

#endregion

#region Movement
public class debuff_beagsuain : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_DarkChain : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_Silence : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_Halt : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_beagsuaingar : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_charmed : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_frozen : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_sleep : Debuff
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
            UpdateDebuff(aisling);
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
public class debuff_reaping : Debuff
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
            UpdateDebuff(aisling);
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
        var script = monster.Scripts.Values.First(_ => monster.IsAlive);
        script?.OnDeath();
    }
}
#endregion

#region DoT
public class debuff_bleeding : Debuff
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
            UpdateDebuff(aisling);
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

public class debuff_ArdPoison : Debuff
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
            UpdateDebuff(aisling);
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

        var cap = affected.CurrentHp - (int)(affected.CurrentHp * Modifier);
        if (cap > 0) affected.CurrentHp = cap;
    }
}

public class debuff_MorPoison : Debuff
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
            UpdateDebuff(aisling);
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

        var cap = affected.CurrentHp - (int)(affected.CurrentHp * Modifier);
        if (cap > 0) affected.CurrentHp = cap;
    }
}

public class debuff_Poison : Debuff
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
            UpdateDebuff(aisling);
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

        var cap = affected.CurrentHp - (int)(affected.CurrentHp * Modifier);
        if (cap > 0) affected.CurrentHp = cap;
    }
}

public class debuff_BeagPoison : Debuff
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
            UpdateDebuff(aisling);
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

        var cap = affected.CurrentHp - (int)(affected.CurrentHp * Modifier);
        if (cap > 0) affected.CurrentHp = cap;
    }
}

public class debuff_blind : Debuff
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
            UpdateDebuff(aisling);
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

#region Fas
public class debuff_ardfasnadur : Debuff
{
    public override byte Icon => 219;
    public override int Length => 640;
    public override string Name => "Ard Fas Nadur";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.Amplified = 3;
        }

        if (affected is not Aisling player) return;
        InsertDebuff(player, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        aisling.Amplified = 3;
        UpdateDebuff(aisling);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.Amplified = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer amplified.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}

public class debuff_morfasnadur : Debuff
{
    public override byte Icon => 218;
    public override int Length => 320;
    public override string Name => "Mor Fas Nadur";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.Amplified = 2;
        }

        if (affected is not Aisling player) return;
        InsertDebuff(player, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        aisling.Amplified = 2;
        UpdateDebuff(aisling);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.Amplified = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer amplified.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}

public class debuff_fasnadur : Debuff
{
    public override byte Icon => 119;
    public override int Length => 120;
    public override string Name => "Fas Nadur";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
            affected.Amplified = 1.5;
        }

        if (affected is not Aisling player) return;
        InsertDebuff(player, debuff);
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        aisling.Amplified = 1.5;
        UpdateDebuff(aisling);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);
        affected.Amplified = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer amplified.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}

public class debuff_fasspiorad : Debuff
{
    public override byte Icon => 26;
    public override int Length => 2;
    public override string Name => "Fas Spiorad";

    public override void OnApplied(Sprite affected, Debuff debuff)
    {
        if (affected.Debuffs.TryAdd(debuff.Name, debuff))
        {
            DebuffSpell = debuff;
            DebuffSpell.TimeLeft = DebuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));

        var reduce = aisling.MaximumMp * 0.33;
        aisling.CurrentHp -= (int)reduce;

        if (aisling.CurrentHp <= 0)
            aisling.CurrentHp = 1;

        aisling.CurrentMp = aisling.MaximumMp;

        InsertDebuff(aisling, debuff);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(26, false));
    }

    public override void OnDurationUpdate(Sprite affected, Debuff debuff)
    {
        if (affected is not Aisling aisling) return;
        UpdateDebuff(aisling);
    }

    public override void OnEnded(Sprite affected, Debuff debuff)
    {
        affected.Debuffs.TryRemove(debuff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The impact to your body dissipates.");
        DeleteDebuff(aisling, debuff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}
#endregion