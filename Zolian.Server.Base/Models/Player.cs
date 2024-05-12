using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.Models;

public class Player : Sprite
{
    public DateTime Created { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public byte PasswordAttempts { get; set; }
    public bool Hacked { get; set; }
    public bool LoggedIn { get; set; }
    public DateTime LastLogged { get; set; }
    public string LastIP { get; set; }
    public string LastAttemptIP { get; set; }
    public ushort AbpLevel { get; set; }
    public int AbpNext { get; set; }
    public long AbpTotal { get; set; }
    public ushort ExpLevel { get; set; }
    public long ExpNext { get; set; }
    public long ExpTotal { get; set; }
    public ClassStage Stage { get; set; }
    public Job JobClass { get; set; }
    public Class Path { get; set; }
    public Class PastClass { get; set; }
    public Race Race { get; set; }
    public Afflictions Afflictions { get; set; }
    public Gender Gender { get; set; }
    public byte HairColor { get; set; }
    public byte HairStyle { get; set; }
    public byte NameColor { get; set; }
    public string ProfileMessage { get; set; }
    public Nation Nation { get; set; }
    public string Clan { get; set; }
    public string ClanRank { get; set; }
    public string ClanTitle { get; set; }
    public AnimalForm AnimalForm { get; set; }
    public ushort MonsterForm { get; set; }
    public ActivityStatus ActiveStatus { get; set; }
    public AislingFlags Flags { get; set; }
    public short CurrentWeight { get; set; }
    public byte World { get; set; }
    public byte Lantern { get; set; }
    public RestPosition Resting { get; set; }
    public bool FireImmunity { get; set; }
    public bool WaterImmunity { get; set; }
    public bool WindImmunity { get; set; }
    public bool EarthImmunity { get; set; }
    public bool LightImmunity { get; set; }
    public bool DarkImmunity { get; set; }
    public bool PoisonImmunity { get; set; }
    public bool EnticeImmunity { get; set; }
    public GroupStatus PartyStatus { get; set; }
    public string RaceSkill { get; set; }
    public string RaceSpell { get; set; }
    public bool GameMaster { get; set; }
    public bool ArenaHost { get; set; }
    public bool Knight { get; set; }
    public ulong GoldPoints { get; set; }
    public short StatPoints { get; set; }
    public ulong GamePoints { get; set; }
    public ulong BankedGold { get; set; }
    public short ArmorImg { get; set; }
    public short HelmetImg { get; set; }
    public short ShieldImg { get; set; }
    public short WeaponImg { get; set; }
    public short BootsImg { get; set; }
    public short HeadAccessoryImg { get; set; }
    public short Accessory1Img { get; set; }
    public short Accessory2Img { get; set; }
    public short Accessory3Img { get; set; }
    public byte Accessory1Color { get; set; }
    public byte Accessory2Color { get; set; }
    public byte Accessory3Color { get; set; }
    public byte BodyColor { get; set; }
    public byte BodySprite { get; set; }
    public byte FaceSprite { get; set; }
    public short OverCoatImg { get; set; }
    public byte BootColor { get; set; }
    public byte OverCoatColor { get; set; }
    public byte Pants { get; set; }
    public byte Aegis { get; set; }
    public byte Bleeding { get; set; }
    public byte Spikes { get; set; }
    public byte Rending { get; set; }
    public byte Reaping { get; set; }
    public byte Vampirism { get; set; }
    public byte Ghosting { get; set; }
    public byte Haste { get; set; }
    public byte Gust { get; set; }
    public byte Quake { get; set; }
    public byte Rain { get; set; }
    public byte Flame { get; set; }
    public byte Dusk { get; set; }
    public byte Dawn { get; set; }
}

public class ComboScroll
{
    public string Combo1 { get; set; } = string.Empty;
    public string Combo2 { get; set; } = string.Empty;
    public string Combo3 { get; set; } = string.Empty;
    public string Combo4 { get; set; } = string.Empty;
    public string Combo5 { get; set; } = string.Empty;
    public string Combo6 { get; set; } = string.Empty;
    public string Combo7 { get; set; } = string.Empty;
    public string Combo8 { get; set; } = string.Empty;
    public string Combo9 { get; set; } = string.Empty;
    public string Combo10 { get; set; } = string.Empty;
    public string Combo11 { get; set; } = string.Empty;
    public string Combo12 { get; set; } = string.Empty;
    public string Combo13 { get; set; } = string.Empty;
    public string Combo14 { get; set; } = string.Empty;
    public string Combo15 { get; set; } = string.Empty;
}

public class Quests
{
    public int MailBoxNumber { get; set; }
    public bool TutorialCompleted { get; set; }
    public bool BetaReset { get; set; }
    public int BlackSmithing { get; set; }
    public string BlackSmithingTier { get; set; } = "Novice";
    public int ArmorSmithing { get; set; }
    public string ArmorSmithingTier { get; set; } = "Novice";
    public int JewelCrafting { get; set; }
    public string JewelCraftingTier { get; set; } = "Novice";
    public int StoneSmithing { get; set; }
    public string StoneSmithingTier { get; set; } = "Novice";
    public int MilethReputation { get; set; }
    public int AbelReputation { get; set; }
    public int RucesionReputation { get; set; }
    public int SuomiReputation { get; set; }
    public int RionnagReputation { get; set; }
    public int OrenReputation { get; set; }
    public int PietReputation { get; set; }
    public int LouresReputation { get; set; }
    public int UndineReputation { get; set; }
    public int TagorReputation { get; set; }
    public int ThievesGuildReputation { get; set; }
    public int AssassinsGuildReputation { get; set; }
    public int AdventuresGuildReputation { get; set; }
    public int ArtursGift { get; set; }
    public bool CamilleGreetingComplete { get; set; }
    public bool ConnPotions { get; set; }
    public bool CryptTerror { get; set; }
    public bool CryptTerrorSlayed { get; set; }
    public bool CryptTerrorContinued { get; set; }
    public bool CryptTerrorContSlayed { get; set; }
    public bool NightTerror { get; set; }
    public bool NightTerrorSlayed { get; set; }
    public bool DreamWalking { get; set; }
    public bool DreamWalkingSlayed { get; set; }
    public int Dar { get; set; }
    public string DarItem { get; set; } = string.Empty;
    public bool ReleasedTodesbaum { get; set; }
    public bool DrunkenHabit { get; set; }
    public bool EternalLove { get; set; }
    public bool EternalLoveStarted { get; set; }
    public bool UnhappyEnding { get; set; }
    public bool HonoringTheFallen { get; set; }
    public bool ReadTheFallenNotes { get; set; }
    public bool GivenTarnishedBreastplate { get; set; }
    public bool FionaDance { get; set; }
    public int Keela { get; set; }
    public int KeelaCount { get; set; }
    public string KeelaKill { get; set; } = string.Empty;
    public bool KeelaQuesting { get; set; }
    public bool KillerBee { get; set; }
    public int Neal { get; set; }
    public int NealCount { get; set; }
    public string NealKill { get; set; } = string.Empty;
    public bool AbelShopAccess { get; set; }
    public int PeteKill { get; set; }
    public bool PeteComplete { get; set; }
    public bool SwampAccess { get; set; }
    public int SwampCount { get; set; }
    public bool TagorDungeonAccess { get; set; }
    public int Lau { get; set; }
    public string BeltDegree { get; set; } = string.Empty;
    public string BeltQuest { get; set; } = string.Empty;
    public bool SavedChristmas { get; set; }
    public bool RescuedReindeer { get; set; }
    public bool YetiKilled { get; set; }
    public bool UnknownStart { get; set; }
    public bool PirateShipAccess { get; set; }
    public bool ScubaSchematics { get; set; }
    public bool ScubaMaterialsQuest { get; set; }
    public bool ScubaGearCrafted { get; set; }
    public string EternalBond { get; set; } = string.Empty;
    public bool ArmorCraftingCodex { get; set; }
    public bool ArmorApothecaryAccepted { get; set; }
    public bool ArmorCodexDeciphered { get; set; }
    public bool ArmorCraftingCodexLearned { get; set; }
    public bool ArmorCraftingAdvancedCodexLearned { get; set; }
    public string CthonicKillTarget { get; set; } = string.Empty;
    public string CthonicFindTarget { get; set; } = string.Empty;
    public int CthonicKillCompletions { get; set; }
    public bool CthonicCleansingOne { get; set; }
    public bool CthonicCleansingTwo { get; set; }
    public bool CthonicDepthsCleansing { get; set; }
    public bool CthonicRuinsAccess { get; set; }
    public int CthonicRemainsExplorationLevel { get; set; }
    public bool EndedOmegasRein { get; set; }
}