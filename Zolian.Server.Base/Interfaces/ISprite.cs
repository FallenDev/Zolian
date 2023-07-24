using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Darkages.Enums;
using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface ISprite
{
    uint Serial { get; set; }
    int CurrentMapId { get; set; }
    Aisling PlayerNearby => AislingsNearby().FirstOrDefault();
    double Amplified { get; set; }
    ElementManager.Element OffenseElement { get; set; }
    ElementManager.Element SecondaryOffensiveElement { get; set; }
    ElementManager.Element DefenseElement { get; set; }
    ElementManager.Element SecondaryDefensiveElement { get; set; }
    bool ClawFistEmpowerment { get; set; }
    DateTime AbandonedDate { get; set; }
    Sprite Target { get; set; }
    int X { get; set; }
    int Y { get; set; }
    TileContent TileType { get; set; }
    int GroupId { get; set; }
    byte Direction { get; set; }
    int PendingX { get; set; }
    int PendingY { get; set; }
    DateTime LastMenuInvoked { get; set; }
    DateTime LastMovementChanged { get; set; }
    DateTime LastTargetAcquired { get; set; }
    DateTime LastTurnUpdated { get; set; }
    DateTime LastUpdated { get; set; }
    PrimaryStat MajorAttribute { get; set; }
    ConcurrentDictionary<string, Buff> Buffs { get; }
    ConcurrentDictionary<string, Debuff> Debuffs { get; }

    #region Stats

    int CurrentHp { get; set; }
    int BaseHp { get; set; }
    int BonusHp { get; set; }

    int CurrentMp { get; set; }
    int BaseMp { get; set; }
    int BonusMp { get; set; }

    int _Regen { get; set; }
    int BonusRegen { get; set; }

    byte _Dmg { get; set; }
    byte BonusDmg { get; set; }

    int BonusAc { get; set; }
    int _ac { get; set; }

    int BonusFortitude { get; set; }

    byte _Hit { get; set; }
    byte BonusHit { get; set; }

    byte _Mr { get; set; }
    byte BonusMr { get; set; }

    int _Str { get; set; }
    int BonusStr { get; set; }

    int _Int { get; set; }
    int BonusInt { get; set; }

    int _Wis { get; set; }
    int BonusWis { get; set; }

    int _Con { get; set; }
    int BonusCon { get; set; }

    int _Dex { get; set; }
    int BonusDex { get; set; }

    int _Luck { get; set; }
    int BonusLuck { get; set; }

    #endregion

    bool CanBeAttackedHere(Sprite source);
    void NotifyPropertyChanged([CallerMemberName] string propertyName = "");
    TSprite CastSpriteToType<TSprite>() where TSprite : Sprite;
    void ShowTo(Aisling nearbyAisling);
    List<Sprite> GetAllInFront(Sprite sprite, int tileCount = 1);
    List<Sprite> GetAllInFront(int tileCount = 1, bool intersect = false);
    List<Sprite> DamageableGetInFront(int tileCount = 1, bool intersect = false);
    List<Position> GetTilesInFront(int tileCount = 1, bool intersect = false);
    List<Sprite> DamageableGetAwayInFront(int tileCount = 2, bool intersect = false);
    List<Sprite> DamageableGetBehind(int tileCount = 1, bool intersect = false);
    List<Sprite> MonsterGetInFront(int tileCount = 1, bool intersect = false);
    List<Sprite> GetInFrontToSide(int tileCount = 1, bool intersect = false);
    List<Sprite> GetHorizontalInFront(int tileCount = 1, bool intersect = false);
    Position GetFromAllSidesEmpty(Sprite sprite, Sprite target, int tileCount = 1);
    Position GetFromAllSidesEmpty(Sprite target, int tileCount = 1, bool intersect = false);
    Position GetPendingChargePosition(int warp, Sprite sprite);
    Position GetPendingThrowPosition(int warp, Sprite sprite);
    bool GetPendingThrowIsWall(int warp, Sprite sprite);
    bool WithinRangeOf(Sprite other, bool checkMap = true);
    bool WithinEarShotOf(Sprite other, bool checkMap = true);
    bool WithinRangeOf(int x, int y, int subjectLength);
    bool WithinRangeOf(Sprite other, int distance, bool checkMap = true);
    bool TrapsAreNearby();
    Aisling[] AislingsNearby();
    Aisling[] AislingsEarShotNearby();
    IEnumerable<Monster> MonstersNearby();
    IEnumerable<Mundane> MundanesNearby();
    IEnumerable<Sprite> SpritesNearby();
    bool NextTo(int x, int y);
    bool Facing(int x, int y, out int direction);
    bool FacingFarAway(int x, int y, out int direction);
    bool Walk();
    bool WalkTo(int x, int y);
    void Wander();
    void Turn();

    // Damage Begin -- Going to refactor damage out of sprite
    void ApplyElementalSpellDamage(Sprite source, long dmg, ElementManager.Element element, Spell spell);
    void ApplyElementalSkillDamage(Sprite source, long dmg, ElementManager.Element element, Skill skill);
    void ApplyDamage(Sprite damageDealingSprite, long dmg, Skill skill, Action<int> dmgcb = null, bool forceTarget = false);
    void MagicApplyDamage(Sprite damageDealingSprite, long dmg, Spell spell, Action<int> dmgcb = null, bool forceTarget = false);
    void ApplyEquipmentDurability(int dmg);
    long ApplyWeaponBonuses(Sprite source, long dmg);
    Sprite ApplyBuff(string buffName);
    Sprite ApplyDebuff(string debuffName);
    double CalculateElementalDamageMod(ElementManager.Element element);
    long CompleteDamageApplication(Sprite damageDealingSprite, long dmg, byte sound, Action<int> dmgcb, double amplifier);
    void ShowDmg(Aisling aisling, TimeSpan elapsedTime);
    void ThreatGeneratedSubsided(Aisling aisling, TimeSpan elapsedTime);
    long ComputeDmgFromAc(long dmg);
    bool DamageTarget(Sprite damageDealingSprite, ref long dmg, byte sound, Action<int> dmgcb, bool forced);
    double GetElementalModifier(Sprite damageDealingSprite, bool isSecondary = false);
    int GetBaseDamage(Sprite damageDealingSprite, Sprite target, MonsterEnums type);
    void Thorns(Sprite damageDealingSprite, long dmg);
    long Vulnerable(long dmg);
    void VarianceProc(Sprite sprite, long dmg);
    void OnDamaged(Sprite source, long dmg);
    // Damage End -- Going to refactor damage out of sprite

    string GetDebuffName(Func<Debuff, bool> p);
    bool HasBuff(string buff);
    bool HasDebuff(string debuff);
    bool HasDebuff(Func<Debuff, bool> p);
    void RemoveAllBuffs();
    void RemoveAllDebuffs();
    bool RemoveBuff(string buff);
    void RemoveBuffsAndDebuffs();
    bool RemoveDebuff(string debuff, bool cancelled = false);
    void UpdateAddAndRemove();
    void UpdateBuffs(TimeSpan elapsedTime);
    void UpdateDebuffs(TimeSpan elapsedTime);
    void StatusBarDisplayUpdateBuff(Buff buff, TimeSpan elapsedTime);
    void StatusBarDisplayUpdateDebuff(Debuff debuff, TimeSpan elapsedTime);
    void Remove();
    void HideFrom(Aisling nearbyAisling);
}