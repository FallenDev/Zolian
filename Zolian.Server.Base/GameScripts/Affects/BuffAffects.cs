using Chaos.Common.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.GameScripts.Affects;

#region Armor

public class buff_DiaAite : Buff
{
    public override byte Icon => 17;
    public override int Length => 3600;
    public override string Name => "Dia Aite";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYou feel Ceannlaidir's hand on your shoulder");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(314, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(93, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(314, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(93, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your resolve returns to normal");
        DeleteBuff(aisling, buff);
    }
}

public class buff_aite : Buff
{
    public override byte Icon => 11;
    public override int Length => 120;
    public override string Name => "Aite";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You feel a sense of security");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(93, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(93, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your resolve returns to normal");
        DeleteBuff(aisling, buff);
    }
}

public class buff_SpectralShield : Buff
{
    private static StatusOperator AcModifier => new(Operator.Add, 10);
    public override byte Icon => 149;
    public override int Length => 600;
    public override string Name => "Spectral Shield";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.BonusAc += AcModifier.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Spectral Shield has strengthened your resolve.");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(262, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(262, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusAc -= AcModifier.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your resolve returns to normal.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}

public class buff_DefenseUp : Buff
{
    private static StatusOperator AcModifier => new(Operator.Add, 20);
    public override byte Icon => 0;
    public override int Length => 150;
    public override string Name => "Defensive Stance";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.BonusAc += AcModifier.Value;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You're now aware of your surroundings.");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(83, false));
            InsertBuff(aisling, buff);
            aisling.Client.SendAttributes(StatUpdateType.Secondary);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(83, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusAc -= AcModifier.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've grown complacent.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }
}

#endregion

#region Enhancement

public class buff_Hasten : Buff
{
    public override byte Icon => 148;
    public override int Length => 20;
    public override string Name => "Hasten";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(750);
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Everything starts to slow down around you");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(189, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(189, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is Aisling aisling)
        {
            aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000);
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Time goes back to normal");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(190, null, affected.Serial));
            aisling.Client.SendEffect(byte.MinValue, Icon);
            DeleteBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(190, null, affected.Serial));
        }
    }
}

public class buff_clawfist : Buff
{
    public override byte Icon => 13;
    public override int Length => 8;
    public override string Name => "Claw Fist";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.ClawFistEmpowerment = true;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your hands are empowered!");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.ClawFistEmpowerment = false;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your hands turn back to normal.");
        DeleteBuff(aisling, buff);
    }
}

public class buff_ninthGate : Buff
{
    public override byte Icon => 208;
    public override int Length => 34;
    public override string Name => "Ninth Gate Release";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYour blood begins to boil, your bones crack!");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(305, null, aisling.Serial));
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYour body is badly damaged... so drained");
        if (aisling.CurrentHp > 100)
            aisling.CurrentHp = 100;
        if (aisling.CurrentMp > 100)
            aisling.CurrentMp = 100;

        aisling.Client.SendAttributes(StatUpdateType.Vitality);
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(75, null, aisling.Serial));
        DeleteBuff(aisling, buff);
    }
}

public class buff_berserk : Buff
{
    private static byte DmgModifier => 50;
    public override byte Icon => 207;
    public override int Length => 45;
    public override string Name => "Berserker Rage";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.BonusDmg += DmgModifier;
        }

        if (affected is not Aisling aisling) return;
        aisling.CurrentHp = aisling.MaximumHp;
        aisling.CurrentMp = 0;
        aisling.Client.SendAttributes(StatUpdateType.Full);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bEverything turns red!");
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(324, null, aisling.Serial));
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation((ushort)Random.Shared.Next(367, 369), null, aisling.Serial));
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusDmg -= DmgModifier;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou begin to realize your actions");
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(55, null, aisling.Serial));
        DeleteBuff(aisling, buff);
    }
}

