using Darkages.Interfaces;
using Darkages.Models;
using Darkages.Types;

namespace Darkages;

public class ServerConstants : IServerConstants
{
    public int RefreshRate { get; set; }
    public int SaveRate { get; set; }
    public int WarpCheckRate { get; set; }
    public bool CancelCastingWhenWalking { get; set; }
    public bool CancelWalkingIfRefreshing { get; set; }
    public double NationReturnHours { get; set; }
    public int StartingMap { get; set; }
    public Position StartingPosition { get; set; }
    public short TransitionPointX { get; set; }
    public short TransitionPointY { get; set; }
    public int TransitionZone { get; set; }

    // Scripts
    public string ElementTableScript { get; set; }
    public string ACFormulaScript { get; set; }
    public string BaseDamageScript { get; set; }
    public string MonsterCreationScript { get; set; }
    public string MonsterRewardScript { get; set; }
    public string HelperMenuTemplateKey { get; set; }

    // Offense
    public double AiteDamageReductionMod { get; set; }
    public double BehindDamageMod { get; set; }

    // Defense
    public int MinimumHp { get; set; }
    public int MaxHP { get; set; }

    // Skills
    public double GlobalBaseSkillDelay { get; set; }
    public bool AssailsCancelSpells { get; set; }

    // Items
    public int ClickLootDistance { get; set; }
    public uint DefaultItemDurability { get; set; }
    public uint DefaultItemValue { get; set; }

    // Death
    public int DeathHPPenalty { get; set; }
    public int DeathMap { get; set; }
    public bool CanMoveDuringReap { get; set; }
    public int DeathMapX { get; set; }
    public int DeathMapY { get; set; }

    // Monsters | NPCs
    public double MundaneRespawnInterval { get; set; }
    public double GlobalSpawnTimer { get; set; }
    public double BaseDamageMod { get; set; }
    public int HpGainFactor { get; set; }
    public int MpGainFactor { get; set; }
    public int VeryNearByProximity { get; set; }
    public int WithinRangeProximity { get; set; }

    // Players
    public int PlayerLevelCap { get; set; }
    public double GroupExpBonus { get; set; }
    public ulong MaxCarryGold { get; set; }
    public int StatCap { get; set; }
    public int StatsPerLevel { get; set; }
    public double WeightIncreaseModifer { get; set; }

    // Server Related Maintenance
    public int ConnectionCapacity { get; set; }
    public int HelperMenuId { get; set; }
    public bool LogClientPackets { get; set; }
    public bool LogServerPackets { get; set; }
    public bool MultiUserLoginCheck { get; set; }
    public double PingInterval { get; set; }
    public int SERVER_PORT { get; set; }
    public int LOGIN_PORT { get; set; }
    public int LOBBY_PORT { get; set; }
    public string SERVER_TITLE { get; set; }
    public string ServerWelcomeMessage { get; set; }
    public IEnumerable<string> GameMasters { get; set; }
    public string[] DevModeExemptions { get; set; }
    public ReservedRedirectInfo[] ReservedRedirects { get; set; } = [];

    // Check Packet 00
    public int ClientVersion { get; set; }

    // Default Messages
    public double MessageClearInterval { get; set; }
    public string BadRequestMessage { get; set; }
    public string CantAttack { get; set; }
    public string CantCarryMoreMsg { get; set; }
    public string CantDoThat { get; set; }
    public string CantDropItemMsg { get; set; }
    public string CantUseThat { get; set; }
    public string StrAddedMessage { get; set; }
    public string IntAddedMessage { get; set; }
    public string WisAddedMessage { get; set; }
    public string ConAddedMessage { get; set; }
    public string DexAddedMessage { get; set; }
    public string CursedItemMessage { get; set; }
    public string GroupRequestDeclinedMsg { get; set; }
    public string LevelUpMessage { get; set; }
    public string AbilityUpMessage { get; set; }
    public string MerchantCancelMessage { get; set; }
    public string MerchantConfirmMessage { get; set; }
    public string MerchantRefuseTradeMessage { get; set; }
    public string MerchantStackErrorMessage { get; set; }
    public string MerchantTradeCompletedMessage { get; set; }
    public string NoManaMessage { get; set; }
    public string NotEnoughGoldToDropMsg { get; set; }
    public string ReapMessage { get; set; }
    public string NpcInteraction { get; set; }
    public string DeathReapingMessage { get; set; }
    public string ReapMessageDuringAction { get; set; }
    public string SomethingWentWrong { get; set; }
    public string SpellFailedMessage { get; set; }
    public string ToWeakToLift { get; set; }
    public string UserDroppedGoldMsg { get; set; }
    public string YouDroppedGoldMsg { get; set; }
    public string ItemNotRequiredMsg { get; set; }
}