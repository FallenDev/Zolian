using Darkages.Common;
using Darkages.Enums;
using Darkages.Types;

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using MapFlags = Darkages.Enums.MapFlags;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites.Entity;

namespace Darkages.Sprites;

public abstract class Sprite : INotifyPropertyChanged
{
    public bool Abyss;
    public Position LastPosition;
    public List<List<TileGrid>> MasterGrid = [];
    public event PropertyChangedEventHandler PropertyChanged;
    public readonly Stopwatch MonsterBuffAndDebuffStopWatch = new();
    private readonly Stopwatch _threatControl = new();
    private readonly Lock _aislingsNearbyLock = new();
    private readonly Lock _aislingsEarShotNearbyLock = new();
    private readonly Lock _aislingsOnMapLock = new();
    private readonly Lock _monstersNearbyLock = new();
    private readonly Lock _monstersOnMapLock = new();
    private readonly Lock _mundanesNearbyLock = new();
    private readonly Lock _spritesNearbyLock = new();
    private readonly Lock _spritesWithinRangeLock = new();
    private readonly Lock _getSpritesLock = new();
    private readonly Lock _getAislingDamageLock = new();
    private readonly Lock _getMonsterDamageLock = new();

    public bool Alive => CurrentHp > 1;
    public bool Attackable => this is Monster || this is Aisling;
    public bool Summoned;

    #region Buffs Debuffs

    protected int _frozenStack;
    public bool IsWeakened => CurrentHp <= MaximumHp * .05;
    public bool IsAited => HasBuff("Aite") || HasBuff("Dia Aite");
    public bool Immunity => HasBuff("Dion") || HasBuff("Mor Dion") || HasBuff("Ard Dion") || HasBuff("Stone Skin") ||
                            HasBuff("Iron Skin") || HasBuff("Wings of Protection");
    public bool Hastened => HasBuff("Hastenga") || HasBuff("Hasten") || HasBuff("Haste");
    public bool SpellReflect => HasBuff("Deireas Faileas") || HasBuff("Secured Position");
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
    public bool NinthGateReleased => HasBuff("Ninth Gate Release");
    public bool Berserk => HasBuff("Berserker Rage");
    public bool IsEnhancingSecondaryOffense => HasBuff("Atlantean Weapon");
    public bool IsCharmed => HasDebuff("Entice");
    public bool IsBleeding => HasDebuff("Bleeding");
    public bool IsCradhed => HasDebuff(i => i.Name.Contains("Cradh"));
    public bool IsVulnerable => IsFrozen || IsStopped || IsSleeping || Berserk || IsCharmed || IsWeakened || HasDebuff("Decay");
    public bool IsBlocked => IsFrozen || IsStopped || IsSleeping;

    public bool ClawFistEmpowerment { get; set; }
    public bool HardenedHands => HasBuff("Hardened Hands");
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

    protected static readonly int[][] Directions =
    [
        [+0, -1],
        [+1, +0],
        [+0, +1],
        [-1, +0]
    ];

    protected double TargetDistance { get; set; }

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
    protected int PendingX { get; set; }
    protected int PendingY { get; set; }
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

    protected bool CanBeAttackedHere(Sprite source)
    {
        if (source is not Aisling || this is not Aisling) return true;
        if (CurrentMapId <= 0 || !ServerSetup.Instance.GlobalMapCache.TryGetValue(CurrentMapId, out var value)) return true;

        return value.Flags.MapFlagIsSet(MapFlags.PlayerKill);
    }

    private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool CanAttack(Sprite attackingPlayer)
    {
        if (this is not Monster monster) return false;
        if (monster.Template.MonsterRace == MonsterRace.Dummy) return true;
        var flushTargetRecord = 0;

        // If targeting is not empty, check nearby players
        if (!monster.TargetRecord.TaggedAislings.IsEmpty)
        {
            var nearby = AislingsNearby();
            foreach (var player in nearby)
            {
                monster.TargetRecord.TaggedAislings.TryGetValue(player.Serial, out var playerRecord);

                // If nearby player is found, deny a record flush
                if (playerRecord.player != null)
                    flushTargetRecord++;
            }

            // Return if players are found and run logic if they can attack
            if (flushTargetRecord != 0)
                return monster.TargetRecord.TaggedAislings.TryGetValue(attackingPlayer.Serial, out _) || monster.TryAddTagging(attackingPlayer);

            lock (monster.TaggedAislingsLock)
            {
                // Record flush if no players are found
                monster.TargetRecord.TaggedAislings.Clear();
            }
        }

        // Add player or player's group
        monster.TryAddPlayerAndHisGroup(attackingPlayer);
        return true;
    }

    private IEnumerable<Sprite> UnSafeGetSprites(int x, int y) => ObjectManager.GetObjects(Map, i => (int)i.Pos.X == x && (int)i.Pos.Y == y, ObjectManager.Get.All);

    protected List<Sprite> GetSprites(int x, int y)
    {
        lock (_getSpritesLock)
        {
            return UnSafeGetSprites(x, y).ToList();
        }
    }

    private IEnumerable<Sprite> UnSafeAislingGetDamageableSprites(int x, int y) => ObjectManager.GetObjects(Map, i => (int)i.Pos.X == x && (int)i.Pos.Y == y, ObjectManager.Get.AislingDamage);

    protected List<Sprite> AislingGetDamageableSprites(int x, int y)
    {
        lock (_getAislingDamageLock)
        {
            return UnSafeAislingGetDamageableSprites(x, y).ToList();
        }
    }

    private IEnumerable<Sprite> UnSafeMonsterGetDamageableSprites(int x, int y) => ObjectManager.GetObjects(Map, i => (int)i.Pos.X == x && (int)i.Pos.Y == y, ObjectManager.Get.Monsters | ObjectManager.Get.Aislings);