public class buff_wingsOfProtect : Buff
{
    public override byte Icon => 194;
    public override int Length => 27;
    public override string Name => "Wings of Protection";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Wings of a guardian fall upon you");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(86, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(86, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The protection fades");
        DeleteBuff(aisling, buff);
    }
}

public class buff_ArdDion : Buff
{
    public override byte Icon => 194;
    public override int Length => 35;
    public override string Name => "Ard Dion";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've become almost impervious.");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(244, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(244, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You're no longer impervious.");
        DeleteBuff(aisling, buff);
    }
}

public class buff_MorDion : Buff
{
    public override byte Icon => 53;
    public override int Length => 20;
    public override string Name => "Mor Dion";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've become almost impervious.");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(244, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(244, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You're no longer impervious.");
        DeleteBuff(aisling, buff);
    }
}

public class buff_dion : Buff
{
    public override byte Icon => 53;
    public override int Length => 8;
    public override string Name => "Dion";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've become almost impervious.");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(6, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(6, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You're no longer impervious.");
        DeleteBuff(aisling, buff);
    }
}

public class buff_IronSkin : Buff
{
    public override byte Icon => 53;
    public override int Length => 50;
    public override string Name => "Iron Skin";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your skin turns to iron!");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your skin turns back to normal.");
        DeleteBuff(aisling, buff);
    }
}

public class buff_StoneSkin : Buff
{
    public override byte Icon => 53;
    public override int Length => 25;
    public override string Name => "Stone Skin";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your skin turns to stone!");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your skin turns back to normal.");
        DeleteBuff(aisling, buff);
    }
}

public class buff_hide : Buff
{
    public override byte Icon => 10;
    public override int Length => 45;
    public override string Name => "Hide";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            if (aisling.Client == null || aisling.IsDead()) return;
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Blended into the shadows");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(43, false));
            InsertBuff(aisling, buff);
            aisling.Client.UpdateDisplay();
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(43, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;

        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Emerged from the shadows");
        DeleteBuff(aisling, buff);
        aisling.Client.UpdateDisplay();
    }
}

public class buff_ShadowFade : Buff
{
    public override byte Icon => 10;
    public override int Length => 300;
    public override string Name => "Shadowfade";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            if (aisling.Client == null || aisling.IsDead()) return;
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Faded into the dark");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(43, false));
            InsertBuff(aisling, buff);
            aisling.Client.UpdateDisplay();
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(43, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        var client = aisling.Client;

        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Emerged from the shadows");
        client.UpdateDisplay();
        DeleteBuff(aisling, buff);
    }
}

public class buff_DexUp : Buff
{
    public override byte Icon => 148;
    public override int Length => 30;
    public override string Name => "Adrenaline";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.BonusDex += 15;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Adrenaline starts pumping!");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(367, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            aisling.Client.SendAttributes(StatUpdateType.Primary);
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(367, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusDex -= 15;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You begin to come down from your high");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Primary);
    }
}

public class buff_GryphonsGrace : Buff
{
    public override byte Icon => 216;
    public override int Length => 300;
    public override string Name => "Gryphons Grace";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.BonusDex += 50;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(139, false));
            aisling.Client.SendAttributes(StatUpdateType.Primary);
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(86, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(139, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusDex -= 50;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Starting to feel heavier again");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Primary);
    }
}

public class buff_OrcishStrength : Buff
{
    public override byte Icon => 215;
    public override int Length => 300;
    public override string Name => "Orcish Strength";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.BonusStr += 50;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(136, false));
            aisling.Client.SendAttributes(StatUpdateType.Primary);
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(34, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(136, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusStr -= 50;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Muscles return to normal");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Primary);
    }
}

public class buff_FeywildNectar : Buff
{
    public override byte Icon => 214;
    public override int Length => 300;
    public override string Name => "Feywild Nectar";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.BonusInt += 50;
            affected.BonusWis += 50;
        }

        if (affected is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(165, false));
            aisling.Client.SendAttributes(StatUpdateType.Primary);
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(35, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(165, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusInt -= 50;
        affected.BonusWis -= 50;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Feys disappear");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Primary);
    }
}

