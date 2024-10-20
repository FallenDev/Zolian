using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Spells;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Types;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using Darkages.Sprites.Abstractions;
using MapFlags = Darkages.Enums.MapFlags;
using Darkages.Network.Server;

namespace Darkages.Sprites;

public abstract class Sprite : ObjectManager, INotifyPropertyChanged, ISprite
{
    public bool Abyss;
    public Position LastPosition;
    public List<List<TileGrid>> MasterGrid = [];
    public event PropertyChangedEventHandler PropertyChanged;
    public readonly WorldServerTimer BuffAndDebuffTimer;
    public readonly Stopwatch MonsterBuffAndDebuffStopWatch = new();
    private readonly Stopwatch _threatControl = new();
    private readonly object _walkLock = new();

    public bool Alive => CurrentHp > 1;
    public bool Attackable => this is Monster || this is Aisling;
    public bool Summoned;

    public Aisling PlayerNearby => AislingsNearby().FirstOrDefault();

    #region Buffs Debuffs

    private int _frozenStack;
    public bool IsWeakened => CurrentHp <= MaximumHp * .05;
    public bool IsAited => HasBuff("Aite") || HasBuff("Dia Aite");
    public bool Immunity => HasBuff("Dion") || HasBuff("Mor Dion") || HasBuff("Ard Dion") || HasBuff("Stone Skin") ||
                            HasBuff("Iron Skin") || HasBuff("Wings of Protection");
    public bool Hastened => HasBuff("Hastenga") || HasBuff("Hasten") || HasBuff("Haste");
    public bool SpellReflect => HasBuff("Deireas Faileas");
    public bool SpellNegate => HasBuff("Perfect Defense") || this is Aisling { GameMaster: true };
    public bool SkillReflect => HasBuff("Asgall") || this is Aisling { GameMaster: true };
    public bool IsBlind => HasDebuff("Blind");
    public bool IsConfused => HasDebuff("Confused");
    public bool IsSilenced => HasDebuff("Silence");
    public bool IsFrozen => HasDebuff("Frozen") || HasDebuff("Adv Frozen") || HasDebuff("Dark Chain");
    public bool IsStopped => HasDebuff("Halt");
    public bool IsBeagParalyzed => HasDebuff("Beag Suain");
    public bool IsPoisoned => HasDebuff(i => i.Name.Contains("Puinsein") || HasDebuff("Deadly Poison"));
    public bool IsSleeping => HasDebuff("Sleep");
    public bool IsInvisible => HasBuff("Hide") || HasBuff("Shadowfade");
    public bool DrunkenFist => HasBuff("Drunken Fist");
    private bool NinthGateReleased => HasBuff("Ninth Gate Release");
    private bool Berserk => HasBuff("Berserker Rage");
    public bool IsEnhancingSecondaryOffense => HasBuff("Atlantean Weapon");
    private bool IsCharmed => HasDebuff("Entice");
    private bool IsBleeding => HasDebuff("Bleeding");
    public bool IsCradhed => HasDebuff(i => i.Name.Contains("Cradh"));
    public bool IsVulnerable => IsFrozen || IsStopped || IsBlind || IsSleeping || Berserk || IsCharmed || IsWeakened || HasDebuff("Decay");
    public bool IsBlocked => IsFrozen || IsStopped || IsSleeping;

    public bool ClawFistEmpowerment { get; set; }
    public bool CanSeeInvisible
    {
        get
        {
            if (this is not Monster monster) return HasBuff("Shadow Sight");
            var canSee = monster.Scripts.TryGetValue("Aosda Remnant", out _);
            if (canSee) return true;
            canSee = monster.Scripts.TryGetValue("ShadowSight", out _);
            if (canSee) return true;
            canSee = monster.Scripts.TryGetValue("Weak ShadowSight", out _);
            return canSee || HasBuff("Shadow Sight");
        }
    }

    #endregion

