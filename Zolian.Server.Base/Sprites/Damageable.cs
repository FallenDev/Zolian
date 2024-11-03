using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Spells;
using Darkages.ScriptingBase;
using Darkages.Types;
using MapFlags = Darkages.Enums.MapFlags;
using Darkages.Network.Server;

namespace Darkages.Sprites;

public class Damageable : Movable
{
    #region Initial Damage Application
    // Entry methods to all damage to sprite

    public void ApplyElementalSpellDamage(Sprite source, long dmg, ElementManager.Element element, Spell spell)
    {
        var saved = source.OffenseElement;
        source.OffenseElement = element;

        if (this is Aisling aisling)
        {
            if (aisling.FireImmunity && source.OffenseElement == ElementManager.Element.Fire)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bFire damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.WaterImmunity && source.OffenseElement == ElementManager.Element.Water)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=eWater damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.EarthImmunity && source.OffenseElement == ElementManager.Element.Earth)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=rEarth damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.WindImmunity && source.OffenseElement == ElementManager.Element.Wind)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=hWind damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.DarkImmunity && source.OffenseElement == ElementManager.Element.Void)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=nDark damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.LightImmunity && source.OffenseElement == ElementManager.Element.Holy)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=uLight damage negated");
                source.OffenseElement = saved;
                return;
            }
        }

        MagicApplyDamage(source, dmg, spell);
        source.OffenseElement = saved;
    }

    public void ApplyElementalSkillDamage(Sprite source, long dmg, ElementManager.Element element, Skill skill)
    {
        var saved = source.OffenseElement;

        source.OffenseElement = element;
        if (this is Aisling aisling)
        {
            if (aisling.FireImmunity && source.OffenseElement == ElementManager.Element.Fire)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=bFire damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.WaterImmunity && source.OffenseElement == ElementManager.Element.Water)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=eWater damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.EarthImmunity && source.OffenseElement == ElementManager.Element.Earth)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=rEarth damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.WindImmunity && source.OffenseElement == ElementManager.Element.Wind)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=hWind damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.DarkImmunity && source.OffenseElement == ElementManager.Element.Void)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=nDark damage negated");
                source.OffenseElement = saved;
                return;
            }
            if (aisling.LightImmunity && source.OffenseElement == ElementManager.Element.Holy)
            {
                aisling.Client.SendServerMessage(ServerMessageType.ActiveMessage, "{=uLight damage negated");
                source.OffenseElement = saved;
                return;
            }
        }

        ApplyDamage(source, dmg, skill);
        source.OffenseElement = saved;
    }

    public void ApplyDamage(Sprite damageDealingSprite, long dmg, Skill skill, bool forceTarget = false)
    {
        if (!WithinRangeOf(damageDealingSprite)) return;
        if (!Attackable) return;
        if (!CanBeAttackedHere(damageDealingSprite)) return;

        if (OffenseElement != ElementManager.Element.None)
        {
            dmg += (long)GetBaseDamage(damageDealingSprite, this, MonsterEnums.Elemental);
        }
        else
        {
            dmg += (long)GetBaseDamage(damageDealingSprite, this, MonsterEnums.Physical);
        }

        // Apply modifiers for attacker
        dmg = ApplyPhysicalModifier();

        if (damageDealingSprite is Aisling aisling)
        {
            dmg = ApplyBehindTargetMod();
            dmg = ApplyWeaponBonuses(damageDealingSprite, dmg);
            if (damageDealingSprite.ClawFistEmpowerment)
                dmg = (long)(dmg * 1.3);
            if (damageDealingSprite.HardenedHands)
                dmg *= 2;
            if (this is Monster monster && monster.Template.MonsterRace == aisling.FavoredEnemy)
                dmg *= 2;
        }

        // Check vulnerable and proc variances
        dmg = Vulnerable(dmg);
        VarianceProc(damageDealingSprite, dmg);

        // Apply modifiers for defender
        if (this is Aisling defender)
        {
            dmg = CraneStance(defender);
            dmg = PainBane(defender);
            dmg = ApplyPvpMod();
        }

        if (skill == null)
        {
            // Thrown weapon scripts play the swoosh sound #9
            if (!DamageTarget(damageDealingSprite, ref dmg, 9, forceTarget)) return;
        }
        else
        {
            if (skill.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.IgnoreDefense) ||
                skill.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.Both))
                forceTarget = true;
            if (!DamageTarget(damageDealingSprite, ref dmg, skill.Template.Sound, forceTarget)) return;
        }

        // Apply consequences
        Thorns(damageDealingSprite, dmg);

        // Run OnDamaged scripts
        OnDamaged(damageDealingSprite, dmg);
        return;

        long ApplyPhysicalModifier()
        {
            var dmgAboveAcModifier = damageDealingSprite.Str * 0.25;
            dmgAboveAcModifier /= 100;
            var dmgAboveAcBoost = dmgAboveAcModifier * dmg;
            dmg += (long)dmgAboveAcBoost;
            return dmg;
        }

        long ApplyPvpMod()
        {
            if (Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
                dmg = (long)(dmg * 0.75);
            return dmg;
        }

        long ApplyBehindTargetMod()
        {
            if (damageDealingSprite is not Aisling aisling2) return dmg;
            if (aisling2.Client.IsBehind(this))
                dmg += (long)((dmg + ServerSetup.Instance.Config.BehindDamageMod) / 1.99);
            return dmg;
        }

        long PainBane(Aisling aisling2)
        {
            if (aisling2.PainBane)
                return (long)(dmg * 0.95);
            return dmg;
        }

        long CraneStance(Aisling aisling2)
        {
            if (aisling2.CraneStance)
                return (long)(dmg * 0.85);
            return dmg;
        }
    }

    public void ApplyTrapDamage(Sprite damageDealingSprite, long dmg, byte sound)
    {
        if (!Attackable) return;

        if (Immunity)
        {
            PlayerNearby?.Client.SendHealthBar(this, sound);
            return;
        }

        if (this is Aisling)
        {
            dmg = CraneStance();
            dmg = ApplyPvpMod();
            dmg = PainBane();
        }

        if (IsAited && dmg > 100)
            dmg -= (long)(dmg * ServerSetup.Instance.Config.AiteDamageReductionMod);

        dmg = LuckModifier(dmg);

        if (CurrentHp > MaximumHp)
            CurrentHp = MaximumHp;

        CurrentHp -= dmg;

        if (damageDealingSprite is Aisling aisling)
        {
            var time = DateTime.UtcNow;
            var estTime = time.TimeOfDay;
            aisling.DamageCounter += dmg;
            if (aisling.ThreatMeter + dmg >= long.MaxValue)
                aisling.ThreatMeter = 500000;
            aisling.ThreatMeter += dmg;
            aisling.ThreatTimer = new WorldServerTimer(TimeSpan.FromSeconds(60));
            ShowDmg(aisling, estTime);
        }

        if (this is Aisling damagedPlayer)
        {
            damagedPlayer.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendHealthBar(this, sound));
            if (CurrentHp <= 0)
                damagedPlayer.Client.DeathStatusCheck();
        }
        else
            PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendHealthBar(this, sound));

        if (dmg > 50)
            ApplyEquipmentDurability(dmg);

        OnDamaged(damageDealingSprite, dmg);
        return;

        long ApplyPvpMod()
        {
            if (Map.Flags.MapFlagIsSet(MapFlags.PlayerKill))
                dmg = (long)(dmg * 0.75);
            return dmg;
        }

        long PainBane()
        {
            if (damageDealingSprite is not Aisling aisling2) return dmg;
            if (aisling2.PainBane)
                return (long)(dmg * 0.95);
            return dmg;
        }

        long CraneStance()
        {
            if (this is not Aisling aisling2) return dmg;
            if (aisling2.CraneStance)
                return (long)(dmg * 0.85);
            return dmg;
        }
    }

    public void MagicApplyDamage(Sprite damageDealingSprite, long dmg, Spell spell, bool forceTarget = false)
    {
        if (!WithinRangeOf(damageDealingSprite)) return;
        if (!Attackable) return;
        if (!CanBeAttackedHere(damageDealingSprite)) return;

        if (OffenseElement != ElementManager.Element.None)
        {
            dmg += (long)GetBaseDamage(damageDealingSprite, this, MonsterEnums.Elemental);
        }
        else
        {
            dmg += (long)GetBaseDamage(damageDealingSprite, this, MonsterEnums.Physical);
        }

        dmg = ApplyMagicalModifier();

        if (damageDealingSprite is Aisling aisling)
        {
            dmg = PainBane();
            dmg = ApplyWeaponBonuses(damageDealingSprite, dmg);
            if (this is Monster monster && monster.Template.MonsterRace == aisling.FavoredEnemy)
                dmg *= 2;
        }

        dmg = Vulnerable(dmg);
        VarianceProc(damageDealingSprite, dmg);

        if (this is Aisling)
            dmg = (long)(dmg * 0.50);

        if (spell == null)
        {
            if (!MagicDamageTarget(damageDealingSprite, ref dmg, 0, forceTarget)) return;
        }
        else
        {
            if (spell.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.IgnoreDefense) ||
                spell.Template.PostQualifiers.QualifierFlagIsSet(PostQualifier.Both))
                forceTarget = true;
            if (!MagicDamageTarget(damageDealingSprite, ref dmg, spell.Template.Sound, forceTarget)) return;
        }

        OnDamaged(damageDealingSprite, dmg);
        return;

        long ApplyMagicalModifier()
        {
            var dmgAboveAcModifier = damageDealingSprite.Int * 0.05;
            dmgAboveAcModifier /= 100;
            var dmgAboveAcBoost = dmgAboveAcModifier * dmg;
            dmg += (long)dmgAboveAcBoost;
            return dmg;
        }

        long PainBane()
        {
            if (damageDealingSprite is not Aisling aisling2) return dmg;
            if (aisling2.PainBane)
                return (long)(dmg * 0.95);
            return dmg;
        }
    }

    #endregion

    #region Physical Damage Application

    public bool DamageTarget(Sprite damageDealingSprite, ref long dmg, byte sound, bool forced)
    {
        if (damageDealingSprite.IsBlind)
        {
            var negateAttack = Random.Shared.Next(0, 100);
            if (negateAttack >= 75)
                return false;
        }

        if (this is Monster monster)
        {
            if (damageDealingSprite is Aisling aisling)
                if (!CanAttack(aisling))
                {
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.CantAttack}");
                    return false;
                }

            if (monster.Camouflage)
                dmg = (long)(dmg * .90);
        }

        if (Immunity && !forced)
        {
            PlayerNearby?.Client.SendHealthBar(this, sound);
            return false;
        }

        if (IsAited && dmg > 100)
            dmg -= (int)(dmg * ServerSetup.Instance.Config.AiteDamageReductionMod);

        double secondary = 0;
        var weak = false;

        if (damageDealingSprite.SecondaryOffensiveElement != ElementManager.Element.None)
        {
            secondary = GetElementalModifier(damageDealingSprite, true);
            if (secondary < 1.0) weak = true;
            secondary /= 2;
        }

        var amplifier = GetElementalModifier(damageDealingSprite);
        {
            if (weak)
                amplifier -= secondary;
            else
                amplifier += secondary;
        }

        dmg = LuckModifier(dmg);
        dmg = ComputeDmgFromAc(dmg);

        if (DrunkenFist)
            dmg -= (int)(dmg * 0.25);

        if (damageDealingSprite.DrunkenFist)
            dmg = (int)(dmg * 1.25);

        if (damageDealingSprite.NinthGateReleased)
            dmg *= 3;

        if (damageDealingSprite.Berserk)
            dmg *= 2;

        dmg = CompleteDamageApplication(damageDealingSprite, dmg, sound, amplifier);
        var convDmg = (int)dmg;

        if (convDmg > 0)
            ApplyEquipmentDurability(convDmg);

        return true;
    }

    public long ComputeDmgFromAc(long dmg)
    {
        var script = ScriptManager.Load<FormulaScript>(ServerSetup.Instance.Config.ACFormulaScript, this);

        return script?.Aggregate(dmg, (current, s) => s.Value.Calculate(this, current)) ?? dmg;
    }

    #endregion

    #region Magical Damage Application

    public bool MagicDamageTarget(Sprite damageDealingSprite, ref long dmg, byte sound, bool forced)
    {
        if (damageDealingSprite.IsBlind)
        {
            var negateAttack = Random.Shared.Next(0, 100);
            if (negateAttack >= 75)
                return false;
        }

        if (this is Monster monster)
        {
            if (damageDealingSprite is Aisling aisling)
                if (!CanAttack(aisling))
                {
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.CantAttack}");
                    return false;
                }

            if (monster.Camouflage)
                dmg = (long)(dmg * .90);
        }

        if (Immunity && !forced)
        {
            PlayerNearby?.Client.SendHealthBar(this, sound);
            return false;
        }

        if (IsAited && dmg > 100)
            dmg -= (int)(dmg * ServerSetup.Instance.Config.AiteDamageReductionMod);

        double secondary = 0;
        var weak = false;

        if (damageDealingSprite.SecondaryOffensiveElement != ElementManager.Element.None)
        {
            secondary = GetElementalModifier(damageDealingSprite, true);
            if (secondary < 1.0) weak = true;
            secondary /= 2;
        }

        var amplifier = GetElementalModifier(damageDealingSprite);
        {
            if (weak)
                amplifier -= secondary;
            else
                amplifier += secondary;
        }

        dmg = LuckModifier(dmg);
        dmg = ComputeDmgFromWillSavingThrow(dmg);

        if (DrunkenFist)
            dmg -= (int)(dmg * 0.25);

        if (damageDealingSprite.Berserk)
            dmg *= 2;

        dmg = CompleteDamageApplication(damageDealingSprite, dmg, sound, amplifier);
        var convDmg = (int)dmg;

        if (convDmg > 0)
            ApplyEquipmentDurability(convDmg);

        return true;
    }

    public long ComputeDmgFromWillSavingThrow(long dmg)
    {
        var script = ScriptManager.Load<FormulaScript>("Will Saving Throw", this);

        return script?.Aggregate(dmg, (current, s) => s.Value.Calculate(this, current)) ?? dmg;
    }

    #endregion

    #region Damage Application Helper Methods
    // Methods below are in order as per execution

    public double GetBaseDamage(Sprite damageDealingSprite, Sprite target, MonsterEnums type)
    {
        var script = ScriptManager.Load<DamageFormulaScript>(ServerSetup.Instance.Config.BaseDamageScript, this, target, type);
        return script?.Values.Sum(s => s.Calculate(damageDealingSprite, target, type)) ?? 1;
    }

    public long Vulnerable(long dmg)
    {
        if (!IsVulnerable)
        {
            double hit = Generator.RandNumGen100();
            double fort = Generator.RandNumGen100();

            if (hit <= Reflex)
            {
                PlayerNearby?.Client.SendHealthBar(this);
                if (this is not Aisling aisling) return dmg;
                aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(92, aisling.Position));
            }

            if (fort <= Fortitude)
            {
                dmg = (int)(dmg * 0.33);
            }

            return dmg;
        }

        dmg *= 2;

        // Unbreakable Frozen returns damage
        if (HasDebuff("Adv Frozen")) return dmg;

        // Sleep gets removed on hit
        if (IsSleeping) RemoveDebuff("Sleep");

        // Weak Frozen status gets removed after five successful hits
        if (!IsFrozen) return dmg;

        _frozenStack += 1;
        if (_frozenStack <= 4) return dmg;

        if (HasDebuff("Frozen"))
            RemoveDebuff("Frozen");
        if (HasDebuff("Dark Chain"))
            RemoveDebuff("Dark Chain");

        // Reset Frozen Stack
        _frozenStack = 0;

        return dmg;
    }

    public long ApplyWeaponBonuses(Sprite source, long dmg)
    {
        if (source is not Aisling aisling) return dmg;

        if (aisling.DualWield && aisling.EquipmentManager.Equipment[3] != null && aisling.EquipmentManager.Equipment[3].Item.Template.ScriptName == "Weapon")
        {
            var weapon2 = aisling.EquipmentManager.Equipment[3].Item;
            long dmg2 = 0;

            switch (weapon2.GearEnhancement)
            {
                default:
                case Item.GearEnhancements.None:
                    dmg2 += Random.Shared.Next(
                        (weapon2.Template.DmgMin + aisling.Dmg) * 1,
                        (weapon2.Template.DmgMax + aisling.Dmg) * 5);
                    break;
                case Item.GearEnhancements.One:
                    var min1 = weapon2.Template.DmgMin * 0.04;
                    var max1 = weapon2.Template.DmgMax * 0.04;
                    dmg2 += Random.Shared.Next(
                        ((int)(weapon2.Template.DmgMin + min1) + aisling.Dmg) * 1,
                        ((int)(weapon2.Template.DmgMax + max1) + aisling.Dmg) * 5);
                    break;
                case Item.GearEnhancements.Two:
                    var min2 = weapon2.Template.DmgMin * 0.08;
                    var max2 = weapon2.Template.DmgMax * 0.08;
                    dmg2 += Random.Shared.Next(
                        ((int)(weapon2.Template.DmgMin + min2) + aisling.Dmg) * 1,
                        ((int)(weapon2.Template.DmgMax + max2) + aisling.Dmg) * 5);
                    break;
                case Item.GearEnhancements.Three:
                    var min3 = weapon2.Template.DmgMin * 0.12;
                    var max3 = weapon2.Template.DmgMax * 0.12;
                    dmg2 += Random.Shared.Next(
                        ((int)(weapon2.Template.DmgMin + min3) + aisling.Dmg) * 1,
                        ((int)(weapon2.Template.DmgMax + max3) + aisling.Dmg) * 5);
                    break;
                case Item.GearEnhancements.Four:
                    var min4 = weapon2.Template.DmgMin * 0.16;
                    var max4 = weapon2.Template.DmgMax * 0.16;
                    dmg2 += Random.Shared.Next(
                        ((int)(weapon2.Template.DmgMin + min4) + aisling.Dmg) * 1,
                        ((int)(weapon2.Template.DmgMax + max4) + aisling.Dmg) * 5);
                    break;
                case Item.GearEnhancements.Five:
                    var min5 = weapon2.Template.DmgMin * 0.20;
                    var max5 = weapon2.Template.DmgMax * 0.20;
                    dmg2 += Random.Shared.Next(
                        ((int)(weapon2.Template.DmgMin + min5) + aisling.Dmg) * 1,
                        ((int)(weapon2.Template.DmgMax + max5) + aisling.Dmg) * 5);
                    break;
                case Item.GearEnhancements.Six:
                    var min6 = weapon2.Template.DmgMin * 0.25;
                    var max6 = weapon2.Template.DmgMax * 0.25;
                    dmg2 += Random.Shared.Next(
                        ((int)(weapon2.Template.DmgMin + min6) + aisling.Dmg) * 1,
                        ((int)(weapon2.Template.DmgMax + max6) + aisling.Dmg) * 5);
                    break;
            }

            dmg2 /= 2;
            dmg += dmg2;
        }

        if (aisling.EquipmentManager.Equipment[1] == null) return dmg;
        var weapon = aisling.EquipmentManager.Equipment[1].Item;

        switch (weapon.GearEnhancement)
        {
            default:
            case Item.GearEnhancements.None:
                dmg += Random.Shared.Next(
                    (weapon.Template.DmgMin + aisling.Dmg) * 1,
                    (weapon.Template.DmgMax + aisling.Dmg) * 5);
                break;
            case Item.GearEnhancements.One:
                var min1 = weapon.Template.DmgMin * 0.04;
                var max1 = weapon.Template.DmgMax * 0.04;
                dmg += Random.Shared.Next(
                    ((int)(weapon.Template.DmgMin + min1) + aisling.Dmg) * 1,
                    ((int)(weapon.Template.DmgMax + max1) + aisling.Dmg) * 5);
                break;
            case Item.GearEnhancements.Two:
                var min2 = weapon.Template.DmgMin * 0.08;
                var max2 = weapon.Template.DmgMax * 0.08;
                dmg += Random.Shared.Next(
                    ((int)(weapon.Template.DmgMin + min2) + aisling.Dmg) * 1,
                    ((int)(weapon.Template.DmgMax + max2) + aisling.Dmg) * 5);
                break;
            case Item.GearEnhancements.Three:
                var min3 = weapon.Template.DmgMin * 0.12;
                var max3 = weapon.Template.DmgMax * 0.12;
                dmg += Random.Shared.Next(
                    ((int)(weapon.Template.DmgMin + min3) + aisling.Dmg) * 1,
                    ((int)(weapon.Template.DmgMax + max3) + aisling.Dmg) * 5);
                break;
            case Item.GearEnhancements.Four:
                var min4 = weapon.Template.DmgMin * 0.16;
                var max4 = weapon.Template.DmgMax * 0.16;
                dmg += Random.Shared.Next(
                    ((int)(weapon.Template.DmgMin + min4) + aisling.Dmg) * 1,
                    ((int)(weapon.Template.DmgMax + max4) + aisling.Dmg) * 5);
                break;
            case Item.GearEnhancements.Five:
                var min5 = weapon.Template.DmgMin * 0.20;
                var max5 = weapon.Template.DmgMax * 0.20;
                dmg += Random.Shared.Next(
                    ((int)(weapon.Template.DmgMin + min5) + aisling.Dmg) * 1,
                    ((int)(weapon.Template.DmgMax + max5) + aisling.Dmg) * 5);
                break;
            case Item.GearEnhancements.Six:
                var min6 = weapon.Template.DmgMin * 0.25;
                var max6 = weapon.Template.DmgMax * 0.25;
                dmg += Random.Shared.Next(
                    ((int)(weapon.Template.DmgMin + min6) + aisling.Dmg) * 1,
                    ((int)(weapon.Template.DmgMax + max6) + aisling.Dmg) * 5);
                break;
        }

        return dmg;
    }

    public void VarianceProc(Sprite sprite, long dmg)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        var enemy = client.Aisling.DamageableGetInFront();
        var target = enemy.FirstOrDefault();
        var aegisChance = Generator.RandNumGen100();
        var bleedingChance = Generator.RandNumGen100();
        var rendingChance = Generator.RandNumGen100();
        var vampChance = Generator.RandNumGen100();
        var reapChance = Generator.RandomNumPercentGen();
        var hasteChance = Generator.RandNumGen100();
        var gustChance = Generator.RandNumGen100();
        var quakeChance = Generator.RandNumGen100();
        var rainChance = Generator.RandNumGen100();
        var flameChance = Generator.RandNumGen100();
        var duskChance = Generator.RandNumGen100();
        var dawnChance = Generator.RandNumGen100();

        switch (damageDealingSprite.Aegis)
        {
            case 1 when aegisChance >= 99:
                {
                    var buff = new buff_spell_reflect();
                    if (!damageDealingSprite.HasBuff(buff.Name)) damageDealingSprite.Client.EnqueueBuffAppliedEvent(damageDealingSprite, buff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "A flash of light surrounds you, shielding you.");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(83, null, damageDealingSprite.Serial));
                    break;
                }
            case 2 when aegisChance >= 97:
                {
                    var buff = new buff_spell_reflect();
                    if (!damageDealingSprite.HasBuff(buff.Name)) damageDealingSprite.Client.EnqueueBuffAppliedEvent(damageDealingSprite, buff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "A flash of light surrounds you, shielding you.");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(83, null, damageDealingSprite.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Haste)
        {
            case 1 when hasteChance >= 99:
                {
                    var buff = new buff_Haste();
                    damageDealingSprite.Client.EnqueueBuffAppliedEvent(damageDealingSprite, buff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Things begin to slow down around you.");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(291, null, damageDealingSprite.Serial));
                    break;
                }
            case 2 when hasteChance >= 97:
                {
                    var buff = new buff_Hasten();
                    damageDealingSprite.Client.EnqueueBuffAppliedEvent(damageDealingSprite, buff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Things begin to really slow down around you.");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(291, null, damageDealingSprite.Serial));
                    break;
                }
        }

        // The below procs are separated out by these checks to reduce complexity
        if (target == null) return;
        if (damageDealingSprite.Vampirism == 0 && damageDealingSprite.Rending == 0 && damageDealingSprite.Bleeding == 0 && damageDealingSprite.Reaping == 0
            && damageDealingSprite.Gust == 0 && damageDealingSprite.Quake == 0 && damageDealingSprite.Rain == 0
            && damageDealingSprite.Flame == 0 && damageDealingSprite.Dusk == 0 && damageDealingSprite.Dawn == 0) return;

        switch (damageDealingSprite.Vampirism)
        {
            case 1 when vampChance >= 99:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Vampiring doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 15 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Vampiring doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    const double absorbPct = 0.07;
                    var absorb = absorbPct * dmg;
                    damageDealingSprite.CurrentHp += (int)absorb;
                    if (target.CurrentHp >= (int)absorb)
                        target.CurrentHp -= (int)absorb;
                    client.SendAttributes(StatUpdateType.Vitality);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon is hungry....life force.. - it whispers");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(324, null, damageDealingSprite.Serial));
                    break;
                }
            case 2 when vampChance >= 97:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Vampiring doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 20 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Vampiring doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    const double absorbPct = 0.14;
                    var absorb = absorbPct * dmg;
                    damageDealingSprite.CurrentHp += (int)absorb;
                    if (target.CurrentHp >= (int)absorb)
                        target.CurrentHp -= (int)absorb;
                    client.SendAttributes(StatUpdateType.Vitality);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon is hungry....life force.. - it whispers");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(324, null, damageDealingSprite.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Ghosting)
        {
            case 1 when vampChance >= 99:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Siphon doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 15 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Siphon doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    const double absorbPct = 0.07;
                    var absorb = absorbPct * dmg;
                    damageDealingSprite.CurrentMp += (int)absorb;
                    if (target.CurrentMp >= (int)absorb)
                        target.CurrentMp -= (int)absorb;
                    client.SendAttributes(StatUpdateType.Vitality);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon phases in and out of reality");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(61, null, damageDealingSprite.Serial));
                    break;
                }
            case 2 when vampChance >= 97:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Siphon doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 20 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Siphon doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    const double absorbPct = 0.14;
                    var absorb = absorbPct * dmg;
                    damageDealingSprite.CurrentMp += (int)absorb;
                    if (target.CurrentMp >= (int)absorb)
                        target.CurrentMp -= (int)absorb;
                    client.SendAttributes(StatUpdateType.Vitality);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon phases in and out of reality");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(61, null, damageDealingSprite.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Bleeding)
        {
            case 1 when bleedingChance >= 99:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Bleeding doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 15 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Bleeding doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    var deBuff = new DebuffBleeding();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "The enemy has begun to bleed.");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(105, null, target.Serial));
                    break;
                }
            case 2 when bleedingChance >= 97:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Bleeding doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 20 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Bleeding doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    var deBuff = new DebuffBleeding();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "The enemy has begun to bleed.");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(105, null, target.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Rending)
        {
            case 1 when rendingChance >= 99:
                {
                    var deBuff = new DebuffRending();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You temporarily found a weakness! Exploit it!");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(160, null, target.Serial));
                    break;
                }
            case 2 when rendingChance >= 97:
                {
                    var deBuff = new DebuffRending();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You temporarily found a weakness! Exploit it!");
                    damageDealingSprite.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(160, null, target.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Reaping)
        {
            case 1 when reapChance >= 0.999:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 15 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    var deBuff = new DebuffReaping();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've cast Death.");
                    break;
                }
            case 2 when reapChance >= 0.995:
                {
                    switch (target)
                    {
                        case Aisling:
                        case Monster monster when monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Boss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.MiniBoss)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineDex)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineCon)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineWis)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineInt)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.DivineStr)
                                                  || monster.Template.MonsterType.MonsterTypeIsSet(MonsterType.Forsaken):
                            client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to work on them");
                            return;
                    }

                    if (target.Level >= 20 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    var deBuff = new DebuffReaping();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've cast Death.");
                    break;
                }
        }

        switch (damageDealingSprite.Gust)
        {
            case 1 when gustChance >= 98:
                _ = new Gust(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Gust seal breaks!");
                break;
            case 2 when gustChance >= 95:
                _ = new Gust(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Gust seal breaks!");
                break;
        }

        switch (damageDealingSprite.Quake)
        {
            case 1 when quakeChance >= 98:
                _ = new Quake(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Quake seal breaks!");
                break;
            case 2 when quakeChance >= 95:
                _ = new Quake(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Quake seal breaks!");
                break;
        }

        switch (damageDealingSprite.Rain)
        {
            case 1 when rainChance >= 98:
                _ = new Rain(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Rain seal breaks!");
                break;
            case 2 when rainChance >= 95:
                _ = new Rain(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Rain seal breaks!");
                break;
        }

        switch (damageDealingSprite.Flame)
        {
            case 1 when flameChance >= 98:
                _ = new Flame(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Flame seal breaks!");
                break;
            case 2 when flameChance >= 95:
                _ = new Flame(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Flame seal breaks!");
                break;
        }

        switch (damageDealingSprite.Dusk)
        {
            case 1 when duskChance >= 98:
                _ = new Dusk(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Dusk seal breaks!");
                break;
            case 2 when duskChance >= 95:
                _ = new Dusk(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Dusk seal breaks!");
                break;
        }

        switch (damageDealingSprite.Dawn)
        {
            case 1 when dawnChance >= 98:
                _ = new Dawn(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Dawn seal breaks!");
                break;
            case 2 when dawnChance >= 95:
                _ = new Dawn(damageDealingSprite, target);
                client.SendServerMessage(ServerMessageType.ActiveMessage, "Dawn seal breaks!");
                break;
        }
    }

    public double GetElementalModifier(Sprite damageDealingSprite, bool isSecondary = false)
    {
        if (damageDealingSprite == null) return 1;

        if (!isSecondary)
        {
            var offense = damageDealingSprite.OffenseElement;
            var defense = DefenseElement;

            // Calc takes the sprite and sends the attackers offense element
            var amplifier = CalculateElementalDamageMod(damageDealingSprite, offense);
            {
                DefenseElement = defense;
            }

            if (damageDealingSprite.Amplified == 0) return amplifier;

            amplifier *= damageDealingSprite.Amplified;

            return amplifier;
        }
        else
        {
            var offense = damageDealingSprite.SecondaryOffensiveElement;
            var defense = DefenseElement;

            var amplifier = CalculateElementalDamageMod(damageDealingSprite, offense);
            {
                DefenseElement = defense;
            }

            if (damageDealingSprite.Amplified == 0) return amplifier;

            amplifier *= damageDealingSprite.Amplified;

            return amplifier;
        }
    }

    public double CalculateElementalDamageMod(Sprite attacker, ElementManager.Element element)
    {
        var script = ScriptManager.Load<ElementFormulaScript>(ServerSetup.Instance.Config.ElementTableScript, this);
        return script?.Values.Sum(s => s.Calculate(this, attacker, element)) ?? 0.0;
    }

    private long LuckModifier(long dmg)
    {
        if (Luck <= 0) return dmg;
        long mod;

        switch (Luck)
        {
            case >= 1 and <= 5:
                mod = (long)(dmg * 0.03);
                dmg -= mod;
                break;
            case <= 10:
                mod = (long)(dmg * 0.05);
                dmg -= mod;
                break;
            case <= 15:
                mod = (long)(dmg * 0.07);
                dmg -= mod;
                break;
            case <= 16:
                mod = (long)(dmg * 0.10);
                dmg -= mod;
                break;
        }

        return dmg;
    }

    private long LevelDamageMitigation(Sprite damageDealingSprite, long dmg)
    {
        if (Level <= damageDealingSprite.Level) return dmg;
        var diff = Level - damageDealingSprite.Level;

        switch (diff)
        {
            case >= 10 and < 25:
                dmg = (long)(dmg * .60);
                break;
            case >= 25 and < 50:
                dmg = (long)(dmg * .45);
                break;
            case >= 50 and < 75:
                dmg = (long)(dmg * .30);
                break;
            case >= 75:
                dmg = (long)(dmg * .15);
                break;
            default:
                return dmg;
        }

        return dmg;
    }

    public void Thorns(Sprite damageDealingSprite, long dmg)
    {
        if (damageDealingSprite is null) return;
        if (this is not Aisling aisling) return;
        if (aisling.Spikes == 0) return;

        var thornsDmg = aisling.Spikes * 0.03;
        Math.Clamp(thornsDmg, 1, int.MaxValue);
        dmg = (long)(thornsDmg * dmg);

        if (dmg > int.MaxValue)
        {
            dmg = int.MaxValue;
        }

        var convDmg = (int)dmg;
        aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, client => client.SendAnimation(163, damageDealingSprite.Position));
        damageDealingSprite.CurrentHp -= convDmg;
    }

    #endregion

    #region Complete Damage Application

    public long CompleteDamageApplication(Sprite damageDealingSprite, long dmg, byte sound, double amplifier)
    {
        if (dmg <= 0) dmg = 1;

        if (CurrentHp > MaximumHp)
            CurrentHp = MaximumHp;

        var dmgApplied = (long)Math.Abs(dmg * amplifier);
        var finalDmg = LevelDamageMitigation(damageDealingSprite, dmgApplied);
        CurrentHp -= finalDmg;

        if (this is Aisling aisling)
        {
            aisling.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendHealthBar(this, sound));
            if (CurrentHp <= 0)
                aisling.Client.DeathStatusCheck();
        }
        else
            PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendHealthBar(this, sound));

        return finalDmg;
    }

    public void ApplyEquipmentDurability(long dmg)
    {
        if (this is Aisling aisling && aisling.EquipmentDamageTaken++ % 2 == 0 && dmg > 100)
            aisling.EquipmentManager.DecreaseDurability();
    }

    public void OnDamaged(Sprite source, long dmg)
    {
        (this as Aisling)?.Client.SendAttributes(StatUpdateType.Vitality);
        if (source is not Aisling aisling) return;

        var time = DateTime.UtcNow;
        var estTime = time.TimeOfDay;
        aisling.DamageCounter += dmg;
        if (aisling.ThreatMeter + dmg >= long.MaxValue)
            aisling.ThreatMeter = (long)(long.MaxValue * .95);
        aisling.ThreatMeter += dmg;
        if (aisling.GameSettings.DmgNumbers)
            ShowDmg(aisling, estTime);

        if (this is not Monster monster) return;
        if (monster.Template?.ScriptName == null) return;
        monster.Scripts?.First().Value.OnDamaged(aisling.Client, dmg, source);
    }

    public void ShowDmg(Aisling aisling, TimeSpan elapsedTime)
    {
        if (!aisling.AttackDmgTrack.Update(elapsedTime)) return;
        aisling.AttackDmgTrack.Delay = elapsedTime + TimeSpan.FromSeconds(1);

        var dmgShow = aisling.DamageCounter.ToString();
        aisling.Client.SendPublicMessage(aisling.Serial, PublicMessageType.Chant, $"{dmgShow}");
        aisling.DamageCounter = 0;
    }

    #endregion
}