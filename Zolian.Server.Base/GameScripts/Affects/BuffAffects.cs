using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.GameScripts.Affects;

#region Afflictions

public class BuffLycanisim : Buff
{
    private static int DexModifier => 30;
    private static byte DmgModifier => 50;
    public override byte Icon => 183;
    public override int Length => int.MaxValue;
    public override string Name => "Lycanisim";
    public override bool Affliction => true;

    public override void OnApplied(Sprite affected, Buff affliction)
    {
        if (affected is not Aisling aisling) return;
        var vamp = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Vampirisim);

        if (vamp)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bThey do not realize who they've bitten");
            return;
        }

        if (affected.Buffs.TryAdd(affliction.Name, affliction))
        {
            BuffSpell = affliction;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        InsertBuff(aisling, affliction);

        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bYou begin to howl uncontrollably");
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(345, aisling.Position));
        aisling.BonusDex += DexModifier;
        aisling.BonusDmg += DmgModifier;
        aisling.Afflictions |= Afflictions.Lycanisim;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(139, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnEnded(Sprite affected, Buff affliction)
    {
        if (affected is not Aisling aisling) return;
        affected.Buffs.TryRemove(affliction.Name, out _);
        aisling.BonusDex -= DexModifier;
        aisling.BonusDmg -= DmgModifier;
        aisling.Afflictions &= ~Afflictions.Lycanisim;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The desire to kill has passed.");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteBuff(aisling, affliction);
    }

    public override void OnItemChange(Aisling affected, Buff affliction)
    {
        affected.BonusDex += DexModifier;
        affected.BonusDmg += DmgModifier;
    }
}

public class BuffVampirisim : Buff
{
    private static int DexModifier => 30;
    private static byte HitModifier => 50;
    public override byte Icon => 172;
    public override int Length => int.MaxValue;
    public override string Name => "Vampirisim";
    public override bool Affliction => true;

    public override void OnApplied(Sprite affected, Buff affliction)
    {
        if (affected is not Aisling aisling) return;
        var lycan = aisling.Afflictions.AfflictionFlagIsSet(Afflictions.Lycanisim);

        if (lycan)
        {
            aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bClawing me? Hah!");
            return;
        }

        if (affected.Buffs.TryAdd(affliction.Name, affliction))
        {
            BuffSpell = affliction;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        InsertBuff(aisling, affliction);

        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bYour thirst is unquenchable!");
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(345, aisling.Position));
        aisling.BonusDex += DexModifier;
        aisling.BonusDmg += HitModifier;
        aisling.Afflictions |= Afflictions.Vampirisim;
        aisling.Afflictions &= ~Afflictions.Normal;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(139, null, affected.Serial));
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }

    public override void OnEnded(Sprite affected, Buff affliction)
    {
        if (affected is not Aisling aisling) return;
        affected.Buffs.TryRemove(affliction.Name, out _);
        aisling.BonusDex -= DexModifier;
        aisling.BonusDmg -= HitModifier;
        aisling.Afflictions &= ~Afflictions.Vampirisim;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "The thirst for others has passed.");
        aisling.Client.SendAttributes(StatUpdateType.Full);
        DeleteBuff(aisling, affliction);
    }

    public override void OnItemChange(Aisling affected, Buff affliction)
    {
        affected.BonusDex += DexModifier;
        affected.BonusDmg += HitModifier;
    }
}

#endregion

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

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(314, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(93, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYou feel Ceannlaidir's hand on your shoulder");
        InsertBuff(aisling, buff);
    }

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

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(93, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You feel a sense of security");
        InsertBuff(aisling, buff);
    }

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
            affected.BonusAc += AcModifier.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(262, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Spectral Shield has strengthened your resolve.");
        InsertBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusAc -= AcModifier.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your resolve returns to normal.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusAc += AcModifier.Value.Item2;
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
            affected.BonusAc += AcModifier.Value.Item2;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(83, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You're now aware of your surroundings.");
        InsertBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusAc -= AcModifier.Value.Item2;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've grown complacent.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
    }

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusAc += AcModifier.Value.Item2;
    }
}

