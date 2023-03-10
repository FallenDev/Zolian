using Darkages.Enums;
using Darkages.Sprites;

namespace Darkages.Models;

public class Player : Sprite
{
    public DateTime Created { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int PasswordAttempts { get; set; }
    public bool Hacked { get; set; }
    public bool LoggedIn { get; set; }
    public DateTime LastLogged { get; set; }
    public string LastIP { get; set; }
    public string LastAttemptIP { get; set; }
    public uint AbpLevel { get; set; }
    public uint AbpNext { get; set; }
    public uint AbpTotal { get; set; }
    public uint ExpLevel { get; set; }
    // Since this property needs to be used in negative calculations it must stay int
    public int ExpNext { get; set; }
    public uint ExpTotal { get; set; }
    public ClassStage Stage { get; set; }
    public Class Path { get; set; }
    public Class PastClass { get; set; }
    public Race Race { get; set; }
    public RacialAfflictions Afflictions { get; set; }
    public Gender Gender { get; set; }
    public byte HairColor { get; set; }
    public byte HairStyle { get; set; }
    public byte OldColor { get; set; }
    public byte OldStyle { get; set; }
    public int Styling { get; set; }
    public int Coloring { get; set; }
    public byte NameColor { get; set; }
    public string ProfileMessage { get; set; }
    public string Nation { get; set; }
    public string Clan { get; set; }
    public string ClanRank { get; set; }
    public string ClanTitle { get; set; }
    public AnimalForm AnimalForm { get; set; }
    public ushort MonsterForm { get; set; }
    public ActivityStatus ActiveStatus { get; set; }
    public AislingFlags Flags { get; set; }
    public int CurrentWeight { get; set; }
    public int World { get; set; }
    public byte Lantern { get; set; }
    public bool Invisible { get; set; }
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
    public bool Developer { get; set; }
    public bool Ranger { get; set; }
    public bool Knight { get; set; }
    public uint GoldPoints { get; set; }
    public int StatPoints { get; set; }
    public uint GamePoints { get; set; }
    public uint BankedGold { get; set; }
    public uint ArmorImg { get; set; }
    public uint HelmetImg { get; set; }
    public uint ShieldImg { get; set; }
    public uint WeaponImg { get; set; }
    public uint BootsImg { get; set; }
    public uint HeadAccessory1Img { get; set; }
    public uint HeadAccessory2Img { get; set; }
    public uint OverCoatImg { get; set; }
    public byte BootColor { get; set; }
    public byte OverCoatColor { get; set; }
    public byte Pants { get; set; }
    public byte Aegis { get; set; }
    public byte Bleeding { get; set; }
    public int Spikes { get; set; }
    public byte Rending { get; set; }
    public byte Reaping { get; set; }
    public byte Vampirism { get; set; }
    public byte Haste { get; set; }
    public byte Gust { get; set; }
    public byte Quake { get; set; }
    public byte Rain { get; set; }
    public byte Flame { get; set; }
    public byte Dusk { get; set; }
    public byte Dawn { get; set; }
}

public class Quests
{
    public bool TutorialCompleted { get; set; }
    public bool BetaReset { get; set; }
    public int StoneSmithing { get; set; }
    public int MilethReputation { get; set; }
    public byte ArtursGift { get; set; }
    public bool CamilleGreetingComplete { get; set; }
    public bool ConnPotions { get; set; }
    public bool CryptTerror { get; set; }
    public bool CryptTerrorSlayed { get; set; }
    public int Dar { get; set; }
    public string DarItem { get; set; }
    public bool DrunkenHabit { get; set; }
    public bool EternalLove { get; set; }
    public bool FionaDance { get; set; }
    public int Keela { get; set; }
    public int KeelaCount { get; set; }
    public string KeelaKill { get; set; }
    public bool KeelaQuesting { get; set; }
    public bool KillerBee { get; set; }
    public int Neal { get; set; }
    public int NealCount { get; set; }
    public string NealKill { get; set; }
    public bool AbelShopAccess { get; set; }
    public int PeteKill { get; set; }
    public bool PeteComplete { get; set; }
    public bool SwampAccess { get; set; }
    public int SwampCount { get; set; }
}