public class buff_randWeaponElement : Buff
{
    public override byte Icon => 110;
    public override int Length => 120;
    public override string Name => "Atlantean Weapon";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            while (affected.SecondaryOffensiveElement == ElementManager.Element.None)
            {
                affected.SecondaryOffensiveElement = Generator.RandomEnumValue<ElementManager.Element>();
            }
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Secondary Offensive element has changed {aisling.SecondaryOffensiveElement}");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(195, null, affected.Serial));
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(195, null, affected.Serial));
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.SecondaryOffensiveElement = ElementManager.Element.None;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Element enhancement has worn off");
        DeleteBuff(aisling, buff);
    }
}

public class buff_ElementalBane : Buff
{
    public override byte Icon => 17;
    public override int Length => 120;
    public override string Name => "Elemental Bane";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;

            affected.BonusFortitude += 100;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Resistance to damage increased by 33%");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusFortitude -= 100;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are no longer protected.");
        DeleteBuff(aisling, buff);
    }
}

#endregion

#region Reflection

public class buff_skill_reflect : Buff
{
    public override byte Icon => 118;
    public override int Length => 12;
    public override string Name => "Asgall";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Physical attacks are now being repelled");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Physical attacks can get through again");
        DeleteBuff(aisling, buff);
    }
}

public class buff_spell_reflect : Buff
{
    public override byte Icon => 54;
    public override int Length => 12;
    public override string Name => "Deireas Faileas";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Magical attacks are now being reflected back");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Spells are no longer being reflected");
        DeleteBuff(aisling, buff);
    }
}

public class buff_PerfectDefense : Buff
{
    public override byte Icon => 178;
    public override int Length => 18;
    public override string Name => "Perfect Defense";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff)
        {
            if (affected.HasBuff("Berserker Rage"))
            {
                berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
                return;
            }
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Aisling aisling)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Magical attacks are now being deflected");
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
            InsertBuff(aisling, buff);
        }
        else
        {
            var playerNearby = affected.PlayerNearby;
            if (playerNearby == null) return;
            playerNearby.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling berserkDebuff) return;
        if (!affected.HasBuff("Berserker Rage")) return;
        berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
        OnEnded(berserkDebuff, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Spells are no longer being deflected");
        DeleteBuff(aisling, buff);
    }
}

#endregion

#region Aura

public class aura_BriarThorn : Buff
{
    public override byte Icon => 213;
    public override int Length => 32767;
    public override string Name => "Briarthorn Aura";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Spikes += 10;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cAura: Briarthorn");
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(87, null, affected.Serial));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff) { }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Spikes -= 10;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Briarthorn ended");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);
    }
}

public class aura_LawsOfAosda : Buff
{
    public override byte Icon => 206;
    public override int Length => 32767;
    public override string Name => "Laws of Aosda";


    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cAura: Laws of Aosda");
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(236, null, affected.Serial));
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendSound(30, false));
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        if (aisling.PartyMembers == null) return;

        foreach (var player in aisling.PartyMembers)
        {
            if (player == null) continue;
            if (!player.Skulled) continue;
            if (!player.WithinRangeOf(aisling)) continue;
            aisling.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(236, null, aisling.Serial));
            aisling.CurrentHp = aisling.MaximumHp;
            aisling.Client.SendAttributes(StatUpdateType.Vitality);
            if (!aisling.LawsOfAosda.IsRunning)
                aisling.LawsOfAosda.Start();
        }

        if (!aisling.LawsOfAosda.IsRunning) return;
        if (aisling.LawsOfAosda.Elapsed.TotalSeconds < 10) return;
        aisling.LawsOfAosda.Stop();
        OnEnded(aisling, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Laws of Aosda ended");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);
    }
}

#endregion