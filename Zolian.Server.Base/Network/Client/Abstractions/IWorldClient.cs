using Chaos.Common.Definitions;
using Chaos.Geometry;
using Chaos.Geometry.Abstractions.Definitions;
using Chaos.Networking.Abstractions;
using Chaos.Packets;

using Darkages.Meta;
using Darkages.Models;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using System.Diagnostics;

using EquipmentSlot = Chaos.Common.Definitions.EquipmentSlot;

namespace Darkages.Network.Client.Abstractions;

public interface IWorldClient : ISocketClient
{
    bool MapUpdating { get; set; }
    bool MapOpen { get; set; }
    Aisling Aisling { get; set; }
    DateTime BoardOpened { get; set; }
    DialogSession DlgSession { get; set; }
    bool CanSendLocation { get; }
    bool IsRefreshing { get; }
    bool CanRefresh { get; }
    bool IsEquipping { get; }
    bool IsDayDreaming { get; }
    bool IsMoving { get; }
    bool IsWarping { get; }
    bool WasUpdatingMapRecently { get; }
    CastInfo SpellCastInfo { get; set; }
    DateTime LastAssail { get; set; }
    DateTime LastSpellCast { get; set; }
    DateTime LastItemUsed { get; set; }
    DateTime LastSelfProfileRequest { get; set; }
    DateTime LastWorldListRequest { get; set; }
    DateTime LastClientRefresh { get; set; }
    DateTime LastWarp { get; set; }
    Item LastItemDropped { get; set; }
    DateTime LastLocationSent { get; set; }
    DateTime LastMapUpdated { get; set; }
    DateTime LastMessageSent { get; set; }
    DateTime LastMovement { get; set; }
    DateTime LastEquip { get; set; }
    Stopwatch Latency { get; set; }
    DateTime LastSave { get; set; }
    DateTime LastWhisperMessageSent { get; set; }
    PendingBuy PendingBuySessions { get; set; }
    PendingSell PendingItemSessions { get; set; }
    bool ShouldUpdateMap { get; set; }
    DateTime LastNodeClicked { get; set; }
    WorldPortal PendingNode { get; set; }
    Position LastKnownPosition { get; set; }
    int MapClicks { get; set; }
    uint EntryCheck { get; set; }
    WorldClient LoadEquipment();
    WorldClient LoadInventory();
    WorldClient LoadBank();
    void LoadSkillBook();
    void LoadSpellBook();
    void SendLoginMessage(LoginMessageType loginMessageType, string message = null);
    void SendAddItemToPane(Item item);
    void SendAddSkillToPane(Skill skill);
    void SendAddSpellToPane(Spell spell);
    void SendAnimation(ushort targetEffect, Position position = null, uint targetSerial = 0, ushort speed = 100, ushort casterEffect = 0, uint casterSerial = 0);
    void SendAttributes(StatUpdateType statUpdateType);
    void SendBoard(string boardName, short? startPost, int index = 0);
    void SendBoardList(IEnumerable<Board> boards);
    void SendEmbeddedBoard(int index, short? startPost);
    void SendBoardResponse(BoardOrResponseType responseType, string message, bool success);
    void SendPost(PostFormat post, bool isMail, bool enablePrevBtn = true);
    void SendBodyAnimation(uint id, BodyAnimation bodyAnimation, ushort speed, byte? sound = null);
    bool AttemptCastSpellFromCache(string spellName, Sprite caster, Sprite target);
    void SendCancelCasting();
    void SendConfirmClientWalk(Position oldPoint, Direction direction);
    void SendConfirmExit();
    void SendCooldown(bool skill, byte slot, int cooldownSeconds);
    void SendCreatureTurn(uint id, Direction direction);
    void SendCreatureWalk(uint id, Point startPoint, Direction direction);
    void SendDisplayAisling(Aisling aisling);
    //void SendDoors(IEnumerable<Door> doors);
    void SendEffect(EffectColor effectColor, byte effectIcon);
    void SendEquipment(byte displaySlot, Item item);
    void SendExchangeAccepted(bool persistExchange);
    void SendExchangeAddItem(bool rightSide, byte index, Item item);
    void SendExchangeCancel(bool rightSide);
    void SendExchangeRequestAmount(byte slot);
    void SendExchangeSetGold(bool rightSide, uint amount);
    void SendExchangeStart(Aisling fromAisling);
    void SendForcedClientPacket(ref ClientPacket clientPacket);
    void SendGroupRequest(GroupRequestType groupRequestType, string fromName);
    void SendHealthBar(Sprite creature, byte? sound = null);
    void SendLightLevel(LightLevel lightLevel);
    void SendLocation();
    void SendMapChangeComplete();
    void SendMapChangePending();
    void SendMapData();
    void SendMapInfo();
    void SendMapLoadComplete();
    void SendMetaData(MetaDataRequestType metaDataRequestType, MetafileManager metaDataStore, string name = null);
    void SendNotepad(byte identifier, NotepadType type, byte height, byte width, string message);
    void SendProfile(Aisling aisling);
    void SendProfileRequest();
    void SendPublicMessage(uint id, PublicMessageType publicMessageType, string message);
    void SendRefreshResponse();
    void SendRemoveItemFromPane(byte slot);
    void SendRemoveObject(uint id);
    void SendRemoveSkillFromPane(byte slot);
    void SendRemoveSpellFromPane(byte slot);
    void SendSelfProfile();
    void SendServerMessage(ServerMessageType serverMessageType, string message);
    void SendSound(byte sound, bool isMusic);
    void SendUnequip(EquipmentSlot equipmentSlot);
    void SendUserId();
    void SendVisibleEntities(List<Sprite> objects);
    void SendWorldList(IEnumerable<Aisling> users);
    void SendWorldMap();
    WorldClient AislingToGhostForm();
    WorldClient GhostFormToAisling();
    void LearnSkill(Mundane source, SkillTemplate subject, string message);
    void LearnSpell(Mundane source, SpellTemplate subject, string message);
    void ClientRefreshed();
    void DaydreamingRoutine();
    WorldClient SystemMessage(string message);
    Task<bool> Save();
    void DeathStatusCheck();
    WorldClient UpdateDisplay(bool excludeSelf = false);
    WorldClient PayItemPrerequisites(LearningPredicate prerequisites);
    bool PayPrerequisites(LearningPredicate prerequisites);
    bool CheckReqs(WorldClient client, Item item);
    void EquipLantern();
    void CheckDayDreaming();
    void HandleBadTrades();
    WorldClient Insert(bool update, bool delete);
    void Interrupt();
    void WorldMapInterrupt();
    void ForgetSkill(string s);
    void ForgetSkills();
    void ForgetSpell(string s);
    void ForgetSpells();
    void ForgetSpellSend(Spell spell);
    void TrainSkill(Skill skill);
    void TrainSpell(Spell spell);
    WorldClient ApproachGroup(Aisling targetAisling, IReadOnlyList<string> allowedMaps);
    bool GiveItem(string itemName);
    void GiveQuantity(Aisling aisling, string itemName, int range);
    void TakeAwayQuantity(Sprite owner, string item, int range);
    WorldClient LoggedIn(bool state);
    void Port(int i, int x = 0, int y = 0);
    void ResetLocation(WorldClient client);
    void Recover();
    void RevivePlayer(string u);
    void GiveScar();
    void RepairEquipment();
    bool Revive();
    bool IsBehind(Sprite sprite);
    void KillPlayer(Area map, string u);
    void GiveHp(int v = 1);
    void GiveMp(int v = 1);
    void GiveStr(byte v = 1);
    void GiveInt(byte v = 1);
    void GiveWis(byte v = 1);
    void GiveCon(byte v = 1);
    void GiveDex(byte v = 1);
    void EnqueueExperienceEvent(Aisling player, int exp, bool hunting, bool overflow);
    void EnqueueAbilityEvent(Aisling player, int exp, bool hunting, bool overflow);
    void EnqueueDebuffAppliedEvent(Sprite affected, Debuff debuff, TimeSpan timeLeft);
    void EnqueueBuffAppliedEvent(Sprite affected, Buff buff, TimeSpan timeLeft);
    void EnqueueDebuffUpdatedEvent(Sprite affected, Debuff debuff, TimeSpan timeLeft);
    void EnqueueBuffUpdatedEvent(Sprite affected, Buff buff, TimeSpan timeLeft);
    void GiveExp(int exp, bool overflow = false);
    void LevelUp(Aisling player, int extraExp);
    void GiveAp(int a, bool overflow = false);
    void DarkRankUp(Aisling player, int extraExp);
    WorldClient RefreshMap(bool updateView = false);
    WorldClient TransitionToMap(Area area, Position position);
    WorldClient TransitionToMap(int area, Position position);
    void WarpToAdjacentMap(WarpTemplate warps);
    void WarpTo(Position position);
    void WarpToAndRefresh(Position position);
    void CheckWarpTransitions(WorldClient client);
    void CheckWarpTransitions(WorldClient client, int x, int y);
    WorldClient Enter();
    WorldClient LeaveArea(int travelTo, bool update = false, bool delete = false);
    void CompleteMapTransition();
    void DeleteSkillFromDb(Skill skill);
    void DeleteSpellFromDb(Spell spell);
    void AddDiscoveredMapToDb();
    void AddToIgnoreListDb(string ignored);
    void RemoveFromIgnoreListDb(string ignored);
}