    public bool CantCast => IsFrozen || IsStopped || IsSleeping || IsSilenced;
    public bool CantAttack => IsFrozen || IsStopped || IsSleeping || IsCharmed;
    public bool CantMove => IsFrozen || IsStopped || IsSleeping || IsBeagParalyzed;
    public bool HasDoT => IsBleeding || IsPoisoned;
    private long CheckHp => Math.Clamp(BaseHp + BonusHp, 0, long.MaxValue);
    public long MaximumHp => Math.Clamp(CheckHp, 0, long.MaxValue);
    private long CheckMp => Math.Clamp(BaseMp + BonusMp, 0, long.MaxValue);
    public long MaximumMp => Math.Clamp(CheckMp, 0, long.MaxValue);
    public int Regen => (_Regen + BonusRegen).IntClamp(1, 150);
    public int Dmg => _Dmg + BonusDmg;
    public double SealedModifier { get; set; }
    public int SealedAc
    {
        get
        {
            switch (Ac)
            {
                case > 0:
                    if (SealedModifier == 0) return Ac;
                    return (int)(Ac * SealedModifier);
                case <= 0:
                    if (SealedModifier == 0) return Ac;
                    return Ac - (int)(Math.Abs(Ac) * SealedModifier);
            }
        }
    }
    private int AcFromDex => (Dex / 15).IntClamp(0, 500);
    private int Ac => (_ac + BonusAc + AcFromDex).IntClamp(-200, 500);
    private double _fortitude => (Con * 0.1).DoubleClamp(0, 90);
    public double Fortitude => Math.Round(_fortitude + BonusFortitude, 2);
    private double _reflex => (Hit * 0.1).DoubleClamp(0, 90);
    public double Reflex => Math.Round(_reflex, 2);
    private double _will => (Mr * 0.14).DoubleClamp(0, 80);
    public double Will => Math.Round(_will, 2);
    public int Hit => _Hit + BonusHit;
    private int Mr => _Mr + BonusMr;
    public int Str => (_Str + BonusStr).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Int => (_Int + BonusInt).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Wis => (_Wis + BonusWis).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Con => (_Con + BonusCon).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Dex => (_Dex + BonusDex).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Luck => _Luck + BonusLuck;

    public Area Map => ServerSetup.Instance.GlobalMapCache.GetValueOrDefault(CurrentMapId);

    public Position Position => new(Pos);

    public ushort Level => TileType switch
    {
        TileContent.Aisling => ((Aisling)this).ExpLevel,
        TileContent.Monster => (ushort)(((Monster)this).Template.Level + ((Monster)this).SummonerAdjLevel),
        TileContent.Item => ((Item)this).Template.LevelRequired,
        _ => 0
    };

    private static readonly int[][] Directions =
    [
        [+0, -1],
        [+1, +0],
        [+0, +1],
        [-1, +0]
    ];

    private static int[][] DirectionTable { get; } =
    [
        [-1, +3, -1],
        [+0, -1, +2],
        [-1, +1, -1]
    ];

    private double TargetDistance { get; set; }

    protected Sprite()
    {
        if (this is Aisling)
            TileType = TileContent.Aisling;
        if (this is Monster)
            TileType = TileContent.Monster;
        if (this is Mundane)
            TileType = TileContent.Mundane;
        if (this is Money)
            TileType = TileContent.Money;
        if (this is Item)
            TileType = TileContent.Item;
        var readyTime = DateTime.UtcNow;
        BuffAndDebuffTimer = new WorldServerTimer(TimeSpan.FromSeconds(1));
        Amplified = 0;
        SealedModifier = 0;
        Target = null;
        Buffs = [];
        Debuffs = [];
        LastTargetAcquired = readyTime;
        LastMovementChanged = readyTime;
        LastTurnUpdated = readyTime;
        LastUpdated = readyTime;
        LastPosition = new Position(Vector2.Zero);
    }

