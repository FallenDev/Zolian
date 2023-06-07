using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IGameClient
{
    bool SerialSent { get; set; }
    Aisling Aisling { get; set; }
    bool Authenticated { get; set; }
    DateTime BoardOpened { get; set; }
    bool CanSendLocation { get; }
    DialogSession DlgSession { get; set; }
    bool IsRefreshing { get; }
    bool CanRefresh { get; }
    bool IsDayDreaming { get; }
    bool IsEquipping { get; }
    bool IsMoving { get; }
    bool IsWarping { get; }
    DateTime LastAssail { get; set; }
    DateTime LastClientRefresh { get; set; }
    Item LastItemDropped { get; set; }
    DateTime LastLocationSent { get; set; }
    DateTime LastMapUpdated { get; set; }
    DateTime LastMessageSent { get; set; }
    DateTime LastMovement { get; set; }
    DateTime LastEquip { get; set; }
    DateTime LastPing { get; set; }
    DateTime LastPingResponse { get; set; }
    DateTime LastSave { get; set; }
    DateTime LastWarp { get; set; }
    DateTime LastWhisperMessageSent { get; set; }
    PendingSell PendingItemSessions { get; set; }
    PendingBuy PendingBuySessions { get; set; }
    PendingBanked PendingBankedSession { get; set; }
    GameServer Server { get; set; }
    bool ShouldUpdateMap { get; set; }
    DateTime LastNodeClicked { get; set; }
    WorldPortal PendingNode { get; set; }
    bool WasUpdatingMapRecently { get; }
    Position LastKnownPosition { get; set; }
    int MapClicks { get; set; }
    int EntryCheck { get; set; }

    GameClient AislingToGhostForm();
    void BuildSettings();
    bool CastSpell(string spellName, Sprite caster, Sprite target);
    bool CheckReqs(GameClient client, Item item);
    void CloseDialog();
    void DoUpdate(TimeSpan elapsedTime);
    void PreventMultiLogging();
    Task EffectAsync(ushort n, int d = 1000, int r = 1);
    void ForgetSkill(string s);
    void ForgetSkills();
    void DeleteSkillFromDb(Skill skill);
    void ForgetSpell(string s);
    void ForgetSpells();
    void DeleteSpellFromDb(Spell spell);
    GameClient GhostFormToAisling();
    void GiveCon(byte v = 1);
    void GiveDex(byte v = 1);
    void GiveExp(uint a);
    void HandleExp(Aisling player, uint exp);
    void LevelUp(Player player);
    void GiveHp(int v = 1);
    void GiveInt(byte v = 1);
    bool GiveItem(string itemName);
    void GiveQuantity(Aisling aisling, string itemName, int range);
    void TakeAwayQuantity(Sprite owner, string item, int range);
    GameClient ApproachGroup(Aisling targetAisling, IReadOnlyList<string> allowedMaps);
    void GiveMp(int v = 1);
    void GiveScar();
    void GiveStr(byte v = 1);
    bool GiveTutorialArmor();
    bool IsBehind(Sprite sprite);
    void GiveWis(byte v = 1);
    void HandleBadTrades();
    GameClient InitSpellBar();
    GameClient InitBuffs();
    GameClient InitDeBuffs();
    GameClient InitLegend();
    GameClient InitDiscoveredMaps();
    GameClient InitIgnoreList();
    GameClient Insert(bool update, bool delete);
    void Interrupt();
    void KillPlayer(string u);
    void LearnEverything();
    GameClient LearnSkill(Mundane source, SkillTemplate subject, string message);
    GameClient LearnSpell(Mundane source, SpellTemplate subject, string message);
    GameClient LeaveArea(bool update = false, bool delete = false);
    Task<GameClient> Load();
    GameClient LoadEquipment();
    GameClient LoadInventory();
    GameClient LoadSkillBook();
    GameClient LoadSpellBook();
    GameClient LoggedIn(bool state);
    void OpenBoard(string n);
    GameClient PayItemPrerequisites(LearningPredicate prerequisites);
    bool PayPrerequisites(LearningPredicate prerequisites);
    void Port(int i, int x = 0, int y = 0);
    void ResetLocation(GameClient client);
    void Recover();
    void ClientRefreshed();
    GameClient RefreshMap(bool updateView = false);
    void DisableShade();
    void RepairEquipment();
    bool Revive();
    void RevivePlayer(string u);
    Task<GameClient> Save();
    void Say(string message, byte type = 0x00);
    void SendAnimation(ushort animation, Sprite to, Sprite from, byte speed = 100);
    void SendItemSellDialog(Mundane mundane, string text, ushort step, IEnumerable<byte> items);
    void SendItemShopDialog(Mundane mundane, string text, ushort step, IEnumerable<ItemTemplate> items);
    GameClient SendLocation();
    GameClient SendMessage(byte type, string text);
    GameClient SendMessage(string text);
    void SendMessage(Scope scope, byte type, string text);
    void SendMapMusic();
    void SendOptionsDialog(Mundane mundane, string text, params OptionsDataItem[] options);
    void SendOptionsDialog(Mundane mundane, string text, string args, params OptionsDataItem[] options);
    void SendPopupDialog(Mundane popup, string text, params OptionsDataItem[] options);
    void SendProfileUpdate();
    void SendSerial();
    GameClient InitQuests();
    void SkillsAndSpellsCleanup();
    void SkillCleanup();
    void SpellCleanup();
    void EquipGearAndAttachScripts();
    void LoadBank();
    void SendSkillForgetDialog(Mundane mundane, string text, ushort step);
    void SendSkillLearnDialog(Mundane mundane, string text, ushort step, IEnumerable<SkillTemplate> skills);
    GameClient SendSound(byte sound, Scope scope = Scope.Self);
    void SendSpellForgetDialog(Mundane mundane, string text, ushort step);
    void SendSpellLearnDialog(Mundane mundane, string text, ushort step, IEnumerable<SpellTemplate> spells);
    GameClient SendStats(StatusFlags flags);
    void SetAislingStartupVariables();
    void Spawn(string t, int x, int y, int c);
    void DeathStatusCheck();
    void StressTest();
    GameClient SystemMessage(string lpmessage);
    void TrainSkill(Skill skill);
    void TrainSpell(Spell spell);
    GameClient TransitionToMap(Area area, Position position);
    GameClient TransitionToMap(int area, Position position);
    void Update(TimeSpan elapsedTime);
    void PassEncryption();
    void DaydreamingRoutine(TimeSpan elapsedTime);
    void VariableLagDisconnector(int delay);
    void DispatchCasts();
    GameClient UpdateDisplay(bool excludeSelf);
    void UpdateStatusBarAndThreat(TimeSpan elapsedTime);
    void UpdateSkillSpellCooldown(TimeSpan elapsedTime);
    void WarpToAdjacentMap(WarpTemplate warps);
    void WarpTo(Position position, bool overrideRefresh);
    GameClient Enter();
    void CompleteMapTransition();
    void AddDiscoveredMapToDb();
    void AddToIgnoreListDb(string ignored);
    void RemoveFromIgnoreListDb(string ignored);
}