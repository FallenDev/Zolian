using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

using Chaos.Common.Definitions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;

using Darkages.Common;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Spells;
using Darkages.Interfaces;
using Darkages.Object;
using Darkages.ScriptingBase;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;

using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.Sprites;

public abstract class Sprite : ObjectManager, INotifyPropertyChanged, ISprite
{
    public bool Abyss;
    public Position LastPosition;
    public int RelicFinder;
    public event PropertyChangedEventHandler PropertyChanged;
    private readonly WorldServerTimer _buffAndDebuffTimer;
    private readonly object _walkLock = new();

    public bool Alive => CurrentHp > 1;
    public bool Attackable => this is Monster || this is Aisling;
    public Aisling PlayerNearby => AislingsNearby().FirstOrDefault();
    public Trackers Trackers { get; set; }

    #region Buffs Debuffs

    private int _frozenStack;
    public bool IsWeakened => (CurrentHp <= MaximumHp * .05);
    public bool IsAited => HasBuff("Aite") || HasBuff("Dia Aite");

    public bool Immunity => HasBuff("Dion") || HasBuff("Mor Dion") || HasBuff("Ard Dion") || HasBuff("Stone Skin") ||
                            HasBuff("Iron Skin") || HasBuff("Wings of Protection");

    public bool SpellReflect => HasBuff("Deireas Faileas");
    public bool SpellNegate => HasBuff("Perfect Defense") || this is Aisling { GameMaster: true };
    public bool SkillReflect => HasBuff("Asgall") || this is Aisling { GameMaster: true };
    public bool IsBleeding => HasDebuff("Bleeding");
    public bool IsBlind => HasDebuff("Blind");
    public bool IsConfused => HasDebuff("Confused");
    public bool IsSilenced => HasDebuff("Silence");
    public bool IsArmorReduced => HasDebuff(i => i.Name.Contains("Cradh") || i.Name.Contains("Dark Seal") || i.Name.Contains("Rend") || i.Name.Contains("Hurricane") || i.Name.Contains("Decay"));
    public bool IsFrozen => HasDebuff("Frozen") || HasDebuff("Dark Chain");
    public bool IsVulnerable => HasDebuff("Frozen") || HasDebuff("Dark Chain") || HasDebuff("Halt") || HasDebuff("Blind") || HasDebuff("Sleep");
    public bool IsStopped => HasDebuff("Halt");
    public bool IsCharmed => HasDebuff("Entice");
    public bool IsBeagParalyzed => HasDebuff("Beag Suain");
    public bool IsPoisoned => HasDebuff(i => i.Name.Contains("Puinsein"));
    public bool IsSleeping => HasDebuff("Sleep");
    public bool IsEnhancingSecondaryOffense => HasBuff("Atlantean Weapon");
    public bool IsInvisible => HasBuff("Hide") || HasBuff("Shadowfade");

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

    public bool CantCast => (IsFrozen || IsStopped || IsSleeping || IsSilenced);
    public bool CantAttack => (IsFrozen || IsStopped || IsSleeping || IsCharmed);
    public bool CantMove => (IsFrozen || IsStopped || IsSleeping || IsBeagParalyzed);
    public bool HasDoT => (IsBleeding || IsPoisoned);
    private int CheckHp => BaseHp + BonusHp;
    public int MaximumHp => Math.Clamp(CheckHp, 0, int.MaxValue);
    private int CheckMp => BaseMp + BonusMp;
    public int MaximumMp => Math.Clamp(CheckMp, 0, int.MaxValue);
    public int Regen => (_Regen + BonusRegen).IntClamp(1, 150);
    public int Dmg => _Dmg + BonusDmg;
    private int AcFromDex => (Dex / 8).IntClamp(0, 500);
    public int Ac => _ac + BonusAc + AcFromDex;
    private double _fortitude => Con * 0.2;
    public double Fortitude => Math.Round(_fortitude + BonusFortitude, 2);
    private double _reflex => Hit * 0.2;
    public double Reflex => Math.Round(_reflex, 2);
    private double _will => Mr * 0.2;
    public double Will => Math.Round(_will, 2);
    public byte Hit => (byte)(_Hit + BonusHit);
    private byte Mr => (byte)(_Mr + BonusMr);
    public int Str => (_Str + BonusStr).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Int => (_Int + BonusInt).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Wis => (_Wis + BonusWis).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Con => (_Con + BonusCon).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Dex => (_Dex + BonusDex).IntClamp(0, ServerSetup.Instance.Config.StatCap);
    public int Luck => _Luck + BonusLuck;

