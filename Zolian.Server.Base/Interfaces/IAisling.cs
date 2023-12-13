using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using System.Numerics;

namespace Darkages.Interfaces;

public interface IAisling : ISprite
{
    WorldClient Client { get; set; }
    bool Loading { get; set; }
    long DamageCounter { get; set; }
    long ThreatMeter { get; set; }
    DialogSequence ActiveSequence { get; set; }
    ExchangeSession Exchange { get; set; }
    NameDisplayStyle NameStyle { get; set; }
    bool IsCastingSpell { get; set; }
    bool ProfileOpen { get; set; }
    Summon SummonObjects { get; set; }
    bool UsingTwoHanded { get; set; }
    int LastMapId { get; set; }
    WorldServerTimer AttackDmgTrack { get; }
    WorldServerTimer ThreatTimer { get; }
    ChantTimer ChantTimer { get; }
    UserOptions GameSettings { get; init; }
    Mail MailFlags { get; set; }
    SkillBook SkillBook { get; set; }
    SpellBook SpellBook { get; set; }
    string ActionUsed { get; set; }
    bool ThrewHealingPot { get; set; }
    Death Remains { get; set; }
    Legend LegendBook { get; set; }
    byte[] PictureData { get; set; }
    Bank BankManager { get; set; }
    Inventory Inventory { get; set; }
    EquipmentManager EquipmentManager { get; set; }
    ComboScroll ComboManager { get; set; }
    Quests QuestManager { get; set; }
    List<int> DiscoveredMaps { get; set; }
    int Styling { get; set; }
    int Coloring { get; set; }
    byte OldColor { get; set; }
    byte OldStyle { get; set; }
    List<string> IgnoredList { get; set; }
    ConcurrentDictionary<string, string> ExplorePositions { get; set; }
    Vector2 DeathLocation { get; set; }
    int DeathMapId { get; set; }

    void SendTargetedClientMethod(Scope op, Action<IWorldClient> method, IEnumerable<Aisling> definer = null);
    void AStarPath(List<Vector2> pathList);
    void CancelExchange();
    bool CanSeeGhosts();
    void UsedSkill(Skill skill);
    Aisling CastAnimation(Spell spell, byte actionSpeed = 30);
    void CastDeath();
    void CastSpell(Spell spell, CastInfo info);
    void FinishExchange();
    IEnumerable<Skill> GetAssails();
    Skill GetSkill(string s);
    bool GiveGold(uint offer, bool sendClientUpdate = true);
    Aisling GiveHealth(Sprite target, int value);
    void GoHome();
    bool HasInInventory(string item, int count);
    bool HasItem(string item);
    bool HasStacks(string item, ushort amount);
    bool HasKilled(string value, int number);
    Aisling HasManaFor(Spell spell);
    bool HasVisitedMap(int mapId);
    void ExplorePosition(string quest);
    bool IsDead();
    bool IsWearing(string item);
    void Recover();
    void Remove(bool update = false, bool delete = true);
    void ReviveInFront();
    void PrepareForHell();
    Aisling TrainSpell(Spell lpSpell);
    Aisling UpdateStats(Spell lpSpell);
    void UpdateStats();
    void WarpToHell();
    void AutoRoutine();
}