#endregion

#region Enhancement

public class buff_Dia_Haste : Buff
{
    public override byte Icon => 148;
    public override int Length => 7200;
    public override string Name => "Dia Haste";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(189, affected.Position));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(750);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Everything starts to slow down around you");
        InsertBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is Aisling aisling)
            aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(750);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(190, affected.Position));

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Time goes back to normal");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }
}


public class buff_Hastenga : Buff
{
    public override byte Icon => 148;
    public override int Length => 20;
    public override string Name => "Hastenga";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(189, affected.Position));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(500);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Everything starts to slow down around you");
        InsertBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(190, affected.Position));

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Time goes back to normal");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }
}

public class buff_Hasten : Buff
{
    public override byte Icon => 148;
    public override int Length => 10;
    public override string Name => "Hasten";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(189, affected.Position));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(500);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Everything starts to slow down around you");
        InsertBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(190, affected.Position));

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Time goes back to normal");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }
}

public class buff_Haste : Buff
{
    public override byte Icon => 148;
    public override int Length => 5;
    public override string Name => "Haste";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(189, affected.Position));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(750);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Everything starts to slow down around you");
        InsertBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
        }
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(190, affected.Position));

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Time goes back to normal");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);

        foreach (var (_, skill) in aisling.SkillBook.Skills)
        {
            if (skill == null) continue;
            aisling.Client.SendCooldown(true, skill.Slot, skill.CurrentCooldown);
        }

        foreach (var (_, spell) in aisling.SpellBook.Spells)
        {
            if (spell == null) continue;
            aisling.Client.SendCooldown(false, spell.Slot, spell.CurrentCooldown);
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

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your hands are empowered!");
        InsertBuff(aisling, buff);
    }

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

public class BuffHardenedHands : Buff
{
    public override byte Icon => 97;
    public override int Length => 10;
    public override string Name => "Hardened Hands";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(34, null, affected.Serial));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your hands are hardened!");
        InsertBuff(aisling, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your hands return to normal.");
        DeleteBuff(aisling, buff);
    }
}

public class buff_drunkenFist : Buff
{
    public override byte Icon => 203;
    public override int Length => 45;
    public override string Name => "Drunken Fist";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYour body begins to sway, (+25% dmg +25% def)");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(208, null, aisling.Serial));
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=cYou are no longer drunk!");
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(1, null, aisling.Serial));
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
        base.OnDurationUpdate(affected, buff);
        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(305, null, aisling.Serial));
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
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(75, null, aisling.Serial));
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
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(324, null, aisling.Serial));
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation((ushort)Random.Shared.Next(367, 369), null, aisling.Serial));
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusDmg -= DmgModifier;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendAttributes(StatUpdateType.Secondary);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"{{=bYou begin to realize your actions");
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(55, null, aisling.Serial));
        DeleteBuff(aisling, buff);
    }

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusDmg += DmgModifier;
    }
}

