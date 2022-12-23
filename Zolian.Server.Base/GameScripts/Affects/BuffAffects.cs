using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Formats.Models.ServerFormats;
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
            aisling.Client.SendMessage(0x02, "You feel a sense of security");
            aisling.Client.SendAnimation(93, aisling.Client.Aisling, aisling.Client.Aisling.Target ?? aisling.Client.Aisling);
            aisling.Show(Scope.NearbyAislings, new ServerFormat19(30));
            InsertBuff(aisling, buff);
            aisling.Client.SendStats(StatusFlags.StructD);
        }
        else
        {
            var nearby = affected.GetObjects<Aisling>(affected.Map, i => i.WithinRangeOf(affected));

            foreach (var near in nearby)
            {
                near.Client.SendAnimation(93, affected, affected);
                near.Show(Scope.NearbyAislings, new ServerFormat19(30));
            }
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Your resolve returns to normal");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.StructD);
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
            aisling.Client.SendMessage(0x02, "You feel a sense of security");
            aisling.Client.SendAnimation(93, aisling.Client.Aisling, aisling.Client.Aisling.Target ?? aisling.Client.Aisling);
            aisling.Show(Scope.NearbyAislings, new ServerFormat19(30));
            InsertBuff(aisling, buff);
            aisling.Client.SendStats(StatusFlags.StructD);
        }
        else
        {
            var nearby = affected.GetObjects<Aisling>(affected.Map, i => i.WithinRangeOf(affected));

            foreach (var near in nearby)
            {
                near.Client.SendAnimation(93, affected, affected);
                near.Show(Scope.NearbyAislings, new ServerFormat19(30));
            }
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Your resolve returns to normal");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.StructD);
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
            aisling.Client.SendMessage(0x02, "Spectral Shield has strengthened your resolve.");
            aisling.Client.SendAnimation(262, aisling.Client.Aisling, aisling.Client.Aisling.Target ?? aisling.Client.Aisling);
            aisling.Show(Scope.NearbyAislings, new ServerFormat19(30));
            InsertBuff(aisling, buff);
            aisling.Client.SendStats(StatusFlags.StructD);
        }
        else
        {
            var nearby = affected.GetObjects<Aisling>(affected.Map, i => i.WithinRangeOf(affected));

            foreach (var near in nearby)
            {
                near.Client.SendAnimation(262, affected, affected);
                near.Show(Scope.NearbyAislings, new ServerFormat19(30));
            }
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusAc -= AcModifier.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Your resolve returns to normal.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.StructD);
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
            aisling.Client.SendMessage(0x02, "You're now aware of your surroundings.");
            aisling.Client.SendAnimation(89, aisling.Client.Aisling, aisling.Client.Aisling.Target ?? aisling.Client.Aisling);
            aisling.Show(Scope.NearbyAislings, new ServerFormat19(83));
            InsertBuff(aisling, buff);
            aisling.Client.SendStats(StatusFlags.StructD);
        }
        else
        {
            var nearby = affected.GetObjects<Aisling>(affected.Map, i => i.WithinRangeOf(affected));

            foreach (var near in nearby)
            {
                near.Client.SendAnimation(89, affected, affected);
                near.Show(Scope.NearbyAislings, new ServerFormat19(83));
            }
        }
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusAc -= AcModifier.Value;

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "You've grown complacent.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.StructD);
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

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(500);
        aisling.Client.SendMessage(0x02, "Everything starts to slow down around you")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000);
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Time goes back to normal");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
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

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "Your hands are empowered!")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.ClawFistEmpowerment = false;

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Your hands turn back to normal.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_wingsOfProtect : Buff
{
    public override byte Icon => 194;
    public override int Length => 27;
    public override string Name => "Wings of Protection";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "You've become almost impervious.")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "You're no longer impervious.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_ArdDion : Buff
{
    public override byte Icon => 194;
    public override int Length => 35;
    public override string Name => "Ard Dion";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "You've become almost impervious.")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "You're no longer impervious.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_MorDion : Buff
{
    public override byte Icon => 53;
    public override int Length => 20;
    public override string Name => "Mor Dion";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "You've become almost impervious.")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "You're no longer impervious.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_dion : Buff
{
    public override byte Icon => 53;
    public override int Length => 8;
    public override string Name => "Dion";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "You've become almost impervious.")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "You're no longer impervious.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_IronSkin : Buff
{
    public override byte Icon => 53;
    public override int Length => 35;
    public override string Name => "Iron Skin";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "Your skin turns to Iron!")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Your skin turns back to normal.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_StoneSkin : Buff
{
    public override byte Icon => 53;
    public override int Length => 25;
    public override string Name => "Stone Skin";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "Your skin turns to stone!")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Your skin turns back to normal.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_hide : Buff
{
    public override byte Icon => 10;
    public override int Length => 45;
    public override string Name => "Hide";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        var client = aisling.Client;
        if (client.Aisling == null || client.Aisling.Dead) return;

        client.Aisling.Invisible = true;
        client.SendMessage(0x02, "You blend in to the shadows.");
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(43));
        client.UpdateDisplay();
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        affected.Show(Scope.NearbyAislings, new ServerFormat29((uint)affected.Serial, (uint)affected.Serial, 0, 0, 100));
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        var client = aisling.Client;

        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        client.Aisling.Invisible = false;
        aisling.Client.SendMessage(0x02, "You've emerged from the shadows.");
        aisling.Client.SendStats(StatusFlags.MultiStat);
        client.UpdateDisplay();
        DeleteBuff(aisling, buff);
    }
}

