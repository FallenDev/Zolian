using Darkages.Infrastructure;
using Darkages.Models;
using Darkages.Types;

namespace Darkages.Interfaces;

public interface IServerConstants
{
    bool AssailsCancelSpells { get; }
    string BadRequestMessage { get; }
    double BehindDamageMod { get; }
    bool CancelCastingWhenWalking { get; }
    bool CancelWalkingIfRefreshing { get; }
    bool CanMoveDuringReap { get; }
    string CantAttack { get; }
    string CantCarryMoreMsg { get; }
    string CantDoThat { get; }
    string CantDropItemMsg { get; }
    string CantUseThat { get; }
    int ClickLootDistance { get; }
    int ClientVersion { get; }
    string ConAddedMessage { get; }
    int ConnectionCapacity { get; }
    string CursedItemMessage { get; }
    int DeathHPPenalty { get; }
    int DeathMap { get; }
    int DeathMapX { get; }
    int DeathMapY { get; }
    string DeathReapingMessage { get; }
    uint DefaultItemDurability { get; }
    uint DefaultItemValue { get; }
    string[] DevModeExemptions { get; }
    string DexAddedMessage { get; }
    IEnumerable<string> GameMasters { get; }
    double GlobalBaseSkillDelay { get; }
    double GlobalSpawnTimer { get; }
    double GroupExpBonus { get; set; }
    string GroupRequestDeclinedMsg { get; }
    int HelperMenuId { get; }
    string HelperMenuTemplateKey { get; }
    int HpGainFactor { get; }
    string IntAddedMessage { get; }
    string ItemNotRequiredMsg { get; }
    string LevelUpMessage { get; }
    string AbilityUpMessage { get; }
    bool LogClientPackets { get; }
    int LOGIN_PORT { get; }
    int LOBBY_PORT { get; }
    bool LogServerPackets { get; }
    uint MaxCarryGold { get; }
    int MaxHP { get; }
    string MerchantCancelMessage { get; }
    string MerchantConfirmMessage { get; }
    string MerchantRefuseTradeMessage { get; }
    string MerchantStackErrorMessage { get; }
    string MerchantTradeCompletedMessage { get; }
    double MessageClearInterval { get; }
    int MinimumHp { get; }
    int MpGainFactor { get; }
    bool MultiUserLoginCheck { get; }
    double MundaneRespawnInterval { get; }
    double NationReturnHours { get; }
    string NoManaMessage { get; }
    string NotEnoughGoldToDropMsg { get; }
    double PingInterval { get; }
    int PlayerLevelCap { get; }
    string ReapMessage { get; }
    string NpcInteraction { get; }
    string ReapMessageDuringAction { get; }
    int RefreshRate { get; }
    int SaveRate { get; }
    int SERVER_PORT { get; }
    string SERVER_TITLE { get; }
    string ServerWelcomeMessage { get; }
    List<GameSetting> Settings { get; }
    public ReservedRedirectInfo[] ReservedRedirects { get; }
    string SomethingWentWrong { get; }
    string SpellFailedMessage { get; }
    int StartingMap { get; }
    Position StartingPosition { get; }
    int StatCap { get; }
    int StatsPerLevel { get; }
    string StrAddedMessage { get; }
    string ToWeakToLift { get; }
    short TransitionPointX { get; }
    short TransitionPointY { get; }
    int TransitionZone { get; }
    string UserDroppedGoldMsg { get; }
    int VeryNearByProximity { get; }
    int WarpCheckRate { get; }
    double WeightIncreaseModifer { get; }
    string WisAddedMessage { get; }
    int WithinRangeProximity { get; }
    string YouDroppedGoldMsg { get; }
    double AiteDamageReductionMod { get; }
    int BaseDamageMod { get; }
    string ACFormulaScript { get; }
    string ElementTableScript { get; }
    string MonsterRewardScript { get; }
    string BaseDamageScript { get; }
    string MonsterCreationScript { get; }
}