public class buff_wingsOfProtect : Buff
{
    public override byte Icon => 194;
    public override int Length => 27;
    public override string Name => "Wings of Protection";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected is Aisling berserkDebuff && affected.HasDebuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(86, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }


        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Wings of a guardian fall upon you");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(244, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've become almost impervious.");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(244, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've become almost impervious.");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(6, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You've become almost impervious.");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your skin turns to iron!");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(89, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Your skin turns to stone!");
        InsertBuff(aisling, buff);
    }


    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(43, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Blended into the shadows");
        InsertBuff(aisling, buff);
        aisling.Client.UpdateDisplay();
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(43, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Faded into the dark");
        InsertBuff(aisling, buff);
        aisling.Client.UpdateDisplay();
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(367, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Adrenaline starts pumping!");
        aisling.Client.SendAttributes(StatUpdateType.Primary);
        InsertBuff(aisling, buff);
    }

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

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusDex += 15;
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

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(139, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendAttributes(StatUpdateType.Primary);
        InsertBuff(aisling, buff);
    }

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

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusDex += 50;
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

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(136, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendAttributes(StatUpdateType.Primary);
        InsertBuff(aisling, buff);
    }

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

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusStr += 50;
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

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(165, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendAttributes(StatUpdateType.Primary);
        InsertBuff(aisling, buff);
    }

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

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusInt += 50;
        affected.BonusWis += 50;
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
            affected.SecondaryOffensiveElement = Generator.RandomEnumValue<ElementManager.Element>();
        }

        if (affected is Damageable damageable)
        {
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(195, null, affected.Serial));
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        }

        if (affected is not Aisling aisling) return;
        aisling.TempOffensiveHold = aisling.SecondaryOffensiveElement;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Secondary Offensive element has changed {aisling.SecondaryOffensiveElement}");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
        if (affected is Aisling aisling)
            aisling.SecondaryOffensiveElement = aisling.TempOffensiveHold;
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.SecondaryOffensiveElement = ElementManager.Element.None;

        if (affected is not Aisling aisling) return;
        aisling.TempOffensiveHold = ElementManager.Element.None;

        // Off-Hand elements override First Accessory
        if (aisling.EquipmentManager.Equipment[3]?.Item != null && aisling.EquipmentManager.Equipment[3].Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
        {
            if (aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryOffensiveElement != ElementManager.Element.None)
                aisling.SecondaryOffensiveElement = aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryOffensiveElement;

            if (aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryDefensiveElement != ElementManager.Element.None)
                aisling.SecondaryDefensiveElement = aisling.EquipmentManager.Equipment[3].Item.Template.SecondaryDefensiveElement;
        }
        else if (aisling.EquipmentManager.Equipment[14]?.Item != null && aisling.EquipmentManager.Equipment[14].Item.Template.Flags.FlagIsSet(ItemFlags.Elemental))
        {
            if (aisling.EquipmentManager.Equipment[14].Item.Template.SecondaryOffensiveElement != ElementManager.Element.None)
                aisling.SecondaryOffensiveElement = aisling.EquipmentManager.Equipment[14].Item.Template.SecondaryOffensiveElement;

            if (aisling.EquipmentManager.Equipment[14].Item.Template.SecondaryDefensiveElement != ElementManager.Element.None)
                aisling.SecondaryDefensiveElement = aisling.EquipmentManager.Equipment[14].Item.Template.SecondaryDefensiveElement;
        }

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

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusFortitude -= 100;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are no longer protected.");
        DeleteBuff(aisling, buff);
    }

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.BonusFortitude += 100;
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Physical attacks are now being repelled");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Magical attacks are now being reflected back");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        if (affected is Aisling berserkDebuff && affected.HasBuff("Berserker Rage"))
        {
            berserkDebuff.Client.SendServerMessage(ServerMessageType.ActiveMessage, "You are unsure of your actions");
            return;
        }

        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is Damageable damageable)
            damageable.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Magical attacks are now being deflected");
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
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
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(87, null, affected.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        InsertBuff(aisling, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        var item = new Item();
        item.ReapplyItemModifiers(aisling.Client);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Briarthorn ended");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);
    }

    public override void OnItemChange(Aisling affected, Buff buff)
    {
        affected.Spikes += 10;
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
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(236, null, affected.Serial));
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
        if (affected is not Aisling aisling) return;
        if (aisling.Skulled)
        {
            aisling.ReviveFromAfar(aisling);

            if (aisling.LawsOfAosda.IsRunning)
            {
                aisling.LawsOfAosda.Stop();
                aisling.LawsOfAosda.Reset();
            }

            OnEnded(aisling, buff);
            return;
        }

        if (aisling.GroupParty?.PartyMembers != null)
        {
            foreach (var player in aisling.GroupParty.PartyMembers.Values)
            {
                if (player == null) continue;
                if (player == aisling) continue;
                if (!player.Skulled) continue;
                if (!player.WithinRangeOf(aisling)) continue;

                // Start the Clock if a player needs rescue
                if (!aisling.LawsOfAosda.IsRunning)
                    aisling.LawsOfAosda.Start();

                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(236, null, aisling.Serial));
                aisling.CurrentHp = aisling.MaximumHp;
                aisling.Client.SendAttributes(StatUpdateType.Vitality);
            }
        }

        if (!aisling.LawsOfAosda.IsRunning) return;
        if (aisling.LawsOfAosda.Elapsed.TotalMilliseconds < 7000) return;
        aisling.LawsOfAosda.Stop();
        aisling.LawsOfAosda.Reset();
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