public class buff_ShadowFade : Buff
{
    public override byte Icon => 10;
    public override int Length => 300;
    public override string Name => "Shadowfade";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        var client = aisling.Client;
        if (client.Aisling == null || client.Aisling.Dead) return;

        client.Aisling.Invisible = true;
        client.SendMessage(0x02, "You've faded into the dark.");
        aisling.Show(Scope.NearbyAislings, new ServerFormat19(43));
        client.UpdateDisplay();
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        affected.Show(Scope.NearbyAislings, new ServerFormat29((uint)affected.Serial, (uint)affected.Serial, 0, 0, 100));
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        var client = aisling.Client;
        
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        client.Aisling.Invisible = false;
        aisling.Client.SendMessage(0x02, "You've emerged from the shadows.");
        aisling.Client.SendStats(StatusFlags.MultiStat);
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

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, "Adrenaline starts pumping!")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusDex -= 15;

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "You begin to come back down from your high.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
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
            if (affected.SecondaryOffensiveElement == ElementManager.Element.None)
            {
                affected.SecondaryOffensiveElement = Generator.RandomEnumValue<ElementManager.Element>();
            }
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendMessage(0x02, $"Secondary Offensive element has changed {aisling.SecondaryOffensiveElement}")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.SecondaryOffensiveElement = ElementManager.Element.None;

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Element applied to your offense has worn off");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
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
        aisling.Client.SendMessage(0x02, $"Your resistance to all damage has been increased by 33%")
            .SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);
        affected.BonusFortitude -= 100;

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "You are no longer protected.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
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
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        aisling.Client.SendMessage(0x02, "Skills are no longer being reflected.");
        DeleteBuff(aisling, buff);
        aisling.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_spell_reflect : Buff
{
    public override byte Icon => 54;
    public override int Length => 12;
    public override string Name => "Deireas Faileas";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        affected.Client.SendMessage(0x02, "Spells are no longer reflecting.");
        DeleteBuff(aisling, buff);
        affected.Client.SendStats(StatusFlags.MultiStat);
    }
}

public class buff_PerfectDefense : Buff
{
    public override byte Icon => 178;
    public override int Length => 18;
    public override string Name => "Perfect Defense";

    public override void OnApplied(Sprite affected, Buff buff)
    {
        if (affected.Buffs.TryAdd(buff.Name, buff))
        {
            BuffSpell = buff;
            BuffSpell.TimeLeft = BuffSpell.Length;
        }

        if (affected is not Aisling aisling) return;
        aisling.Client.SendStats(StatusFlags.MultiStat);
        InsertBuff(aisling, buff);
    }

    public override void OnDurationUpdate(Sprite affected, Buff buff)
    {
        if (affected is not Aisling aisling) return;
        UpdateBuff(aisling);
    }

    public override void OnEnded(Sprite affected, Buff buff)
    {
        affected.Buffs.TryRemove(buff.Name, out _);

        if (affected is not Aisling aisling) return;
        aisling.Client.Send(new ServerFormat3A(Icon, byte.MinValue));
        affected.Client.SendMessage(0x02, "Spells are no longer being deflected.");
        DeleteBuff(aisling, buff);
        affected.Client.SendStats(StatusFlags.MultiStat);
    }
}

#endregion
