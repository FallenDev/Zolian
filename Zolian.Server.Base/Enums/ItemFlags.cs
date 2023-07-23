namespace Darkages.Enums;

[Flags]
public enum ItemFlags
{
    Equipable = 1,
    Perishable = 1 << 1,
    Tradeable = 1 << 2,
    Dropable = 1 << 3,
    Bankable = 1 << 4,
    Sellable = 1 << 5,
    Repairable = 1 << 6,
    Stackable = 1 << 7,
    Consumable = 1 << 8,
    PerishIFEquipped = 1 << 9,
    Elemental = 1 << 10,
    QuestRelated = 1 << 11,
    Upgradeable = 1 << 12,
    TwoHanded = 1 << 13,
    LongRanged = 1 << 14,
    Trap = 1 << 15,
    DualWield = 1 << 16,
    TwoHandedStaff = 1 << 17,
    Unique = 1 << 18,
    DropScript = 1 << 19,

    // 125
    NormalEquipment = Equipable | Repairable | Tradeable | Sellable | Bankable | Dropable,

    // Bound
    // 113
    NormalEquipmentBoundNoSell = Equipable | Repairable | Bankable,
    NormalEquipmentBound = Equipable | Repairable | Bankable | Sellable,
    // Perishable Non-Elemental Bound
    // 115
    NormalEquipPerishBound = NormalEquipmentBound | Perishable,

    // Perishable Non-Elemental
    // 127
    NormalEquipPerish = NormalEquipment | Perishable,
    // Perishable Elemental
    // 1151
    NormalEquipElementPerish = NormalEquipment | Perishable | Elemental,

    // Non-Perishable Elemental
    // 1149
    NormalEquipElement = NormalEquipment | Elemental,
    // 444
    NormalConsumable = Consumable | Stackable | Dropable | Sellable | Tradeable | Bankable,
    // 2236
    NormalMonsterDrop = Stackable | Dropable | Sellable | Tradeable | Bankable | QuestRelated,
    // 2320
    NonDropableQuest = Bankable | Consumable | QuestRelated,
    // 2064
    NonDropableQuestNoConsume = Bankable | QuestRelated,
    // 264464
    NonDropableQuestUnique = Bankable | Consumable | QuestRelated | Unique,
    // 264208
    NonDropableQuestUniqueNoConsume = Bankable | QuestRelated | Unique,
    // 524552
    DropScriptConsumable = Consumable | Dropable | DropScript,

    // 4221 Dirk -> Loures Saber & Claidheamh, Broad Sword & Battle Sword -> Masquerade & 
    NonPerishableUpgradeWeapon = Equipable | Bankable | Tradeable | Dropable | Sellable | Repairable | Upgradeable,
    DualNonPerishableUpgradeWeapon = Equipable | Bankable | Tradeable | Dropable | Sellable | Repairable | Upgradeable | DualWield,

    // 4205 Dragon Slayer, Cutlass, Dragon Scale Sword, Scimitar
    NonPerishableUpgradeNoBankWeapon = Equipable | Sellable | Repairable | Tradeable | Dropable | Upgradeable,
    DualNonPerishableUpgradeNoBankWeapon = Equipable | Sellable | Repairable | Tradeable | Dropable | Upgradeable | DualWield,

    // 4223 Hatchet, Wood Axe, Prim Spear, Wooden Club, Spiked Club, Talg Axe, Scythe, Chain Mace, Stone Axe
    PerishableUpgradeWeapon = Equipable | Bankable | Perishable | Tradeable | Dropable | Sellable | Repairable | Upgradeable,
    DualPerishableUpgradeWeapon = Equipable | Bankable | Perishable | Tradeable | Dropable | Sellable | Repairable | Upgradeable | DualWield,

    // Two Hand
    // 12413 Claidhmore, Emerald, Gladius, Kindjal
    NonPerishableUpgradeWeaponTwoHand = Equipable | Bankable | Tradeable | Dropable | Sellable | Repairable | Upgradeable | TwoHanded,
    // 12397 Hy-Brasyl Battle Axe, Gold Kindjal
    NonPerishableNoBankWeaponTwoHand = Equipable | Sellable | Repairable | Tradeable | Dropable | Upgradeable | TwoHanded,
    // 12415 Giant Club, Stone Hammer
    PerishableUpgradeWeaponTwoHand = Equipable | Bankable | Perishable | Tradeable | Dropable | Sellable | Repairable | Upgradeable | TwoHanded,
    // 20607 All long range weapons (bows)
    PerishableUpgradeWeaponLongRange = Equipable | Bankable | Perishable | Tradeable | Dropable | Sellable | Repairable | Upgradeable | LongRanged,
    // 135295 All heavy staves
    PerishableUpgradeWeaponStaff = Equipable | Bankable | Perishable | Tradeable | Dropable | Sellable | Repairable | Upgradeable | TwoHandedStaff
}

[Flags]
public enum LootQualifer
{
    Random = 1 << 1,
    Table = 1 << 2,
    Event = 1 << 3,
    Gold = 1 << 5,
    None = 256,

    RandomGold = Random | Gold
}

public enum MoneySprites : short
{
    GoldCoin = 0x0089,
    SilverCoin = 0x008A,
    CopperCoin = 0x008B,
    GoldPile = 0x008C,
    SilverPile = 0x008D,
    CopperPile = 0x008E,
    MassGoldPile = 0x2804
}

public static class ItemExtensions
{
    public static bool FlagIsSet(this ItemFlags self, ItemFlags flag) => (self & flag) == flag;
    public static bool LootFlagIsSet(this LootQualifer self, LootQualifer flag) => (self & flag) == flag;
}