    protected List<Sprite> MonsterGetDamageableSprites(int x, int y)
    {
        lock (_getMonsterDamageLock)
        {
            return UnSafeMonsterGetDamageableSprites(x, y).ToList();
        }
    }

    public bool WithinRangeOf(Sprite other) => other != null && WithinRangeOf(other, ServerSetup.Instance.Config.WithinRangeProximity);
    public bool WithinEarShotOf(Sprite other) => other != null && WithinRangeOf(other, 14);
    public bool WithinMonsterSpellRangeOf(Sprite other) => other != null && WithinRangeOf(other, 10);
    public bool WithinRangeOf(Sprite other, int distance)
    {
        if (other == null) return false;
        return CurrentMapId == other.CurrentMapId && WithinDistanceOf((int)other.Pos.X, (int)other.Pos.Y, distance);
    }
    public bool WithinRangeOfTile(Position pos, int distance) => pos != null && WithinDistanceOf(pos.X, pos.Y, distance);
    private bool WithinDistanceOf(int x, int y, int subjectLength) => DistanceFrom(x, y) < subjectLength;

    private IEnumerable<Aisling> UnSafeAislingsNearby() => ObjectManager.GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));

    public List<Aisling> AislingsNearby()
    {
        lock (_aislingsNearbyLock)
        {
            return UnSafeAislingsNearby().ToList();
        }
    }

    private IEnumerable<Aisling> UnSafeAislingsEarShotNearby() => ObjectManager.GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, 14));

    public List<Aisling> AislingsEarShotNearby()
    {
        lock (_aislingsEarShotNearbyLock)
        {
            return UnSafeAislingsEarShotNearby().ToList();
        }
    }

    private IEnumerable<Aisling> UnSafeAislingsOnMap() => ObjectManager.GetObjects<Aisling>(Map, i => i != null && Map == i.Map);

    public List<Aisling> AislingsOnMap()
    {
        lock (_aislingsOnMapLock)
        {
            return UnSafeAislingsOnMap().ToList();
        }
    }

    private IEnumerable<Monster> UnSafeMonstersNearby() => ObjectManager.GetObjects<Monster>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));

    public List<Monster> MonstersNearby()
    {
        lock (_monstersNearbyLock)
        {
            return UnSafeMonstersNearby().ToList();
        }
    }

    private IEnumerable<Monster> UnSafeMonstersOnMap() => ObjectManager.GetObjects<Monster>(Map, i => i != null);

    public List<Monster> MonstersOnMap()
    {
        lock (_monstersOnMapLock)
        {
            return UnSafeMonstersOnMap().ToList();
        }
    }

    private IEnumerable<Mundane> UnSafeMundanesNearby() => ObjectManager.GetObjects<Mundane>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity));

    public List<Mundane> MundanesNearby()
    {
        lock (_mundanesNearbyLock)
        {
            return UnSafeMundanesNearby().ToList();
        }
    }

    private List<Sprite> UnSafeDamageableNearby()
    {
        var result = new List<Sprite>();
        var listA = ObjectManager.GetObjects<Monster>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();
        var listB = ObjectManager.GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(this, ServerSetup.Instance.Config.WithinRangeProximity)).ToList();
        result.AddRange(listA);
        result.AddRange(listB);
        return result;
    }

    public List<Sprite> DamageableNearby()
    {
        lock (_spritesNearbyLock)
        {
            return UnSafeDamageableNearby();
        }
    }

    private List<Sprite> UnSafeDamageableWithinRange(Sprite target, int range)
    {
        var result = new List<Sprite>();
        var listA = ObjectManager.GetObjects<Monster>(Map, i => i != null && i.WithinRangeOf(target, range)).ToList();
        var listB = ObjectManager.GetObjects<Aisling>(Map, i => i != null && i.WithinRangeOf(target, range)).ToList();
        result.AddRange(listA);
        result.AddRange(listB);
        return result;
    }

    public List<Sprite> DamageableWithinRange(Sprite target, int range)
    {
        lock (_spritesWithinRangeLock)
        {
            return UnSafeDamageableWithinRange(target, range);
        }
    }

    public int DistanceFrom(int x, int y)
    {
        // Manhattan Distance
        return Math.Abs(X - x) + Math.Abs(Y - y);
    }

    #region Status

    public void UpdateBuffs(Sprite sprite)
    {
        var buffs = Buffs.Values;

        foreach (var b in buffs)
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

        foreach (var d in debuffs)
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

    private void StatusBarDisplayUpdateBuff(Buff buff)
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

    private void StatusBarDisplayUpdateDebuff(Debuff debuff)
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

    protected bool CanUpdate()
    {
        if (CantMove) return false;

        if (this is Monster)
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

    protected bool HasDebuff(Func<Debuff, bool> p)
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

    private void RemoveAllBuffs()
    {
        if (Buffs == null)
            return;

        foreach (var buff in Buffs)
            RemoveBuff(buff.Key);
    }

    private void RemoveAllDebuffs()
    {
        if (Debuffs == null)
            return;

        foreach (var debuff in Debuffs)
            RemoveDebuff(debuff.Key);
    }

    private bool RemoveBuff(string buff)
    {
        if (!HasBuff(buff)) return false;

        lock (Buffs)
        {
            try
            {
                var buffObj = Buffs.TryGetValue(buff, out var foundBuff);
                if (buffObj)
                {
                    foundBuff.OnEnded(this, foundBuff);
                }
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
                var debuffObj = Debuffs.TryGetValue(debuff, out var foundDebuff);
                if (debuffObj)
                {
                    foundDebuff.Cancelled = cancelled;
                    foundDebuff.OnEnded(this, foundDebuff);
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
}