public class aura_SecuredPosition : Buff
{
    public override byte Icon => 49;
    public override int Length => 32767;
    public override string Name => "Secured Position";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(30, false));
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        if (aisling.HeldPosition == null || aisling.HeldPosition.X != aisling.Position.X && aisling.HeldPosition.Y != aisling.Position.Y)
            OnEnded(affected, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        var item = new Item();
        item.ReapplyItemModifiers(aisling.Client);
        aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, $"Position compromised");
        aisling.Client.SendEffect(byte.MinValue, Icon);
        DeleteBuff(aisling, buff);
    }
}

#endregion

#region Fas

public class BuffArdFasNadur : Buff
{
    public override byte Icon => 219;
    public override int Length => 640;
    public override string Name => "Ard Fas Nadur";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.Amplified = 2.5;
        }

        if (affected is not Aisling player) return;
        InsertBuff(player, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
        affected.Amplified = 2.5;
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.Amplified = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer amplified.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}

public class BuffMorFasNadur : Buff
{
    public override byte Icon => 218;
    public override int Length => 320;
    public override string Name => "Mor Fas Nadur";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.Amplified = 2;
        }

        if (affected is not Aisling player) return;
        InsertBuff(player, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
        affected.Amplified = 2;
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.Amplified = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer amplified.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}

public class BuffFasNadur : Buff
{
    public override byte Icon => 119;
    public override int Length => 120;
    public override string Name => "Fas Nadur";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
            affected.Amplified = 1.5;
        }

        if (affected is not Aisling player) return;
        InsertBuff(player, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        base.OnDurationUpdate(affected, buff);
        affected.Amplified = 1.5;
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.Amplified = 0;

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer amplified.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}

public class BuffFasSpiorad : Buff
{
    public override byte Icon => 26;
    public override int Length => 2;
    public override string Name => "Fas Spiorad";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(1, null, affected.Serial));

        var reduce = aisling.MaximumMp * 0.50;
        if (aisling.CurrentHp - reduce <= 0)
        {
            aisling.CurrentHp = 1;
        }
        else
            aisling.CurrentHp -= (long)reduce;

        aisling.CurrentMp = aisling.MaximumMp;

        InsertBuff(aisling, buff);
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendSound(26, false));
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendEffect(byte.MinValue, Icon);
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "The impact to your body dissipates.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendAttributes(StatUpdateType.Full);
    }
}
#endregion

#region Experience

public class BuffDoubleExperience : Buff
{
    public override byte Icon => 144;
    public override int Length => 7200;
    public override string Name => "Double XP";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Experience has been doubled for two hours.");
        InsertBuff(aisling, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer gaining double experience.");
        DeleteBuff(aisling, buff);
    }
}

public class BuffTripleExperience : Buff
{
    public override byte Icon => 145;
    public override int Length => 7200;
    public override string Name => "Triple XP";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "Experience has been tripled for two hours.");
        InsertBuff(aisling, buff);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, "You're no longer gaining tripled experience.");
        DeleteBuff(aisling, buff);
    }
}

#endregion