    public Area Map => ServerSetup.Instance.GlobalMapCache.TryGetValue(CurrentMapId, out var mapId)
        ? mapId
        : null;

    public Position Position => new(Pos);

    public ushort Level => TileType switch
    {
        TileContent.Aisling => ((Aisling)this).ExpLevel,
        TileContent.Monster => ((Monster)this).Template.Level,
        TileContent.Item => ((Item)this).Template.LevelRequired,
        _ => 0
    };

    private static readonly int[][] Directions =
    {
        new[] { +0, -1 },
        new[] { +1, +0 },
        new[] { +0, +1 },
        new[] { -1, +0 }
    };

    private static int[][] DirectionTable { get; } =
    {
        new[] { -1, +3, -1 },
        new[] { +0, -1, +2 },
        new[] { -1, +1, -1 }
    };

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
        _buffAndDebuffTimer = new WorldServerTimer(TimeSpan.FromSeconds(1));
        Amplified = 0;
        Target = null;
        Buffs = new ConcurrentDictionary<string, Buff>();
        Debuffs = new ConcurrentDictionary<string, Debuff>();
        LastTargetAcquired = readyTime;
        LastMovementChanged = readyTime;
        LastTurnUpdated = readyTime;
        LastUpdated = readyTime;
        LastPosition = new Position(Vector2.Zero);
        Trackers = new Trackers(TimeSpan.FromSeconds(1));
    }

    public uint Serial { get; set; }
    public int CurrentMapId { get; set; }
    public double Amplified { get; set; }
    public ElementManager.Element OffenseElement { get; set; }
    public ElementManager.Element SecondaryOffensiveElement { get; set; }
    public ElementManager.Element DefenseElement { get; set; }
    public ElementManager.Element SecondaryDefensiveElement { get; set; }
    public bool ClawFistEmpowerment { get; set; }
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
    public int GroupId { get; set; }
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

    public int CurrentHp { get; set; }
    public int BaseHp { get; set; }
    public int BonusHp { get; set; }

    public int CurrentMp { get; set; }
    public int BaseMp { get; set; }
    public int BonusMp { get; set; }

    public int _Regen { get; set; }
    public int BonusRegen { get; set; }

    public int _Dmg { get; set; }
    public int BonusDmg { get; set; }

    public int BonusAc { get; set; }
    public int _ac { get; set; }

    public int BonusFortitude { get; set; }

    public byte _Hit { get; set; }
    public byte BonusHit { get; set; }

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
        if (source is not Sprites.Aisling || this is not Sprites.Aisling) return true;
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
        }
        else
        {
            var sprite = new List<Sprite> { this };
            nearbyAisling.Client.SendVisibleEntities(sprite);
        }
    }

    private IEnumerable<Sprite> GetInFrontToSide(int tileCount = 1)
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

    private IEnumerable<Sprite> GetHorizontalInFront(int tileCount = 1)
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

    private Position GetFromAllSidesEmpty(Sprite target, int tileCount = 1)
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

    private IEnumerable<Sprite> GetAllInFront(int tileCount = 1)
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

    private IEnumerable<Sprite> DamageableGetInFront(int tileCount = 1)
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

    private IEnumerable<Position> GetTilesInFront(int tileCount = 1)
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

    private IEnumerable<Sprite> DamageableGetAwayInFront(int tileCount = 2)
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

    private IEnumerable<Sprite> DamageableGetBehind(int tileCount = 1)
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

    private IEnumerable<Sprite> MonsterGetInFront(int tileCount = 1)
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

    public List<Sprite> GetAllInFront(Sprite sprite, int tileCount = 1) => GetAllInFront(tileCount).Where(i => i != null && i.Serial != sprite.Serial).ToList();
    public List<Sprite> GetAllInFront(int tileCount = 1, bool intersect = false) => GetAllInFront(tileCount).ToList();
    public List<Sprite> DamageableGetInFront(int tileCount = 1, bool intersect = false) => DamageableGetInFront(tileCount).ToList();
    public List<Position> GetTilesInFront(int tileCount = 1, bool intersect = false) => GetTilesInFront(tileCount).ToList();
    public List<Sprite> DamageableGetAwayInFront(int tileCount = 2, bool intersect = false) => DamageableGetAwayInFront(tileCount).ToList();
    public List<Sprite> DamageableGetBehind(int tileCount = 1, bool intersect = false) => DamageableGetBehind(tileCount).ToList();
    public List<Sprite> MonsterGetInFront(int tileCount = 1, bool intersect = false) => MonsterGetInFront(tileCount).ToList();
    public List<Sprite> GetInFrontToSide(int tileCount = 1, bool intersect = false) => GetInFrontToSide(tileCount).ToList();
    public List<Sprite> GetHorizontalInFront(int tileCount = 1, bool intersect = false) => GetHorizontalInFront(tileCount).ToList();
    public Position GetFromAllSidesEmpty(Sprite sprite, Sprite target, int tileCount = 1) => GetFromAllSidesEmpty(target, tileCount);
    public Position GetFromAllSidesEmpty(Sprite target, int tileCount = 1, bool intersect = false) => GetFromAllSidesEmpty(target, tileCount);

    public Position GetPendingChargePosition(int warp, Sprite sprite)
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
                PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
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
                PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
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
                PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
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
                PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(197, pos, sprite.Serial));
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
    private IEnumerable<Sprite> MonsterGetDamageableSprites(int x, int y) => GetObjects(Map, i => (int)i.Pos.X == x && (int)i.Pos.Y == y, Get.MonsterDamage);
    public bool WithinRangeOf(Sprite other, bool checkMap = true) => other != null && WithinRangeOf(other, ServerSetup.Instance.Config.WithinRangeProximity, checkMap);
    public bool WithinEarShotOf(Sprite other, bool checkMap = true) => other != null && WithinRangeOf(other, 16, checkMap);
    public bool WithinRangeOf(int x, int y, int subjectLength) => DistanceFrom(x, y) < subjectLength;
    public bool WithinRangeOf(Sprite other, int distance, bool checkMap = true)
    {
        if (other == null) return false;
        if (!checkMap) return WithinRangeOf((int)other.Pos.X, (int)other.Pos.Y, distance);
        return CurrentMapId == other.CurrentMapId && WithinRangeOf((int)other.Pos.X, (int)other.Pos.Y, distance);
    }
    public bool TrapsAreNearby() => ServerSetup.Instance.Traps.Select(i => i.Value).Any(i => i.CurrentMapId == CurrentMapId);
    public Aisling[] AislingsNearby() => GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity)).ToArray();
    public Aisling[] AislingsEarShotNearby() => GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, 16)).ToArray();
    public IEnumerable<Monster> MonstersNearby() => GetObjects<Monster>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));
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

    private bool CanAttack(Sprite attackingPlayer, bool force = false)
    {
        if (force) return false;
        if (this is not Monster monster) return false;
        if (monster.TargetRecord.TaggedAislings.TryGetValue(attackingPlayer.Serial, out _)) return true;
        if (monster.Template.BaseName == "Training Dummy") return true;

        // If the dictionary is empty, add the player
        if (monster.TargetRecord.TaggedAislings.IsEmpty)
        {
            monster.TryAddPlayerAndHisGroup(attackingPlayer);
            return true;
        }

        var playerTagged = monster.TargetRecord.TaggedAislings.TryGetValue(attackingPlayer.Serial, out _);
        if (playerTagged) return true;

        return monster.TryAddTagging(attackingPlayer);
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
                ServerSetup.Logger($"{ex}\nUnknown exception in WalkTo method.");
                Crashes.TrackError(ex);
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

    #region Damage

    public long ComputeDmgFromAc(long dmg)
    {
        var script = ScriptManager.Load<FormulaScript>(ServerSetup.Instance.Config.ACFormulaScript, this);

        return script?.Aggregate(dmg, (current, s) => s.Value.Calculate(this, current)) ?? dmg;
    }

    public long ComputeDmgFromWillSavingThrow(long dmg)
    {
        var script = ScriptManager.Load<FormulaScript>("Will Saving Throw", this);

        return script?.Aggregate(dmg, (current, s) => s.Value.Calculate(this, current)) ?? dmg;
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
            case >= 6 and <= 10:
                mod = (long)(dmg * 0.05);
                dmg -= mod;
                break;
            case >= 11 and <= 15:
                mod = (long)(dmg * 0.07);
                dmg -= mod;
                break;
            case >= 16:
                mod = (long)(dmg * 0.10);
                dmg -= mod;
                break;
        }

        return dmg;
    }

    public void ApplyElementalSpellDamage(Sprite source, long dmg, ElementManager.Element element, Spell spell)
    {
        var saved = source.OffenseElement;
        {
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
    }

    public void ApplyElementalSkillDamage(Sprite source, long dmg, ElementManager.Element element, Skill skill)
    {
        var saved = source.OffenseElement;
        {
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

        if (damageDealingSprite is Aisling)
        {
            dmg = ApplyBehindTargetMod();
            dmg = ApplyWeaponBonuses(damageDealingSprite, dmg);
            if (damageDealingSprite.ClawFistEmpowerment)
                dmg = (long)(dmg * 1.3);
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
            dmg -= (int)(dmg * ServerSetup.Instance.Config.AiteDamageReductionMod);

        dmg = LuckModifier(dmg);

        if (CurrentHp > MaximumHp)
            CurrentHp = MaximumHp;

        // ToDo: Create logic for "Over Damage"

        CurrentHp -= (int)dmg;

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

        PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendHealthBar(this, sound));

        if (dmg > 50)
            ApplyEquipmentDurability((int)dmg);

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
            if (damageDealingSprite is not Aisling aisling) return dmg;
            if (aisling.PainBane)
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

        if (damageDealingSprite is Aisling)
        {
            dmg = PainBane();
            dmg = ApplyWeaponBonuses(damageDealingSprite, dmg);
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
                aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(92, aisling.Position));
            }

            if (fort <= Fortitude)
            {
                dmg = (int)(dmg * 0.33);
            }

            return dmg;
        }

        dmg *= 2;
        _frozenStack += 1;

        // Sleep gets removed on hit
        if (HasDebuff("Sleep")) RemoveDebuff("Sleep");

        // Frozen status gets removed after five successful hits
        if (!IsFrozen) return dmg;
        if (_frozenStack != 5) return dmg;
        if (HasDebuff("Frozen"))
            RemoveDebuff("Frozen");
        if (HasDebuff("Dark Chain"))
            RemoveDebuff("Dark Chain");

        _frozenStack = 0;
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
        aisling.SendTargetedClientMethod(Scope.NearbyAislings, client => client.SendAnimation(163, damageDealingSprite.Position));
        damageDealingSprite.CurrentHp -= convDmg;
    }

    public void VarianceProc(Sprite sprite, long dmg)
    {
        if (sprite is not Aisling damageDealingSprite) return;
        var client = damageDealingSprite.Client;

        var enemy = client.Aisling.DamageableGetInFront(1);
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
                    if (!damageDealingSprite.HasBuff(buff.Name)) damageDealingSprite.Client.EnqueueBuffAppliedEvent(target, buff, TimeSpan.FromSeconds(buff.Length));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "The effects of your weapon surround you.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(83, null, damageDealingSprite.Serial));
                    break;
                }
            case 2 when aegisChance >= 97:
                {
                    var buff = new buff_spell_reflect();
                    if (!damageDealingSprite.HasBuff(buff.Name)) damageDealingSprite.Client.EnqueueBuffAppliedEvent(target, buff, TimeSpan.FromSeconds(buff.Length));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "The effects of your weapon surround you.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(83, null, damageDealingSprite.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Vampirism)
        {
            case 1 when vampChance >= 99:
                {
                    const double absorbPct = 0.07;
                    var absorb = absorbPct * dmg;
                    damageDealingSprite.CurrentHp += (int)absorb;
                    client.SendAttributes(StatUpdateType.Vitality);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon is hungry....");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(324, null, damageDealingSprite.Serial));
                    break;
                }
            case 2 when vampChance >= 97:
                {
                    const double absorbPct = 0.14;
                    var absorb = absorbPct * dmg;
                    damageDealingSprite.CurrentHp += (int)absorb;
                    client.SendAttributes(StatUpdateType.Vitality);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon is hungry....");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(324, null, damageDealingSprite.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Haste)
        {
            case 1 when hasteChance >= 99:
                {
                    client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(750);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You begin to move faster.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(291, null, damageDealingSprite.Serial));
                    Task.Delay(5000).ContinueWith(ct => { client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000); });
                    break;
                }
            case 2 when hasteChance >= 97:
                {
                    client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(500);
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You begin to move faster.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(291, null, damageDealingSprite.Serial));
                    Task.Delay(5000).ContinueWith(ct => { client.SkillSpellTimer.Delay = TimeSpan.FromMilliseconds(1000); });
                    break;
                }
        }

        // The below procs are separated out by these checks to reduce complexity
        if (target == null) return;
        if (damageDealingSprite.Rending == 0 && damageDealingSprite.Bleeding == 0 && damageDealingSprite.Reaping == 0
            && damageDealingSprite.Gust == 0 && damageDealingSprite.Quake == 0 && damageDealingSprite.Rain == 0
            && damageDealingSprite.Flame == 0 && damageDealingSprite.Dusk == 0 && damageDealingSprite.Dawn == 0) return;

        switch (damageDealingSprite.Bleeding)
        {
            case 1 when bleedingChance >= 99:
                {
                    var deBuff = new DebuffBleeding();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff, TimeSpan.FromSeconds(deBuff.Length));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon has caused your target to bleed.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(105, null, target.Serial));
                    break;
                }
            case 2 when bleedingChance >= 97:
                {
                    var deBuff = new DebuffBleeding();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff, TimeSpan.FromSeconds(deBuff.Length));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "The weapon has caused your target to bleed.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(105, null, target.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Rending)
        {
            case 1 when rendingChance >= 99:
                {
                    var deBuff = new DebuffRending();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff, TimeSpan.FromSeconds(deBuff.Length));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon has inflicted a minor curse.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(160, null, target.Serial));
                    break;
                }
            case 2 when rendingChance >= 97:
                {
                    var deBuff = new DebuffRending();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff, TimeSpan.FromSeconds(deBuff.Length));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "Your weapon has inflicted a minor curse.");
                    damageDealingSprite.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendAnimation(160, null, target.Serial));
                    break;
                }
        }

        switch (damageDealingSprite.Reaping)
        {
            case 1 when reapChance >= 0.999:
                {
                    if (target is Aisling)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to work on them");
                        return;
                    }

                    if (target.Level >= 15 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    var deBuff = new DebuffReaping();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff, TimeSpan.FromSeconds(deBuff.Length));
                    client.SendServerMessage(ServerMessageType.ActiveMessage, "You've cast Death.");
                    break;
                }
            case 2 when reapChance >= 0.995:
                {
                    if (target is Aisling)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to work on them");
                        return;
                    }

                    if (target.Level >= 20 + damageDealingSprite.ExpLevel)
                    {
                        client.SendServerMessage(ServerMessageType.ActiveMessage, "Death doesn't seem to be effective. (Level too high)");
                        return;
                    }

                    var deBuff = new DebuffReaping();
                    if (!target.HasDebuff(deBuff.Name)) damageDealingSprite.Client.EnqueueDebuffAppliedEvent(target, deBuff, TimeSpan.FromSeconds(deBuff.Length));
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

    public long CompleteDamageApplication(Sprite damageDealingSprite, long dmg, byte sound, double amplifier)
    {
        if (dmg <= 0) dmg = 1;

        if (CurrentHp > MaximumHp)
            CurrentHp = MaximumHp;

        var dmgApplied = (long)Math.Abs(dmg * amplifier);
        var finalDmg = LevelDamageMitigation(damageDealingSprite, dmgApplied);

        // ToDo: Create logic for "Over Damage"
        if (finalDmg > int.MaxValue)
        {
            finalDmg = int.MaxValue;
        }

        var convDmg = (int)finalDmg;

        CurrentHp -= convDmg;

        if (damageDealingSprite is Aisling aisling)
        {
            var time = DateTime.UtcNow;
            var estTime = time.TimeOfDay;
            aisling.DamageCounter += convDmg;
            if (aisling.ThreatMeter + dmg >= long.MaxValue)
                aisling.ThreatMeter = 500000;
            aisling.ThreatMeter += dmg;
            aisling.ThreatTimer = new WorldServerTimer(TimeSpan.FromSeconds(60));
            ShowDmg(aisling, estTime);
        }

        PlayerNearby?.SendTargetedClientMethod(Scope.NearbyAislings, c => c.SendHealthBar(this, sound));

        return convDmg;
    }

    private long LevelDamageMitigation(Sprite damageDealingSprite, long dmg)
    {
        if (Level <= damageDealingSprite.Level) return dmg;

        var difference = Level - damageDealingSprite.Level;
        dmg = difference switch
        {
            >= 50 => (int)(dmg * 0.75),
            >= 25 => (int)(dmg * 0.85),
            >= 10 => (int)(dmg * 0.90),
            >= 5 => (int)(dmg * 0.95),
            _ => dmg
        };

        return dmg;
    }

    public void ShowDmg(Aisling aisling, TimeSpan elapsedTime)
    {
        if (!aisling.AttackDmgTrack.Update(elapsedTime)) return;
        aisling.AttackDmgTrack.Delay = elapsedTime + TimeSpan.FromSeconds(1);

        var dmgShow = aisling.DamageCounter.ToString();
        aisling.Client.SendPublicMessage(aisling.Serial, PublicMessageType.Chant, $"{dmgShow}");
        aisling.DamageCounter = 0;
    }

    public void ThreatGeneratedSubsided(Aisling aisling, TimeSpan elapsedTime)
    {
        var time = false;

        if (!aisling.ThreatTimer.Disabled)
        {
            time = aisling.ThreatTimer.Update(elapsedTime);
        }

        if (!time) return;
        aisling.ThreatMeter = 0;
        aisling.Client.SendServerMessage(ServerMessageType.PersistentMessage, "");
    }

    public bool DamageTarget(Sprite damageDealingSprite, ref long dmg, byte sound, bool forced)
    {
        if (this is Aisling aislingTarget)
        {
            if (aislingTarget.Path == Class.Peasant && aislingTarget.Map.ID == 3029)
            {
                aislingTarget.Client.SendHealthBar(this, sound);
                return false;
            }
        }

        if (this is Monster)
        {
            if (damageDealingSprite is Aisling aisling)
                if (!CanAttack(aisling, forced))
                {
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.CantAttack}");
                    return false;
                }
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
        dmg = CompleteDamageApplication(damageDealingSprite, dmg, sound, amplifier);
        var convDmg = (int)dmg;

        if (convDmg > 0)
            ApplyEquipmentDurability(convDmg);

        return true;
    }

    public bool MagicDamageTarget(Sprite damageDealingSprite, ref long dmg, byte sound, bool forced)
    {
        if (this is Aisling aislingTarget)
        {
            if (aislingTarget.Path == Class.Peasant && aislingTarget.Map.ID == 3029)
            {
                aislingTarget.Client.SendHealthBar(this, sound);
                return false;
            }
        }

        if (this is Monster)
        {
            if (damageDealingSprite is Aisling aisling)
                if (!CanAttack(aisling, forced))
                {
                    aisling.Client.SendServerMessage(ServerMessageType.OrangeBar1, $"{ServerSetup.Instance.Config.CantAttack}");
                    return false;
                }
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
        dmg = CompleteDamageApplication(damageDealingSprite, dmg, sound, amplifier);
        var convDmg = (int)dmg;

        if (convDmg > 0)
            ApplyEquipmentDurability(convDmg);

        return true;
    }

    public void ApplyEquipmentDurability(int dmg)
    {
        if (this is Aisling aisling && aisling.EquipmentDamageTaken++ % 2 == 0 && dmg > 100)
            aisling.EquipmentManager.DecreaseDurability();
    }

    public long ApplyWeaponBonuses(Sprite source, long dmg)
    {
        if (source is not Aisling aisling) return dmg;

        if (aisling.DualWield && aisling.EquipmentManager.Equipment[3] != null && aisling.EquipmentManager.Equipment[3].Item.Template.ScriptName == "Weapon")
        {
            var weapon2 = aisling.EquipmentManager.Equipment[3].Item;
            var dmg2 = Random.Shared.Next(
                (weapon2.Template.DmgMin + aisling.Dmg) * 1,
                (weapon2.Template.DmgMax + aisling.Dmg) * 5);
            dmg2 /= 2;
            dmg += dmg2;
        }

        if (aisling.EquipmentManager.Equipment[1] == null) return dmg;
        var weapon = aisling.EquipmentManager.Equipment[1].Item;
        dmg += Random.Shared.Next(
            (weapon.Template.DmgMin + aisling.Dmg) * 1,
            (weapon.Template.DmgMax + aisling.Dmg) * 5);

        return dmg;
    }

    public double CalculateElementalDamageMod(ElementManager.Element element)
    {
        var script = ScriptManager.Load<ElementFormulaScript>(ServerSetup.Instance.Config.ElementTableScript, this);
        return script?.Values.Sum(s => s.Calculate(this, element)) ?? 0.0;
    }

    public double GetBaseDamage(Sprite damageDealingSprite, Sprite target, MonsterEnums type)
    {
        var script = ScriptManager.Load<DamageFormulaScript>(ServerSetup.Instance.Config.BaseDamageScript, this, target, type);
        return script?.Values.Sum(s => s.Calculate(damageDealingSprite, target, type)) ?? 1;
    }

    public double GetElementalModifier(Sprite damageDealingSprite, bool isSecondary = false)
    {
        if (damageDealingSprite == null) return 1;

        if (!isSecondary)
        {
            var offense = damageDealingSprite.OffenseElement;
            var defense = DefenseElement;

            var amplifier = CalculateElementalDamageMod(offense);
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

            var amplifier = CalculateElementalDamageMod(offense);
            {
                DefenseElement = defense;
            }

            if (damageDealingSprite.Amplified == 0) return amplifier;

            amplifier *= damageDealingSprite.Amplified;

            return amplifier;
        }
    }

    public void OnDamaged(Sprite source, long dmg)
    {
        (this as Aisling)?.Client.SendAttributes(StatUpdateType.Vitality);

        if (this is not Monster monster) return;
        if (source is not Aisling aisling) return;
        if (monster.Template?.ScriptName == null) return;
        var scriptObj = monster.Scripts?.FirstOrDefault();
        scriptObj?.Value.OnDamaged(aisling.Client, dmg, source);
    }

    public string GetDebuffName(Func<Debuff, bool> p)
    {
        if (Debuffs == null || Debuffs.IsEmpty)
            return string.Empty;

        return Debuffs.Select(i => i.Value)
            .FirstOrDefault(p)
            ?.Name;
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
        var buffObj = Buffs[buff];
        buffObj?.OnEnded(this, buffObj);

        return true;
    }

    public void RemoveBuffsAndDebuffs()
    {
        RemoveAllBuffs();
        RemoveAllDebuffs();
    }

    public bool RemoveDebuff(string debuff, bool cancelled = false)
    {
        if (!cancelled && debuff == "Skulled")
            return true;

        if (!HasDebuff(debuff)) return false;
        var buffObj = Debuffs[debuff];

        if (buffObj == null) return false;
        buffObj.Cancelled = cancelled;
        buffObj.OnEnded(this, buffObj);

        return true;
    }

    #endregion

    #region Status

    public void UpdateAddAndRemove()
    {
        PlayerNearby?.Client.SendRemoveObject(Serial);
        var obj = new List<Sprite> { this };
        PlayerNearby?.Client.SendVisibleEntities(obj);
    }

    public void UpdateBuffs(TimeSpan elapsedTime)
    {
        if (this is Aisling secondaryCheck && !IsEnhancingSecondaryOffense)
        {
            if (secondaryCheck.EquipmentManager.Shield == null &&
                secondaryCheck.SecondaryOffensiveElement != ElementManager.Element.None)
                secondaryCheck.SecondaryOffensiveElement = ElementManager.Element.None;
        }

        var buffs = Buffs.Values;

        Parallel.ForEach(buffs, (b) =>
        {
            if (this is Aisling aisling)
            {
                aisling.Client.EnqueueBuffUpdatedEvent(this, b, elapsedTime);
                StatusBarDisplayUpdateBuff(b, elapsedTime);
            }
            else
                b.Update(this, elapsedTime);
        });
    }

    public void UpdateDebuffs(TimeSpan elapsedTime)
    {
        var debuffs = Debuffs.Values;

        Parallel.ForEach(debuffs, (d) =>
        {
            if (this is Aisling aisling)
            {
                aisling.Client.EnqueueDebuffUpdatedEvent(this, d, elapsedTime);
                StatusBarDisplayUpdateDebuff(d, elapsedTime);
            }
            else
                d.Update(this, elapsedTime);
        });
    }

    public void StatusBarDisplayUpdateBuff(Buff buff, TimeSpan elapsedTime)
    {
        if (!_buffAndDebuffTimer.Update(elapsedTime)) return;
        if (this is not Aisling aisling) return;
        var colorInt = byte.MinValue;

        var countDown = buff.Length - buff.Timer.Tick;
        buff.TimeLeft = countDown;

        if (buff.TimeLeft.IntIsWithin(-10, 1))
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
        else if (buff.TimeLeft.IntIsWithin(121, short.MaxValue))
            colorInt = (byte)StatusBarColor.White;

        aisling.Client.SendEffect((EffectColor)colorInt, buff.Icon);
    }

    public void StatusBarDisplayUpdateDebuff(Debuff debuff, TimeSpan elapsedTime)
    {
        if (!_buffAndDebuffTimer.Update(elapsedTime)) return;
        if (this is not Aisling aisling) return;
        var colorInt = byte.MinValue;

        var countDown = debuff.Length - debuff.Timer.Tick;
        debuff.TimeLeft = countDown;

        if (debuff.TimeLeft.IntIsWithin(-10, 1))
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
        else if (debuff.TimeLeft.IntIsWithin(121, short.MaxValue))
            colorInt = (byte)StatusBarColor.White;

        aisling.Client.SendEffect((EffectColor)colorInt, debuff.Icon);
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

    #endregion

    #region Sprite Methods

    public void Remove()
    {
        var nearby = AislingsEarShotNearby();

        foreach (var o in nearby)
            o?.Client?.SendRemoveObject(Serial);

        DeleteObject();
    }

    public void HideFrom(Aisling nearbyAisling)
    {
        nearbyAisling.Client.SendRemoveObject(Serial);
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