    public uint Serial { get; set; }
    public int CurrentMapId { get; set; }
    public double Amplified { get; set; }
    public ElementManager.Element OffenseElement { get; set; }
    public ElementManager.Element SecondaryOffensiveElement { get; set; }
    public ElementManager.Element DefenseElement { get; set; }
    public ElementManager.Element SecondaryDefensiveElement { get; set; }
    public DateTime AbandonedDate { get; set; }
    public Sprite Target { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public Vector2 Pos
    {
        get => new(X, Y);
        set
        {
            if (Pos == value) return;
            X = (int)value.X;
            Y = (int)value.Y;
            NotifyPropertyChanged();
        }
    }
    public TileContent TileType { get; set; }
    public byte Direction { get; set; }
    public int PendingX { get; set; }
    public int PendingY { get; set; }
    public DateTime LastMenuInvoked { get; set; }
    public DateTime LastMovementChanged { get; set; }
    public DateTime LastTargetAcquired { get; set; }
    public DateTime LastTurnUpdated { get; set; }
    public DateTime LastUpdated { get; set; }
    public PrimaryStat MajorAttribute { get; set; }
    public ConcurrentDictionary<string, Buff> Buffs { get; }
    public ConcurrentDictionary<string, Debuff> Debuffs { get; }

    #region Stats

    public long CurrentHp { get; set; }
    public long BaseHp { get; set; }
    public long BonusHp { get; set; }

    public long CurrentMp { get; set; }
    public long BaseMp { get; set; }
    public long BonusMp { get; set; }

    public int _Regen { get; set; }
    public int BonusRegen { get; set; }

    public int _Dmg { get; set; }
    public int BonusDmg { get; set; }

    public int BonusAc { get; set; }
    public int _ac { get; set; }

    public int BonusFortitude { get; set; }

    public int _Hit { get; set; }
    public int BonusHit { get; set; }

    public int _Mr { get; set; }
    public int BonusMr { get; set; }

    public int _Str { get; set; }
    public int BonusStr { get; set; }

    public int _Int { get; set; }
    public int BonusInt { get; set; }

    public int _Wis { get; set; }
    public int BonusWis { get; set; }

    public int _Con { get; set; }
    public int BonusCon { get; set; }

    public int _Dex { get; set; }
    public int BonusDex { get; set; }

    public int _Luck { get; set; }
    public int BonusLuck { get; set; }

    #endregion

    public bool CanBeAttackedHere(Sprite source)
    {
        if (source is not Aisling || this is not Aisling) return true;
        if (CurrentMapId <= 0 || !ServerSetup.Instance.GlobalMapCache.TryGetValue(CurrentMapId, out var value)) return true;

        return value.Flags.MapFlagIsSet(MapFlags.PlayerKill);
    }

    public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Identification & Position

    public TSprite CastSpriteToType<TSprite>() where TSprite : Sprite
    {
        return this as TSprite;
    }

    public void ShowTo(Aisling nearbyAisling)
    {
        if (nearbyAisling == null) return;
        if (this is Aisling aisling)
        {
            nearbyAisling.Client.SendDisplayAisling(aisling);
            aisling.SpritesInView.AddOrUpdate(nearbyAisling.Serial, nearbyAisling, (_, _) => nearbyAisling);
        }
        else
        {
            var sprite = new List<Sprite> { this };
            nearbyAisling.Client.SendVisibleEntities(sprite);
        }
    }

    public List<Sprite> GetInFrontToSide(int tileCount = 1)
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            case 0:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 1:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;

            case 2:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 3:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;
        }

