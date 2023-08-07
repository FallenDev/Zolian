using System.Collections.Concurrent;
using System.Numerics;
using Darkages.Common;
using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Types;

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
    Quests QuestManager { get; set; }
    List<int> DiscoveredMaps { get; set; }
    List<string> IgnoredList { get; set; }
    ConcurrentDictionary<string, string> ExplorePositions { get; set; }
    Vector2 DeathLocation { get; set; }
    int DeathMapId { get; set; }

    void SendTargetedClientMethod(Scope op, Action<IWorldClient> method, IEnumerable<Aisling> definer = null);
    void AStarPath(List<Vector2> pathList);
    void CancelExchange();
    bool CanSeeGhosts();
    void UsedSkill(Skill skill);
    void CastTargetAnimation(Spell spell, Sprite target, CastInfo info);
    Aisling CastAnimation(Spell spell, Sprite target, byte actionSpeed = 30);
    void CastDeath();
    void CastSpell(Spell spell, CastInfo info);
    void FinishExchange();
    IEnumerable<Skill> GetAssails();
    bool GiveGold(uint offer, bool sendClientUpdate = true);
    Aisling GiveHealth(Sprite target, int value);
    void GoHome();
    bool HasInInventory(string item, int count, out int found);
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
}