        return results;
    }

    public List<Sprite> MonsterGetInFrontToSide(int tileCount = 1)
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            case 0:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 1:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;

            case 2:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                break;

            case 3:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                break;
        }

        return results;
    }

    public List<Sprite> MonsterGetFiveByFourRectInFront()
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            // North
            case 0:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 1));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 3));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 4));
                break;
            // East
            case 1:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 3, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 4, (int)Pos.Y - 2));
                break;
            // South
            case 2:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 1));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 3));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 3));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 1, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X + 2, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 4));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 4));
                break;
            // West
            case 3:
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 1, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 2, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 3, (int)Pos.Y - 2));

                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y + 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y + 2));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y - 1));
                results.AddRange(MonsterGetDamageableSprites((int)Pos.X - 4, (int)Pos.Y - 2));
                break;
        }

        return results;
    }

    public List<Sprite> GetHorizontalInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        switch (Direction)
        {
            case 0:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                break;

            case 1:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y - tileCount));
                break;

            case 2:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X + tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                break;

            case 3:
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y + tileCount));
                results.AddRange(AislingGetDamageableSprites((int)Pos.X - tileCount, (int)Pos.Y - tileCount));
                break;
        }

        return results;
    }

    public Position GetFromAllSidesEmpty(Sprite target, int tileCount = 1)
    {
        var empty = Position;
        var blocks = target.Position.SurroundingContent(Map);

        if (blocks.Length <= 0) return empty;

        var selections = blocks.Where(i => i.Content is TileContent.None or TileContent.Item or TileContent.Money);
        var selection = selections.MaxBy(i => i.Position.DistanceFrom(Position));

        if (selection != null)
            empty = selection.Position;

        return empty;
    }

    public List<Sprite> GetAllInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(GetSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(GetSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(GetSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(GetSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> DamageableGetInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Position> GetTilesInFront(int tileCount = 1)
    {
        var results = new List<Position>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.Add(new Position((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.Add(new Position((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.Add(new Position((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.Add(new Position((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> DamageableGetAwayInFront(int tileCount = 2)
    {
        var results = new List<Sprite>();

        for (var i = 2; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> DamageableGetBehind(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 1:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 3:
                    results.AddRange(AislingGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public List<Sprite> MonsterGetInFront(int tileCount = 1)
    {
        var results = new List<Sprite>();

        for (var i = 1; i <= tileCount; i++)
            switch (Direction)
            {
                case 0:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y - i));
                    break;

                case 1:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X + i, (int)Pos.Y));
                    break;

                case 2:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X, (int)Pos.Y + i));
                    break;

                case 3:
                    results.AddRange(MonsterGetDamageableSprites((int)Pos.X - i, (int)Pos.Y));
                    break;
            }

        return results;
    }

    public Position GetPendingChargePosition(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0)
                pendingY--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1)
                pendingX++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2)
                pendingY++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3)
                pendingX--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX++;
            break;
        }

        return new Position(pendingX, pendingY);
    }

    public Position GetPendingChargePositionNoTarget(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0)
                pendingY--;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1)
                pendingX++;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2)
                pendingY++;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingY--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3)
                pendingX--;
            if (sprite is Aisling aisling)
                aisling.Client.CheckWarpTransitions(aisling.Client, pendingX, pendingY);
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            pendingX++;
            break;
        }

        return new Position(pendingX, pendingY);
    }

    public Position GetPendingThrowPosition(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0)
                pendingY++;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingY--;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1)
                pendingX--;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingX++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2)
                pendingY--;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingY++;
            break;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3)
                pendingX++;
            if (!sprite.Map.IsWall(pendingX, pendingY))
            {
                var pos = new Position(pendingX, pendingY);
                PlayerNearby?.SendTargetedClientMethod(PlayerScope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
                continue;
            }
            pendingX--;
            break;
        }

        return new Position(pendingX, pendingY);
    }
    public bool GetPendingThrowIsWall(int warp, Sprite sprite)
    {
        var pendingX = X;
        var pendingY = Y;

        for (var i = 0; i < warp; i++)
        {
            if (Direction == 0) pendingY++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 1) pendingX--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 2) pendingY--;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }
        for (var i = 0; i < warp; i++)
        {
            if (Direction == 3) pendingX++;
            if (!sprite.Map.IsWall(pendingX, pendingY)) continue;
            return true;
        }

        return false;
    }


    private IEnumerable<Sprite> GetSprites(int x, int y) => GetObjects(Map, i => (int)i.Pos.X == x && (int)i.Pos.Y == y, Get.All);
    private IEnumerable<Sprite> AislingGetDamageableSprites(int x, int y) => GetObjects(Map, i => (int)i.Pos.X == x && (int)i.Pos.Y == y, Get.AislingDamage);
    private IEnumerable<Sprite> MonsterGetDamageableSprites(int x, int y) => GetObjects(Map, i => (int)i.Pos.X == x && (int)i.Pos.Y == y, Get.Monsters | Get.Aislings);
    public bool WithinRangeOf(Sprite other) => other != null && WithinRangeOf(other, ServerSetup.Instance.Config.WithinRangeProximity);
    public bool WithinEarShotOf(Sprite other) => other != null && WithinRangeOf(other, 14);
    public bool WithinMonsterSpellRangeOf(Sprite other) => other != null && WithinRangeOf(other, 10);
    public bool WithinRangeOf(Sprite other, int distance)
    {
        if (other == null) return false;
        return CurrentMapId == other.CurrentMapId && WithinDistanceOf((int)other.Pos.X, (int)other.Pos.Y, distance);
    }
    public bool WithinDistanceOf(int x, int y, int subjectLength) => DistanceFrom(x, y) < subjectLength;

    public Aisling[] AislingsNearby() => GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity)).ToArray();
    public Aisling[] AislingsEarShotNearby() => GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, 14)).ToArray();
    public Aisling[] AislingsOnMap() => GetObjects<Aisling>(Map, i => i != null && Map == i.Map).ToArray();
    public IEnumerable<Monster> MonstersNearby() => GetObjects<Monster>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));
    public IEnumerable<Monster> MonstersOnMap() => GetObjects<Monster>(Map, i => i != null);
    public IEnumerable<Mundane> MundanesNearby() => GetObjects<Mundane>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));

    public IEnumerable<Sprite> SpritesNearby()
    {
        var result = new List<Sprite>();
        var listA = GetObjects<Monster>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));
        var listB = GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));
        result.AddRange(listA);
        result.AddRange(listB);
        return result;
    }

    public bool NextTo(int x, int y)
    {
        var xDist = Math.Abs(x - X);
        var yDist = Math.Abs(y - Y);

        return xDist + yDist == 1;
    }

    private int DistanceFrom(int x, int y)
    {
        // Manhattan Distance
        return Math.Abs(X - x) + Math.Abs(Y - y);
    }

    public bool Facing(int x, int y, out int direction)
    {
        var xDist = (x - (int)Pos.X).IntClamp(-1, +1);
        var yDist = (y - (int)Pos.Y).IntClamp(-1, +1);

        direction = DirectionTable[xDist + 1][yDist + 1];
        return Direction == direction;
    }

    public bool FacingFarAway(int x, int y, out int direction)
    {
        var orgPos = Pos; // Sprites current position
        var xDiff = orgPos.X - x; // Difference between points
        var yDiff = orgPos.Y - y; // Difference between points
        var xGap = Math.Abs(xDiff); // Absolute value of point
        var yGap = Math.Abs(yDiff); // Absolute value of point

        // Determine which point has a greater distance
        if (xGap > yGap)
            switch (xDiff)
            {
                case <= -1: // East
                    direction = 1;
                    return Direction == direction;
                case >= 0: // West
                    direction = 3;
                    return Direction == direction;
            }

        switch (yDiff)
        {
            case <= -1: // South
                direction = 2;
                return Direction == direction;
            case >= 0: // North
                direction = 0;
                return Direction == direction;
        }

        direction = 0;
        return Direction == direction;
    }

    private bool CanAttack(Sprite attackingPlayer)
    {
        if (this is not Monster monster) return false;
        if (monster.Template.MonsterRace == MonsterRace.Dummy) return true;

        // If the dictionary is empty, add the player
        if (monster.TargetRecord.TaggedAislings.IsEmpty)
        {
            monster.TryAddPlayerAndHisGroup(attackingPlayer);
            return true;
        }

        return monster.TargetRecord.TaggedAislings.TryGetValue(attackingPlayer.Serial, out _) || monster.TryAddTagging(attackingPlayer);
    }

    #endregion

    #region Movement

    public bool Walk()
    {
        void Step0C(int x, int y)
        {
            var readyTime = DateTime.UtcNow;
            Pos = new Vector2(PendingX, PendingY);

            foreach (var player in AislingsNearby())
            {
                player.Client.SendCreatureWalk(Serial, new Point(x, y), (Direction)Direction);
            }

            LastMovementChanged = readyTime;
            LastPosition = new Position(x, y);
        }

        lock (_walkLock)
        {
            var currentPosX = X;
            var currentPosY = Y;

            PendingX = X;
            PendingY = Y;

            var allowGhostWalk = this is Aisling { GameMaster: true };

            if (this is Monster { Template: not null } monster)
            {
                allowGhostWalk = monster.Template.IgnoreCollision;
                if (monster.ThrownBack) return false;
            }

            // Check position before we add direction, add direction, check position to see if we can commit
            if (!allowGhostWalk)
            {
                if (Map.IsWall(currentPosX, currentPosY)) return false;
                if (Map.IsSpriteInLocationOnWalk(this, PendingX, PendingY)) return false;
            }

            switch (Direction)
            {
                case 0:
                    PendingY--;
                    break;
                case 1:
                    PendingX++;
                    break;
                case 2:
                    PendingY++;
                    break;
                case 3:
                    PendingX--;
                    break;
            }

            if (!allowGhostWalk)
            {
                if (Map.IsWall(PendingX, PendingY)) return false;
                if (Map.IsSpriteInLocationOnWalk(this, PendingX, PendingY)) return false;
            }

            // Commit Walk to other Player Clients
            Step0C(currentPosX, currentPosY);

            // Check Trap Activation
            if (this is Monster trapCheck)
                CheckTraps(trapCheck);

            // Reset our PendingX & PendingY
            PendingX = currentPosX;
            PendingY = currentPosY;

            return true;
        }
    }

    public bool WalkTo(int x, int y)
    {
        var buffer = new byte[2];
        var length = float.PositiveInfinity;
        var offset = 0;

        for (byte i = 0; i < 4; i++)
        {
            var newX = (int)Pos.X + Directions[i][0];
            var newY = (int)Pos.Y + Directions[i][1];
            var pos = new Vector2(newX, newY);

            if (this is Monster { AStar: false })
            {
                if ((int)pos.X == x && (int)pos.Y == y) return false;
            }

            try
            {
                if (Map.IsWall((int)pos.X, (int)pos.Y)) continue;
                if (Map.IsSpriteInLocationOnWalk(this, (int)pos.X, (int)pos.Y)) continue;
            }
            catch (Exception ex)
            {
                ServerSetup.EventsLogger($"{ex}\nUnknown exception in WalkTo method.");
                SentrySdk.CaptureException(ex);
            }

            var xDist = x - (int)pos.X;
            var yDist = y - (int)pos.Y;

            // Chebyshev Distance
            TargetDistance = Math.Max(Math.Abs(xDist), Math.Abs(yDist));

            if (length < TargetDistance) continue;

            if (length > TargetDistance)
            {
                length = (float)TargetDistance;
                offset = 0;
            }

            if (offset < buffer.Length)
                buffer[offset] = i;

            offset++;
        }

        if (offset == 0) return false;
        var r = Random.Shared.Next(0, offset) % buffer.Length;
        if (r < 0 || buffer.Length <= r) return Walk();
        var pendingDirection = buffer[r];
        Direction = pendingDirection;

        return Walk();
    }

    public void Wander()
    {
        if (!CanUpdate()) return;

        var savedDirection = Direction;
        var update = false;

        Direction = (byte)RandomNumberGenerator.GetInt32(5);
        if (Direction != savedDirection) update = true;

        if (Walk() || !update) return;

        foreach (var player in AislingsNearby())
        {
            player?.Client.SendCreatureTurn(Serial, (Direction)Direction);
        }

        LastTurnUpdated = DateTime.UtcNow;
    }

    public void CheckTraps(Monster monster)
    {
        foreach (var trap in ServerSetup.Instance.Traps.Values.Where(t => t.TrapItem.Map.ID == monster.Map.ID))
        {
            if (trap.Owner == null || trap.Owner.Serial == monster.Serial ||
                monster.X != trap.Location.X || monster.Y != trap.Location.Y) continue;

            var triggered = Trap.Activate(trap, monster);
            if (!triggered) continue;
            ServerSetup.Instance.Traps.TryRemove(trap.Serial, out _);
            break;
        }
    }

    public void Turn()
    {
        if (!CanUpdate()) return;

        foreach (var player in AislingsNearby())
        {
            player?.Client.SendCreatureTurn(Serial, (Direction)Direction);
        }

        LastTurnUpdated = DateTime.UtcNow;
    }

    #endregion

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

        //ApplyAffliction(this, damageDealingSprite);

        // Apply modifiers for attacker
        dmg = ApplyPhysicalModifier();

        if (damageDealingSprite is Aisling aisling)
        {
            dmg = ApplyBehindTargetMod();
            dmg = ApplyWeaponBonuses(damageDealingSprite, dmg);
            if (damageDealingSprite.ClawFistEmpowerment)
                dmg = (long)(dmg * 1.3);
            if (this is Monster monster && monster.Template.MonsterRace == aisling.FavoredEnemy)
                dmg *= 2;
        }

        // Check vulnerable and proc variances
        dmg = Vulnerable(dmg);
        VarianceProc(damageDealingSprite, dmg);

        // Apply modifiers for defender
        if (this is Aisling defender)
        {
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
            if (damageDealingSprite is not Aisling aisling) return dmg;
            if (aisling.Client.IsBehind(this))
                dmg += (long)((dmg + ServerSetup.Instance.Config.BehindDamageMod) / 1.99);
            return dmg;
        }

        long PainBane(Aisling aisling)
        {
            if (aisling.PainBane)
                return (long)(dmg * 0.95);
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
            if (damageDealingSprite is not Aisling aisling) return dmg;
            if (aisling.PainBane)
                return (long)(dmg * 0.95);
            return dmg;
        }
    }

    #endregion

    #region Physical Damage Application

    public bool DamageTarget(Sprite damageDealingSprite, ref long dmg, byte sound, bool forced)
    {
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

    #region Status

    public void UpdateBuffs(Sprite sprite)
    {
        var buffs = Buffs.Values;

        foreach(var b in buffs)
        {
            if (sprite is Aisling aisling)
            {
                aisling.Client.EnqueueBuffUpdatedEvent(sprite, b);
                StatusBarDisplayUpdateBuff(b);
            }
            else
            {
                if (Alive)
                    b.Update(sprite);
            }
        }
    }

    public void UpdateDebuffs(Sprite sprite)
    {
        var debuffs = Debuffs.Values;

        foreach(var d in debuffs)
        {
            if (sprite is Aisling aisling)
            {
                aisling.Client.EnqueueDebuffUpdatedEvent(sprite, d);
                StatusBarDisplayUpdateDebuff(d);
            }
            else
            {
                if (Alive)
                    d.Update(sprite);
            }
        }
    }

    public void StatusBarDisplayUpdateBuff(Buff buff)
    {
        if (this is not Aisling aisling) return;
        var colorInt = byte.MinValue;

        if (buff.TimeLeft.IntIsWithin(-3, 0))
            colorInt = (byte)StatusBarColor.Off;
        else if (buff.TimeLeft.IntIsWithin(1, 10))
            colorInt = (byte)StatusBarColor.Blue;
        else if (buff.TimeLeft.IntIsWithin(11, 30))
            colorInt = (byte)StatusBarColor.Green;
        else if (buff.TimeLeft.IntIsWithin(31, 45))
            colorInt = (byte)StatusBarColor.Yellow;
        else if (buff.TimeLeft.IntIsWithin(46, 60))
            colorInt = (byte)StatusBarColor.Orange;
        else if (buff.TimeLeft.IntIsWithin(61, 120))
            colorInt = (byte)StatusBarColor.Red;
        else if (buff.TimeLeft.IntIsWithin(121, int.MaxValue))
            colorInt = (byte)StatusBarColor.White;

        aisling.Client.SendEffect((EffectColor)colorInt, buff.Icon);
    }

    public void StatusBarDisplayUpdateDebuff(Debuff debuff)
    {
        if (this is not Aisling aisling) return;
        var colorInt = byte.MinValue;

        if (debuff.TimeLeft.IntIsWithin(-3, 0))
            colorInt = (byte)StatusBarColor.Off;
        else if (debuff.TimeLeft.IntIsWithin(1, 10))
            colorInt = (byte)StatusBarColor.Blue;
        else if (debuff.TimeLeft.IntIsWithin(11, 30))
            colorInt = (byte)StatusBarColor.Green;
        else if (debuff.TimeLeft.IntIsWithin(31, 45))
            colorInt = (byte)StatusBarColor.Yellow;
        else if (debuff.TimeLeft.IntIsWithin(46, 60))
            colorInt = (byte)StatusBarColor.Orange;
        else if (debuff.TimeLeft.IntIsWithin(61, 120))
            colorInt = (byte)StatusBarColor.Red;
        else if (debuff.TimeLeft.IntIsWithin(121, int.MaxValue))
            colorInt = (byte)StatusBarColor.White;

        aisling.Client.SendEffect((EffectColor)colorInt, debuff.Icon);
    }

    /// <summary>
    /// Reduces threat to player over time. Also resets the persistent message.
    /// </summary>
    public void ThreatGeneratedSubsided(Aisling aisling)
    {
        var time = false;
        if (!_threatControl.IsRunning)
        {
            _threatControl.Start();
        }

        if (!aisling.ThreatTimer.Disabled)
        {
            time = _threatControl.Elapsed.TotalSeconds > aisling.ThreatTimer.Delay.TotalSeconds;
        }

        if (!time) return;
        if (aisling.MonsterKillCounters.Values.Any(killRecord => DateTime.UtcNow.Subtract(killRecord.TimeKilled) <= TimeSpan.FromSeconds(60))) return;

        _threatControl.Restart();
        aisling.ThreatMeter = 0;
        aisling.Client.SendServerMessage(ServerMessageType.PersistentMessage, "");
    }

    private bool CanUpdate()
    {
        if (CantMove || IsBlind) return false;

        if (this is Monster || this is Mundane)
            if (CurrentHp == 0)
                return false;

        if (ServerSetup.Instance.Config.CanMoveDuringReap) return true;

        if (this is not Aisling { Skulled: true } aisling) return true;

        aisling.Client.SystemMessage(ServerSetup.Instance.Config.ReapMessageDuringAction);
        return false;
    }

    public bool HasBuff(string buff)
    {
        if (Buffs == null || Buffs.IsEmpty)
            return false;

        return Buffs.ContainsKey(buff);
    }

    public bool HasDebuff(string debuff)
    {
        if (Debuffs == null || Debuffs.IsEmpty)
            return false;

        return Debuffs.ContainsKey(debuff);
    }

    public bool HasDebuff(Func<Debuff, bool> p)
    {
        if (Debuffs == null || Debuffs.IsEmpty)
            return false;

        return Debuffs.Select(i => i.Value).FirstOrDefault(p) != null;
    }

    public string GetDebuffName(Func<Debuff, bool> p)
    {
        if (Debuffs == null || Debuffs.IsEmpty)
            return string.Empty;

        return Debuffs.Select(i => i.Value)
            .FirstOrDefault(p)
            ?.Name;
    }

    public void RemoveAllBuffs()
    {
        if (Buffs == null)
            return;

        foreach (var buff in Buffs)
            RemoveBuff(buff.Key);
    }

    public void RemoveAllDebuffs()
    {
        if (Debuffs == null)
            return;

        foreach (var debuff in Debuffs)
            RemoveDebuff(debuff.Key);
    }

    public bool RemoveBuff(string buff)
    {
        if (!HasBuff(buff)) return false;

        lock (Buffs)
        {
            try
            {
                var buffObj = Buffs[buff];
                buffObj?.OnEnded(this, buffObj);
            }
            catch
            {
                // ignored
            }
        }

        return true;
    }

    public void RemoveBuffsAndDebuffs()
    {
        RemoveAllBuffs();
        RemoveAllDebuffs();
    }

    public bool RemoveDebuff(string debuff, bool cancelled = false)
    {
        if (!cancelled && debuff == "Skulled") return true;
        if (!HasDebuff(debuff)) return false;

        lock (Debuffs)
        {
            try
            {
                var debuffObj = Debuffs[debuff];
                if (debuffObj != null)
                {
                    debuffObj.Cancelled = cancelled;
                    debuffObj.OnEnded(this, debuffObj);
                }
            }
            catch
            {
                // ignored
            }
        }

        return true;
    }

    #endregion

    #region Sprite Methods

    public void UpdateAddAndRemove()
    {
        foreach (var playerNearby in AislingsEarShotNearby())
        {
            uint objectId;

            if (this is Item item)
                objectId = item.ItemVisibilityId;
            else
                objectId = Serial;

            playerNearby.Client.SendRemoveObject(objectId);
            var obj = new List<Sprite> { this };
            playerNearby.Client.SendVisibleEntities(obj);
        }
    }

    public void Remove()
    {
        var nearby = AislingsEarShotNearby();
        uint objectId;

        if (this is Item item)
            objectId = item.ItemVisibilityId;
        else
            objectId = Serial;
        
        foreach (var o in nearby)
            o?.Client?.SendRemoveObject(objectId);

        DeleteObject();
    }

    public void HideFrom(Aisling nearbyAisling)
    {
        uint objectId;

        if (this is Item item)
            objectId = item.ItemVisibilityId;
        else
            objectId = Serial;

        nearbyAisling.Client.SendRemoveObject(objectId);
    }

    private void DeleteObject()
    {
        if (this is Monster)
            DelObject(this as Monster);
        if (this is Aisling)
            DelObject(this as Aisling);
        if (this is Money)
            DelObject(this as Money);
        if (this is Item)
            DelObject(this as Item);
        if (this is Mundane)
            DelObject(this as Mundane);
    }

    #endregion
}