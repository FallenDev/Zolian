/* 
After using script to generate ZolianPlayers Tables & Stored Procedures
Ensure you encrypt the column under dbo.Players "Password"

Note: You must create a database called "ZolianPlayers" prior to 
executing this script. If a database already exists, ignore this

While creating characters if the encryption is new or is overwriting
an existing encryption. You may delete your keys within your database
and recreate them. If you encounter errors or issues you may need
to execute this command on the stored procedures

USE [ZolianPlayers]
GO
EXEC sp_refresh_parameter_encryption [StoredProcedureName];
GO
*/

USE [ZolianPlayers]
GO

DROP PROCEDURE [dbo].[SpellToPlayer]
DROP PROCEDURE [dbo].[SkillToPlayer]
DROP PROCEDURE [dbo].[SelectSpells]
DROP PROCEDURE [dbo].[SelectSkills]
DROP PROCEDURE [dbo].[SelectQuests]
DROP PROCEDURE [dbo].[SelectCombos]
DROP PROCEDURE [dbo].[SelectPlayer]
DROP PROCEDURE [dbo].[SelectLegends]
DROP PROCEDURE [dbo].[SelectIgnoredPlayers]
DROP PROCEDURE [dbo].[SelectBanked]
DROP PROCEDURE [dbo].[SelectEquipped]
DROP PROCEDURE [dbo].[SelectInventory]
DROP PROCEDURE [dbo].[SelectDiscoveredMaps]
DROP PROCEDURE [dbo].[SelectDeBuffsCheck]
DROP PROCEDURE [dbo].[SelectDeBuffs]
DROP PROCEDURE [dbo].[SelectBuffsCheck]
DROP PROCEDURE [dbo].[SelectBuffs]
DROP PROCEDURE [dbo].[PlayerSecurity]
DROP PROCEDURE [dbo].[PlayerSaveSpells]
DROP PROCEDURE [dbo].[PlayerSaveSkills]
DROP PROCEDURE [dbo].[PlayerSave]
DROP PROCEDURE [dbo].[PlayerQuestSave]
DROP PROCEDURE [dbo].[PlayerComboSave]
DROP PROCEDURE [dbo].[PlayerCreation]
DROP PROCEDURE [dbo].[AccountLockoutCount]
DROP PROCEDURE [dbo].[PasswordSave]
DROP PROCEDURE [dbo].[InsertQuests]
DROP PROCEDURE [dbo].[InsertDeBuff]
DROP PROCEDURE [dbo].[InsertBuff]
DROP PROCEDURE [dbo].[IgnoredSave]
DROP PROCEDURE [dbo].[FoundMap]
DROP PROCEDURE [dbo].[DeBuffSave]
DROP PROCEDURE [dbo].[CheckIfPlayerSerialExists]
DROP PROCEDURE [dbo].[CheckIfPlayerHashExists]
DROP PROCEDURE [dbo].[CheckIfPlayerExists]
DROP PROCEDURE [dbo].[LoadItemsToCache]
DROP PROCEDURE [dbo].[BuffSave]
DROP PROCEDURE [dbo].[AddLegendMark]
DROP PROCEDURE [dbo].[ItemUpsert]
DROP PROCEDURE [dbo].[ItemMassDelete]
DROP PROCEDURE [dbo].[CheckIfMailBoxNumberExists]
DROP PROCEDURE [dbo].[ObtainMailBoxNumber]
GO

DROP TYPE dbo.PlayerType
DROP TYPE dbo.ComboType
DROP TYPE dbo.QuestType
DROP TYPE dbo.ItemType
DROP TYPE dbo.SkillType
DROP TYPE dbo.SpellType
DROP TYPE dbo.BuffType
DROP TYPE dbo.DebuffType
GO

DROP TABLE PlayersItems;
DROP TABLE PlayersLegend;
DROP TABLE PlayersSkillBook;
DROP TABLE PlayersSpellBook;
DROP TABLE PlayersDebuffs;
DROP TABLE PlayersBuffs;
DROP TABLE PlayersDiscoveredMaps;
DROP TABLE PlayersCombos;
DROP TABLE PlayersQuests;
DROP TABLE PlayersIgnoreList;
DROP TABLE Players;

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE Players
(
    [Serial] BIGINT NOT NULL PRIMARY KEY,
	[Created] DATETIME DEFAULT CURRENT_TIMESTAMP,
	[Username] VARCHAR(12) NOT NULL,
	[Password] VARCHAR(8) NOT NULL,
	[PasswordAttempts] TINYINT NOT NULL DEFAULT 0,
	[Hacked] BIT NOT NULL DEFAULT 0,
	[LoggedIn] BIT NOT NULL DEFAULT 0,
	[LastLogged] DATETIME DEFAULT CURRENT_TIMESTAMP,
	[LastIP] VARCHAR(15) DEFAULT '127.0.0.1',
	[LastAttemptIP] VARCHAR(15) DEFAULT '127.0.0.1',
	[X] TINYINT NOT NULL DEFAULT 0,
	[Y] TINYINT NOT NULL DEFAULT 0,
	[CurrentMapId] INT NOT NULL DEFAULT 3029,
	[Direction] TINYINT NOT NULL DEFAULT 0,
	[CurrentHp] INT NOT NULL DEFAULT 0,
	[BaseHp] INT NOT NULL DEFAULT 0,
	[CurrentMp] INT NOT NULL DEFAULT 0,
	[BaseMp] INT NOT NULL DEFAULT 0,
	[_ac] SMALLINT NOT NULL DEFAULT 0,
	[_Regen] SMALLINT NOT NULL DEFAULT 0,
	[_Dmg] SMALLINT NOT NULL DEFAULT 0,
	[_Hit] SMALLINT NOT NULL DEFAULT 0,
	[_Mr] SMALLINT NOT NULL DEFAULT 0,
	[_Str] SMALLINT NOT NULL DEFAULT 0,
	[_Int] SMALLINT NOT NULL DEFAULT 0,
	[_Wis] SMALLINT NOT NULL DEFAULT 0,
	[_Con] SMALLINT NOT NULL DEFAULT 0,
	[_Dex] SMALLINT NOT NULL DEFAULT 0,
	[_Luck] SMALLINT NOT NULL DEFAULT 0,
	[AbpLevel] INT NOT NULL DEFAULT 0,
	[AbpNext] INT NOT NULL DEFAULT 0,
	[AbpTotal] BIGINT NOT NULL DEFAULT 0,
	[ExpLevel] INT NOT NULL DEFAULT 1,
	[ExpNext] BIGINT NOT NULL DEFAULT 0,
	[ExpTotal] BIGINT NOT NULL DEFAULT 0,
	[Stage] VARCHAR(10) NOT NULL,
    [JobClass] VARCHAR(12) NOT NULL DEFAULT 'None',
	[Path] VARCHAR(10) NOT NULL DEFAULT 'Peasant',
	[PastClass] VARCHAR(10) NOT NULL DEFAULT 'Peasant',
	[Race] VARCHAR(10) NOT NULL DEFAULT 'Human',
	[Afflictions] VARCHAR(120) NOT NULL DEFAULT 'Normal',
	[Gender] VARCHAR(6) NOT NULL DEFAULT 'Both',
	[HairColor] TINYINT NOT NULL DEFAULT 0,
	[HairStyle] TINYINT NOT NULL DEFAULT 0,
	[NameColor] TINYINT NOT NULL DEFAULT 1,
	[Nation] VARCHAR(30) NOT NULL DEFAULT 'Mileth',
	[Clan] VARCHAR(20) NULL,
	[ClanRank] VARCHAR(20) NULL,
	[ClanTitle] VARCHAR(20) NULL,
	[MonsterForm] SMALLINT NOT NULL DEFAULT 0,
	[ActiveStatus] VARCHAR(15) NOT NULL DEFAULT 'Awake',
	[Flags] VARCHAR(6) NOT NULL DEFAULT 'Normal',
	[CurrentWeight] SMALLINT NOT NULL DEFAULT 0,
	[World] TINYINT NOT NULL DEFAULT 0,
	[Lantern] TINYINT NOT NULL DEFAULT 0,
	[Invisible] BIT NOT NULL DEFAULT 0,
	[Resting] VARCHAR(13) NOT NULL DEFAULT 'Standing',
	[FireImmunity] BIT NOT NULL DEFAULT 0,
	[WaterImmunity] BIT NOT NULL DEFAULT 0,
	[WindImmunity] BIT NOT NULL DEFAULT 0,
	[EarthImmunity] BIT NOT NULL DEFAULT 0,
	[LightImmunity] BIT NOT NULL DEFAULT 0,
	[DarkImmunity] BIT NOT NULL DEFAULT 0,
	[PoisonImmunity] BIT NOT NULL DEFAULT 0,
	[EnticeImmunity] BIT NOT NULL DEFAULT 0,
	[PartyStatus] VARCHAR(21) NOT NULL DEFAULT 1,
	[RaceSkill] VARCHAR(20) NULL,
	[RaceSpell] VARCHAR(20) NULL,
	[GameMaster] BIT NOT NULL DEFAULT 0,
	[ArenaHost] BIT NOT NULL DEFAULT 0,
	[Knight] BIT NOT NULL DEFAULT 0,
	[GoldPoints] BIGINT NOT NULL DEFAULT 0,
	[StatPoints] SMALLINT NOT NULL DEFAULT 0,
	[GamePoints] BIGINT NOT NULL DEFAULT 0,
	[BankedGold] BIGINT NOT NULL DEFAULT 0,
	[ArmorImg] SMALLINT NOT NULL DEFAULT 0,
	[HelmetImg] SMALLINT NOT NULL DEFAULT 0,
	[ShieldImg] SMALLINT NOT NULL DEFAULT 0,
	[WeaponImg] SMALLINT NOT NULL DEFAULT 0,
	[BootsImg] SMALLINT NOT NULL DEFAULT 0,
    [HeadAccessoryImg] SMALLINT NOT NULL DEFAULT 0,
	[Accessory1Img] SMALLINT NOT NULL DEFAULT 0,
	[Accessory2Img] SMALLINT NOT NULL DEFAULT 0,
    [Accessory3Img] SMALLINT NOT NULL DEFAULT 0,
    [Accessory1Color] TINYINT NOT NULL DEFAULT 0,
    [Accessory2Color] TINYINT NOT NULL DEFAULT 0,
    [Accessory3Color] TINYINT NOT NULL DEFAULT 0,
    [BodyColor] TINYINT NOT NULL DEFAULT 0,
    [BodySprite] TINYINT NOT NULL DEFAULT 0,
    [FaceSprite] TINYINT NOT NULL DEFAULT 0,
    [OverCoatImg] SMALLINT NOT NULL DEFAULT 0,
	[BootColor] TINYINT NOT NULL DEFAULT 0,
	[OverCoatColor] TINYINT NOT NULL DEFAULT 0,
	[Pants] TINYINT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersDiscoveredMaps
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[MapId] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersBuffs
(
	[BuffId] INT NOT NULL PRIMARY KEY, 
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[Name] VARCHAR(30) NULL,
	[TimeLeft] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersDebuffs
(
	[DebuffId] INT NOT NULL PRIMARY KEY,
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[Name] VARCHAR(30) NULL,
	[TimeLeft] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersSpellBook
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[Level] INT NOT NULL DEFAULT 0,
	[Slot] INT NULL,
	[SpellName] VARCHAR(30) NULL,
	[CurrentCooldown] INT NULL,
	[Casts] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersSkillBook
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[Level] INT NOT NULL DEFAULT 0,
	[Slot] INT NULL,
	[SkillName] VARCHAR(30) NULL,
	[CurrentCooldown] INT NULL,
    [Uses] INT NOT NULL DEFAULT 0,
)

CREATE TABLE PlayersLegend
(
	[LegendId] INT NOT NULL PRIMARY KEY,
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
    [Key] VARCHAR (25) NOT NULL,
    [IsPublic] BIT NOT NULL DEFAULT 1,
	[Time] DATETIME DEFAULT CURRENT_TIMESTAMP,
	[Color] VARCHAR(30) NOT NULL DEFAULT 'White',
	[Icon] INT NOT NULL DEFAULT 0,
	[Text] VARCHAR(50) NOT NULL
)

CREATE TABLE PlayersItems
(
	[ItemId] BIGINT NOT NULL PRIMARY KEY,
	[Name] VARCHAR(45) NOT NULL,
	[Serial] BIGINT NOT NULL DEFAULT 0,
	[ItemPane] VARCHAR(9) NOT NULL DEFAULT 'Ground',
	[Slot] INT NOT NULL DEFAULT 0,
    [InventorySlot] INT NOT NULL DEFAULT 0,
	[Color] INT NOT NULL DEFAULT 0,
	[Cursed] BIT NOT NULL DEFAULT 0,
	[Durability] BIGINT NOT NULL DEFAULT 0,
	[Identified] BIT NOT NULL DEFAULT 0,
	[ItemVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[WeapVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[ItemQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[OriginalQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[Stacks] INT NOT NULL DEFAULT 0,
	[Enchantable] BIT NOT NULL DEFAULT 0,
    [Tarnished] BIT NOT NULL DEFAULT 0,
    [GearEnhancement] VARCHAR(5) NOT NULL DEFAULT 'None',
    [ItemMaterial] VARCHAR(9) NOT NULL DEFAULT 'None',
    [GiftWrapped] VARCHAR(20) NULL
)

CREATE TABLE PlayersCombos
(
    [Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
    [Combo1] VARCHAR(30) NULL,
    [Combo2] VARCHAR(30) NULL,
    [Combo3] VARCHAR(30) NULL,
    [Combo4] VARCHAR(30) NULL,
    [Combo5] VARCHAR(30) NULL,
    [Combo6] VARCHAR(30) NULL,
    [Combo7] VARCHAR(30) NULL,
    [Combo8] VARCHAR(30) NULL,
    [Combo9] VARCHAR(30) NULL,
    [Combo10] VARCHAR(30) NULL,
    [Combo11] VARCHAR(30) NULL,
    [Combo12] VARCHAR(30) NULL,
    [Combo13] VARCHAR(30) NULL,
    [Combo14] VARCHAR(30) NULL,
    [Combo15] VARCHAR(30) NULL,
)

CREATE TABLE PlayersQuests
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
    [MailBoxNumber] int NOT NULL,
    [TutorialCompleted] BIT NULL,
    [BetaReset] BIT NULL,
    [ArtursGift] INT NULL,
    [CamilleGreetingComplete] BIT NULL,
    [ConnPotions] BIT NULL,
    [CryptTerror] BIT NULL,
    [CryptTerrorSlayed] BIT NULL,
    [CryptTerrorContinued] BIT NULL,
    [CryptTerrorContSlayed] BIT NULL,
    [NightTerror] BIT NULL,
    [NightTerrorSlayed] BIT NULL,
    [DreamWalking] BIT NULL,
    [DreamWalkingSlayed] BIT NULL,
    [Dar] INT NULL,
    [DarItem] VARCHAR (20) NULL,
    [ReleasedTodesbaum] BIT NULL,
    [DrunkenHabit] BIT NULL,
    [FionaDance] BIT NULL,
    [Keela] INT NULL,
    [KeelaCount] INT NULL,
    [KeelaKill] VARCHAR (20) NULL,
    [KeelaQuesting] BIT NULL,
    [KillerBee] BIT NULL,
    [Neal] INT NULL,
    [NealCount] INT NULL,
    [NealKill] VARCHAR (20) NULL,
    [AbelShopAccess] BIT NULL,
    [PeteKill] INT NULL,
    [PeteComplete] BIT NULL,
    [SwampAccess] BIT NULL,
    [SwampCount] INT NULL,
    [TagorDungeonAccess] BIT NULL,
    [Lau] INT NULL,
    [BeltDegree] VARCHAR (6) NULL,
    [MilethReputation] INT NULL,
    [AbelReputation] INT NULL,
    [RucesionReputation] INT NULL,
    [SuomiReputation] INT NULL,
    [RionnagReputation] INT NULL,
    [OrenReputation] INT NULL,
    [PietReputation] INT NULL,
    [LouresReputation] INT NULL,
    [UndineReputation] INT NULL,
    [TagorReputation] INT NULL,
    [BlackSmithing] INT NULL,
    [BlackSmithingTier] VARCHAR (10) NULL,
    [ArmorSmithing] INT NULL,
    [ArmorSmithingTier] VARCHAR (10) NULL,
    [JewelCrafting] INT NULL,
    [JewelCraftingTier] VARCHAR (10) NULL,
    [StoneSmithing] INT NULL,
    [StoneSmithingTier] VARCHAR (10) NULL,
    [ThievesGuildReputation] INT NULL,
    [AssassinsGuildReputation] INT NULL,
    [AdventuresGuildReputation] INT NULL,
    [BeltQuest] VARCHAR (6) NULL,
    [SavedChristmas] BIT NULL,
    [RescuedReindeer] BIT NULL,
    [YetiKilled] BIT NULL,
    [UnknownStart] BIT NULL,
    [PirateShipAccess] BIT NULL,
    [ScubaSchematics] BIT NULL,
    [ScubaMaterialsQuest] BIT NULL,
    [ScubaGearCrafted] BIT NULL,
    [EternalLove] BIT NULL,
    [EternalLoveStarted] BIT NULL,
    [UnhappyEnding] BIT NULL,
    [HonoringTheFallen] BIT NULL,
    [ReadTheFallenNotes] BIT NULL,
    [GivenTarnishedBreastplate] BIT NULL,
    [EternalBond] VARCHAR (13) NULL,
    [ArmorCraftingCodex] BIT NULL,
    [ArmorApothecaryAccepted] BIT NULL,
    [ArmorCodexDeciphered] BIT NULL,
    [ArmorCraftingCodexLearned] BIT NULL,
    [ArmorCraftingAdvancedCodexLearned] BIT NULL,
    [CthonicKillTarget] VARCHAR(30) NULL,
    [CthonicFindTarget] VARCHAR(30) NULL,
    [CthonicKillCompletions] INT NULL,
    [CthonicCleansingOne] BIT NULL,
    [CthonicCleansingTwo] BIT NULL,
    [CthonicDepthsCleansing] BIT NULL,
    [CthonicRuinsAccess] BIT NULL,
    [CthonicRemainsExplorationLevel] INT NULL,
    [EndedOmegasRein] BIT NULL
)

CREATE TABLE PlayersIgnoreList
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[PlayerIgnored] VARCHAR(12) NOT NULL,
)

CREATE TYPE dbo.PlayerType AS TABLE
(
    [Serial] BIGINT,
	[Created] DATETIME,
	[Username] VARCHAR(12),
	[LoggedIn] BIT,
	[LastLogged] DATETIME,
	[X] TINYINT,
	[Y] TINYINT,
	[CurrentMapId] INT,
	[Direction] TINYINT,
	[CurrentHp] INT,
	[BaseHp] INT,
	[CurrentMp] INT,
	[BaseMp] INT,
	[_ac] SMALLINT,
	[_Regen] SMALLINT,
	[_Dmg] SMALLINT,
	[_Hit] SMALLINT,
	[_Mr] SMALLINT,
	[_Str] SMALLINT,
	[_Int] SMALLINT,
	[_Wis] SMALLINT,
	[_Con] SMALLINT,
	[_Dex] SMALLINT,
	[_Luck] SMALLINT,
	[AbpLevel] INT,
	[AbpNext] INT,
	[AbpTotal] BIGINT,
	[ExpLevel] INT,
	[ExpNext] BIGINT,
	[ExpTotal] BIGINT,
	[Stage] VARCHAR(10),
    [JobClass] VARCHAR(12),
	[Path] VARCHAR(10),
	[PastClass] VARCHAR(10),
	[Race] VARCHAR(10),
	[Afflictions] VARCHAR(120),
	[Gender] VARCHAR(6),
	[HairColor] TINYINT,
	[HairStyle] TINYINT,
	[NameColor] TINYINT,
	[Nation] VARCHAR(30),
	[Clan] VARCHAR(20),
	[ClanRank] VARCHAR(20),
	[ClanTitle] VARCHAR(20),
	[MonsterForm] SMALLINT,
	[ActiveStatus] VARCHAR(15),
	[Flags] VARCHAR(6),
	[CurrentWeight] SMALLINT,
	[World] TINYINT,
	[Lantern] TINYINT,
	[Invisible] BIT,
	[Resting] VARCHAR(13),
	[FireImmunity] BIT,
	[WaterImmunity] BIT,
	[WindImmunity] BIT,
	[EarthImmunity] BIT,
	[LightImmunity] BIT,
	[DarkImmunity] BIT,
	[PoisonImmunity] BIT,
	[EnticeImmunity] BIT,
	[PartyStatus] VARCHAR(21),
	[RaceSkill] VARCHAR(20),
	[RaceSpell] VARCHAR(20),
	[GameMaster] BIT,
	[ArenaHost] BIT,
	[Knight] BIT,
	[GoldPoints] BIGINT,
	[StatPoints] SMALLINT,
	[GamePoints] BIGINT,
	[BankedGold] BIGINT,
	[ArmorImg] SMALLINT,
	[HelmetImg] SMALLINT,
	[ShieldImg] SMALLINT,
	[WeaponImg] SMALLINT,
	[BootsImg] SMALLINT,
    [HeadAccessoryImg] SMALLINT,
	[Accessory1Img] SMALLINT,
	[Accessory2Img] SMALLINT,
    [Accessory3Img] SMALLINT,
    [Accessory1Color] TINYINT,
    [Accessory2Color] TINYINT,
    [Accessory3Color] TINYINT,
    [BodyColor] TINYINT,
    [BodySprite] TINYINT,
    [FaceSprite] TINYINT,
    [OverCoatImg] SMALLINT,
	[BootColor] TINYINT,
	[OverCoatColor] TINYINT,
	[Pants] TINYINT
);

CREATE TYPE dbo.ComboType AS TABLE
(
    Serial BIGINT,
    Combo1 VARCHAR(30),
    Combo2 VARCHAR(30),
    Combo3 VARCHAR(30),
    Combo4 VARCHAR(30),
    Combo5 VARCHAR(30),
    Combo6 VARCHAR(30),
    Combo7 VARCHAR(30),
    Combo8 VARCHAR(30),
    Combo9 VARCHAR(30),
    Combo10 VARCHAR(30),
    Combo11 VARCHAR(30),
    Combo12 VARCHAR(30),
    Combo13 VARCHAR(30),
    Combo14 VARCHAR(30),
    Combo15 VARCHAR(30)
);

CREATE TYPE dbo.QuestType AS TABLE
(
    Serial BIGINT,
	MailBoxNumber INT,
    TutorialCompleted BIT,
    BetaReset BIT,
    ArtursGift INT,
    CamilleGreetingComplete BIT,
    ConnPotions BIT,
    CryptTerror BIT,
    CryptTerrorSlayed BIT,
	CryptTerrorContinued BIT,
    CryptTerrorContSlayed BIT,
	NightTerror BIT,
    NightTerrorSlayed BIT,
	DreamWalking BIT,
    DreamWalkingSlayed BIT,
    Dar INT,
    DarItem VARCHAR (20),
	ReleasedTodesbaum BIT,
    DrunkenHabit BIT,
    FionaDance BIT,
    Keela INT,
    KeelaCount INT,
    KeelaKill VARCHAR (20),
    KeelaQuesting BIT,
    KillerBee BIT,
    Neal INT,
    NealCount INT,
    NealKill VARCHAR (20),
    AbelShopAccess BIT,
    PeteKill INT,
    PeteComplete BIT,
    SwampAccess BIT,
    SwampCount INT,
    TagorDungeonAccess BIT,
    Lau INT,
    BeltDegree VARCHAR (6),
    MilethReputation INT,
    AbelReputation INT,
    RucesionReputation INT,
    SuomiReputation INT,
    RionnagReputation INT,
    OrenReputation INT,
    PietReputation INT,
    LouresReputation INT,
    UndineReputation INT,
    TagorReputation INT,
    BlackSmithing INT,
	BlackSmithingTier VARCHAR (10),
    ArmorSmithing INT,
	ArmorSmithingTier VARCHAR (10),
    JewelCrafting INT,
	JewelCraftingTier VARCHAR (10),
    StoneSmithing INT,
	StoneSmithingTier VARCHAR (10),
    ThievesGuildReputation INT,
    AssassinsGuildReputation INT,
    AdventuresGuildReputation INT,
    BeltQuest VARCHAR (6),
    SavedChristmas BIT,
	RescuedReindeer BIT,
	YetiKilled BIT,
	UnknownStart BIT,
	PirateShipAccess BIT,
	ScubaSchematics BIT,
	ScubaMaterialsQuest BIT,
	ScubaGearCrafted BIT,
	EternalLove BIT,
    EternalLoveStarted BIT,
    UnhappyEnding BIT,
    HonoringTheFallen BIT,
    ReadTheFallenNotes BIT,
    GivenTarnishedBreastplate BIT,
	EternalBond VARCHAR (13),
	ArmorCraftingCodex BIT,
	ArmorApothecaryAccepted BIT,
	ArmorCodexDeciphered BIT,
	ArmorCraftingCodexLearned BIT,
	ArmorCraftingAdvancedCodexLearned BIT,
    CthonicKillTarget VARCHAR (30),
    CthonicFindTarget VARCHAR (30),
    CthonicKillCompletions INT,
    CthonicCleansingOne BIT,
    CthonicCleansingTwo BIT,
    CthonicDepthsCleansing BIT,
    CthonicRuinsAccess BIT,
    CthonicRemainsExplorationLevel INT,
    EndedOmegasRein BIT,
    CraftedMoonArmor BIT
	);

CREATE TYPE dbo.ItemType AS TABLE  
(  
    ItemId BIGINT,
    Name VARCHAR(45),
    Serial BIGINT,
    ItemPane VARCHAR(9),
    Slot INT,
    InventorySlot INT,
    Color INT,
    Cursed BIT,
    Durability BIGINT,
    Identified BIT,
    ItemVariance VARCHAR (15),
    WeapVariance VARCHAR (15),
    ItemQuality VARCHAR (10),
    OriginalQuality VARCHAR (10),
    Stacks INT,
    Enchantable BIT,
    Tarnished BIT,
    GearEnhancement VARCHAR (5),
    ItemMaterial VARCHAR (9),
    GiftWrapped VARCHAR (20)
);

CREATE TYPE dbo.SkillType AS TABLE
(
    Serial BIGINT,
    Level INT,
    Slot INT,
    Skill VARCHAR (30),
    Uses INT,
    Cooldown INT
);

CREATE TYPE dbo.SpellType AS TABLE
(
    Serial BIGINT,
    Level INT,
    Slot INT,
    Spell VARCHAR (30),
    Casts INT,
    Cooldown INT
);

CREATE TYPE dbo.BuffType AS TABLE
(
    Serial BIGINT,
    Name VARCHAR (30),
    TimeLeft INT
);

CREATE TYPE dbo.DebuffType AS TABLE
(
    Serial BIGINT,
    Name VARCHAR (30),
    TimeLeft INT
);

-- Obtain MailboxNumber
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ObtainMailBoxNumber] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT MailBoxNumber FROM ZolianPlayers.dbo.PlayersQuests WHERE Serial = @Serial
END

-- Check MailboxNumber
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CheckIfMailBoxNumberExists] @MailBoxNumber INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT Serial FROM ZolianPlayers.dbo.PlayersQuests WHERE MailBoxNumber = @MailBoxNumber
END

-- Item Mass Delete
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ItemMassDelete]
@Items dbo.ItemType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM [ZolianPlayers].[dbo].[PlayersItems]
    WHERE ItemId IN (SELECT ItemId FROM @Items);
END

-- Item Upsert
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ItemUpsert]
@Items dbo.ItemType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE INTO [ZolianPlayers].[dbo].[PlayersItems] AS target
    USING @Items AS source 
	ON target.ItemId = source.ItemId

    WHEN MATCHED THEN
    UPDATE SET 
        Name = source.Name,
        Serial = source.Serial,
        ItemPane = source.ItemPane,
        Slot = source.Slot,
        InventorySlot = source.InventorySlot,
        Color = source.Color,
        Cursed = source.Cursed,
        Durability = source.Durability,
        Identified = source.Identified,
        ItemVariance = source.ItemVariance,
        WeapVariance = source.WeapVariance,
        ItemQuality = source.ItemQuality,
        OriginalQuality = source.OriginalQuality,
        Stacks = source.Stacks,
        Enchantable = source.Enchantable,
        Tarnished = source.Tarnished,
        GearEnhancement = source.GearEnhancement,
        ItemMaterial = source.ItemMaterial,
        GiftWrapped = source.GiftWrapped

    WHEN NOT MATCHED THEN
    INSERT (ItemId, Name, Serial, ItemPane, Slot, InventorySlot, Color, Cursed, Durability, Identified, ItemVariance, WeapVariance, ItemQuality, OriginalQuality, Stacks, Enchantable, Tarnished, GearEnhancement, ItemMaterial, GiftWrapped)
    VALUES (source.ItemId, source.Name, source.Serial, source.ItemPane, source.Slot, source.InventorySlot, source.Color, source.Cursed, source.Durability, source.Identified, source.ItemVariance, source.WeapVariance, source.ItemQuality, source.OriginalQuality, source.Stacks, source.Enchantable, source.Tarnished, source.GearEnhancement, source.ItemMaterial, source.GiftWrapped);
END

-- AddLegendMark
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[AddLegendMark]
@LegendId INT, @Serial BIGINT, @Key VARCHAR(20), @IsPublic BIT, @Time DATETIME,
@Color VARCHAR (25), @Icon INT, @Text VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersLegend]
	([LegendId], [Serial], [Key], [IsPublic], [Time], [Color], [Icon], [Text])
    VALUES	(@LegendId, @Serial, @Key, @IsPublic, @Time, @Color, @Icon, @Text);
END
GO

-- BuffSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[BuffSave]
    @Buffs dbo.BuffType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE target
    SET    target.[Name] = source.[Name],
           target.[TimeLeft] = source.[TimeLeft]
    FROM [ZolianPlayers].[dbo].[PlayersBuffs] AS target
    INNER JOIN @Buffs AS source
    ON target.Serial = source.Serial AND target.Name = source.Name;
END
GO

-- LoadItemsToCache
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[LoadItemsToCache]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM   ZolianPlayers.dbo.PlayersItems;
END
GO

-- CheckIfPlayerExists
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CheckIfPlayerExists] @Name NVARCHAR(12)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT Username FROM ZolianPlayers.dbo.Players WHERE Username = @Name
END
GO

-- CheckIfPlayerHashExists
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CheckIfPlayerHashExists] @Name NVARCHAR(12), @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT Username FROM ZolianPlayers.dbo.Players WHERE Username = @Name AND Serial = @Serial
END
GO

-- CheckIfPlayerSerialExists
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CheckIfPlayerSerialExists] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT Username FROM ZolianPlayers.dbo.Players WHERE Serial = @Serial
END
GO

-- DeBuffSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeBuffSave]
    @Debuffs dbo.DebuffType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE target
    SET    target.[Name] = source.[Name],
           target.[TimeLeft] = source.[TimeLeft]
    FROM [ZolianPlayers].[dbo].[PlayersDebuffs] AS target
    INNER JOIN @Debuffs AS source
    ON target.Serial = source.Serial AND target.Name = source.Name;
END
GO

-- FoundMap
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[FoundMap]
@Serial BIGINT, @MapId INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersDiscoveredMaps] ([Serial], [MapId])
    VALUES (@Serial, @MapId);
END
GO

-- IgnoredSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[IgnoredSave]
@Serial BIGINT, @PlayerIgnored VARCHAR(12)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersIgnoreList] ([Serial], [PlayerIgnored])
    VALUES (@Serial, @PlayerIgnored);
END
GO

-- InsertBuff
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertBuff]
@BuffId INT, @Serial BIGINT, @Name VARCHAR(30), @TimeLeft INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersBuffs] ([BuffId], [Serial], [Name], [TimeLeft])
    VALUES	(@BuffId, @Serial, @Name, @TimeLeft);
END
GO

-- InsertDeBuff
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertDeBuff]
@DebuffId INT, @Serial BIGINT, @Name VARCHAR(30), @TimeLeft INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersDeBuffs] ([DebuffId], [Serial], [Name], [TimeLeft])
    VALUES	(@DebuffId, @Serial, @Name, @TimeLeft);
END
GO

-- InsertQuests
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertQuests]
    @Serial BIGINT, @MailBoxNumber INT, @TutComplete BIT, @BetaReset BIT, @StoneSmith INT, @StoneSmithingTier VARCHAR (10), @MilethRep INT,
	@ArtursGift INT, @CamilleGreeting BIT, @ConnPotions BIT, @CryptTerror BIT, @CryptTerrorSlayed BIT, @CryptTerrorContinued BIT, @CryptTerrorContSlayed BIT,
	@NightTerror BIT, @NightTerrorSlayed BIT, @DreamWalking BIT, @DreamWalkingSlayed BIT, @Dar INT, @DarItem VARCHAR (20), @ReleasedTodesbaum BIT, @DrunkenHabit BIT,
	@Fiona BIT, @Keela INT, @KeelaCount INT, @KeelaKill VARCHAR (20), @KeelaQuesting BIT, @KillerBee BIT, @Neal INT, @NealCount INT,
	@NealKill VARCHAR (20), @AbelShopAccess BIT, @PeteKill INT, @PeteComplete BIT, @SwampAccess BIT, @SwampCount INT, @TagorDungeonAccess BIT, @Lau INT,
    @AbelReputation INT, @RucesionReputation INT, @SuomiReputation INT, @RionnagReputation INT,
    @OrenReputation INT, @PietReputation INT, @LouresReputation INT, @UndineReputation INT,
    @TagorReputation INT, @ThievesGuildReputation INT, @AssassinsGuildReputation INT, @AdventuresGuildReputation INT,
    @BlackSmithing INT, @BlackSmithingTier VARCHAR (10), @ArmorSmithing INT, @ArmorSmithingTier VARCHAR (10),
	@JewelCrafting INT, @JewelCraftingTier VARCHAR (10), @BeltDegree VARCHAR (6), @BeltQuest VARCHAR (6),
    @SavedChristmas BIT, @RescuedReindeer BIT, @YetiKilled BIT, @UnknownStart BIT, @PirateShipAccess BIT,
	@ScubaSchematics BIT, @ScubaMaterialsQuest BIT, @ScubaGearCrafted BIT, @EternalLove BIT, @EternalLoveStarted BIT, @UnhappyEnding BIT,
	@HonoringTheFallen BIT, @ReadTheFallenNotes BIT, @GivenTarnishedBreastplate BIT, @EternalBond VARCHAR (13), @ArmorCraftingCodex BIT,
	@ArmorApothecaryAccepted BIT, @ArmorCodexDeciphered BIT, @ArmorCraftingCodexLearned BIT, @ArmorCraftingAdvancedCodexLearned BIT,
    @CthonicKillTarget VARCHAR(30), @CthonicFindTarget VARCHAR(30), @CthonicKillCompletions INT, @CthonicCleansingOne BIT, @CthonicCleansingTwo BIT,
	@CthonicDepthsCleansing BIT, @CthonicRuinsAccess BIT, @CthonicRemainsExplorationLevel INT, @EndedOmegasRein BIT, @CraftedMoonArmor BIT
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [ZolianPlayers].[dbo].[PlayersQuests] (
        [Serial], [MailBoxNumber], [TutorialCompleted], [BetaReset], [StoneSmithing], [StoneSmithingTier], [MilethReputation],
		[ArtursGift], [CamilleGreetingComplete], [ConnPotions], [CryptTerror], [CryptTerrorSlayed], [CryptTerrorContinued], [CryptTerrorContSlayed],
		[NightTerror], [NightTerrorSlayed], [DreamWalking], [DreamWalkingSlayed], [Dar], [DarItem], [ReleasedTodesbaum], [DrunkenHabit],
		[FionaDance], [Keela], [KeelaCount], [KeelaKill], [KeelaQuesting], [KillerBee], [Neal], [NealCount],
		[NealKill], [AbelShopAccess], [PeteKill], [PeteComplete], [SwampAccess], [SwampCount], [TagorDungeonAccess], [Lau],
        [AbelReputation], [RucesionReputation], [SuomiReputation], [RionnagReputation],
        [OrenReputation], [PietReputation], [LouresReputation], [UndineReputation],
        [TagorReputation], [ThievesGuildReputation], [AssassinsGuildReputation], [AdventuresGuildReputation],
        [BlackSmithing], [BlackSmithingTier], [ArmorSmithing], [ArmorSmithingTier], [JewelCrafting], [JewelCraftingTier],
		[BeltDegree], [BeltQuest], [SavedChristmas], [RescuedReindeer], [YetiKilled], [UnknownStart], [PirateShipAccess],
		[ScubaSchematics], [ScubaMaterialsQuest], [ScubaGearCrafted], [EternalLove], [EternalLoveStarted], [UnhappyEnding],
		[HonoringTheFallen], [ReadTheFallenNotes], [GivenTarnishedBreastplate], [EternalBond], [ArmorCraftingCodex],
		[ArmorApothecaryAccepted], [ArmorCodexDeciphered], [ArmorCraftingCodexLearned], [ArmorCraftingAdvancedCodexLearned],
        [CthonicKillTarget], [CthonicFindTarget], [CthonicKillCompletions], [CthonicCleansingOne], [CthonicCleansingTwo],
        [CthonicDepthsCleansing], [CthonicRuinsAccess], [CthonicRemainsExplorationLevel], [EndedOmegasRein], [CraftedMoonArmor]
    )
    VALUES (
        @Serial, @MailBoxNumber, @TutComplete, @BetaReset, @StoneSmith, @StoneSmithingTier, @MilethRep,
		@ArtursGift, @CamilleGreeting, @ConnPotions, @CryptTerror, @CryptTerrorSlayed, @CryptTerrorContinued, @CryptTerrorContSlayed,
		@NightTerror, @NightTerrorSlayed, @DreamWalking, @DreamWalkingSlayed, @Dar, @DarItem, @ReleasedTodesbaum, @DrunkenHabit,
		@Fiona, @Keela, @KeelaCount, @KeelaKill, @KeelaQuesting, @KillerBee, @Neal, @NealCount,
		@NealKill, @AbelShopAccess, @PeteKill, @PeteComplete, @SwampAccess, @SwampCount, @TagorDungeonAccess, @Lau,
        @AbelReputation, @RucesionReputation, @SuomiReputation, @RionnagReputation,
        @OrenReputation, @PietReputation, @LouresReputation, @UndineReputation,
        @TagorReputation, @ThievesGuildReputation, @AssassinsGuildReputation, @AdventuresGuildReputation,
        @BlackSmithing, @BlackSmithingTier, @ArmorSmithing, @ArmorSmithingTier, @JewelCrafting, @JewelCraftingTier,
		@BeltDegree, @BeltQuest, @SavedChristmas, @RescuedReindeer, @YetiKilled, @UnknownStart, @PirateShipAccess,
		@ScubaSchematics, @ScubaMaterialsQuest, @ScubaGearCrafted, @EternalLove, @EternalLoveStarted, @UnhappyEnding,
		@HonoringTheFallen, @ReadTheFallenNotes, @GivenTarnishedBreastplate, @EternalBond, @ArmorCraftingCodex,
		@ArmorApothecaryAccepted, @ArmorCodexDeciphered, @ArmorCraftingCodexLearned, @ArmorCraftingAdvancedCodexLearned,
        @CthonicKillTarget, @CthonicFindTarget, @CthonicKillCompletions, @CthonicCleansingOne, @CthonicCleansingTwo,
        @CthonicDepthsCleansing, @CthonicRuinsAccess, @CthonicRemainsExplorationLevel, @EndedOmegasRein, @CraftedMoonArmor
    );
END
GO

-- PasswordSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PasswordSave]
@Name VARCHAR (12), @Pass VARCHAR (8), @Attempts INT, @Hacked BIT, @LastIP VARCHAR (15), @LastAttemptIP VARCHAR (15)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[Players]
    SET    [Password]         = @Pass,
           [PasswordAttempts] = @Attempts,
           [Hacked]           = @Hacked,
           [LastIP]           = @LastIP,
           [LastAttemptIP]    = @LastAttemptIP
    WHERE  Username = @Name;
END
GO

-- AccountLockoutCount
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[AccountLockoutCount]
@Name VARCHAR (12), @Attempts INT, @Hacked BIT, @LastAttemptIP VARCHAR (15)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[Players]
    SET    [PasswordAttempts] = @Attempts,
           [Hacked]           = @Hacked,
           [LastAttemptIP]    = @LastAttemptIP
    WHERE  Username = @Name;
END
GO

-- PlayerCreation
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerCreation]
@Serial BIGINT, @Created DATETIME, @UserName VARCHAR (12), @Password VARCHAR (8), @LastLogged DATETIME, @CurrentHp INT, @BaseHp INT,
@CurrentMp INT, @BaseMp INT, @Gender VARCHAR (6), @HairColor TINYINT, @HairStyle TINYINT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[Players] ([Serial], [Created], [Username], [Password], [PasswordAttempts], [Hacked], [LoggedIn], [LastLogged],
    [X], [Y], [CurrentMapId], [Direction], [CurrentHp], [BaseHp], [CurrentMp], [BaseMp], [_ac], [_Regen], [_Dmg], [_Hit], [_Mr], [_Str], [_Int], [_Wis],
    [_Con], [_Dex], [_Luck], [AbpLevel], [AbpNext], [AbpTotal], [ExpLevel], [ExpNext], [ExpTotal], [Stage], [JobClass], [Path], [PastClass], [Race],
    [Afflictions], [Gender], [HairColor], [HairStyle], [NameColor], [Nation], [Clan], [ClanRank], [ClanTitle], [MonsterForm],
    [ActiveStatus], [Flags], [CurrentWeight], [World], [Lantern], [Invisible], [Resting], [FireImmunity], [WaterImmunity], [WindImmunity], [EarthImmunity],
    [LightImmunity], [DarkImmunity], [PoisonImmunity], [EnticeImmunity], [PartyStatus], [RaceSkill], [RaceSpell], [GameMaster], [ArenaHost], [Knight],
    [GoldPoints], [StatPoints], [GamePoints], [BankedGold], [ArmorImg], [HelmetImg], [ShieldImg], [WeaponImg], [BootsImg], [HeadAccessoryImg], [Accessory1Img],
    [Accessory2Img], [Accessory3Img], [Accessory1Color], [Accessory2Color], [Accessory3Color], [BodyColor], [BodySprite], [FaceSprite], [OverCoatImg],
    [BootColor], [OverCoatColor], [Pants])
    VALUES (@Serial, @Created, @UserName, @Password, '0', 'False', 'False', @LastLogged,
    '7', '23', '7000', '0', @CurrentHp, @BaseHp, @CurrentMp, @BaseMp, '0', '0', '0', '0', '0', '5', '5', '5', '5', '5', '0', '0', '0',
    '0', '1', '600', '0', 'Class', 'None', 'Peasant', 'Peasant', 'UnDecided', 'Normal', @Gender, @HairColor, @HairStyle,
    '1', 'Mileth', '', '', '', '0', 'Awake', 'Normal', '0', '0', '0', 'False', 'Standing', 'False', 'False', 'False', 'False', 'False', 'False', 'False', 'False',
    'AcceptingRequests', '', '', 'False', 'False', 'False', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0',
    '0', '0', '0', '0', '0', '0', '0', '0', '0');
END
GO

-- PlayerComboSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerComboSave]
    @Combos dbo.ComboType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    
    MERGE INTO [ZolianPlayers].[dbo].[PlayersCombos] AS target
    USING @Combos AS source
    ON target.Serial = source.Serial

    WHEN MATCHED THEN
    UPDATE SET
        [Combo1] = source.Combo1,
        [Combo2] = source.Combo2,
        [Combo3] = source.Combo3,
        [Combo4] = source.Combo4,
        [Combo5] = source.Combo5,
        [Combo6] = source.Combo6,
        [Combo7] = source.Combo7,
        [Combo8] = source.Combo8,
        [Combo9] = source.Combo9,
        [Combo10] = source.Combo10,
        [Combo11] = source.Combo11,
        [Combo12] = source.Combo12,
        [Combo13] = source.Combo13,
        [Combo14] = source.Combo14,
        [Combo15] = source.Combo15
        
    WHEN NOT MATCHED THEN
    INSERT (Serial, Combo1, Combo2, Combo3, Combo4, Combo5, Combo6, Combo7, Combo8, Combo9, Combo10, Combo11, Combo12, Combo13, Combo14, Combo15)
    VALUES (source.Serial, source.Combo1, source.Combo2, source.Combo3, source.Combo4, source.Combo5, source.Combo6, source.Combo7, source.Combo8, source.Combo9, source.Combo10, source.Combo11, source.Combo12, source.Combo13, source.Combo14, source.Combo15);
END
GO

-- PlayerQuestSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerQuestSave]
    @Quests dbo.QuestType READONLY
AS
BEGIN
    SET NOCOUNT ON;
    
    MERGE INTO [ZolianPlayers].[dbo].[PlayersQuests] AS target
    USING @Quests AS source
    ON target.Serial = source.Serial

    WHEN MATCHED THEN
    UPDATE SET
		[MailBoxNumber] = source.MailBoxNumber,
        [TutorialCompleted] = source.TutorialCompleted,
        [BetaReset] = source.BetaReset,
        [ArtursGift] = source.ArtursGift,
        [CamilleGreetingComplete] = source.CamilleGreetingComplete,
        [ConnPotions] = source.ConnPotions,
        [CryptTerror] = source.CryptTerror,
        [CryptTerrorSlayed] = source.CryptTerrorSlayed,
		[CryptTerrorContinued] = source.CryptTerrorContinued,
		[CryptTerrorContSlayed] = source.CryptTerrorContSlayed,
		[NightTerror] = source.NightTerror,
		[NightTerrorSlayed] = source.NightTerrorSlayed,
		[DreamWalking] = source.DreamWalking,
		[DreamWalkingSlayed] = source.DreamWalkingSlayed,
        [Dar] = source.Dar,
        [DarItem] = source.DarItem,
		[ReleasedTodesbaum] = source.ReleasedTodesbaum,
        [DrunkenHabit] = source.DrunkenHabit,
        [FionaDance] = source.FionaDance,
        [Keela] = source.Keela,
        [KeelaCount] = source.KeelaCount,
        [KeelaKill] = source.KeelaKill,
        [KeelaQuesting] = source.KeelaQuesting,
        [KillerBee] = source.KillerBee,
        [Neal] = source.Neal,
        [NealCount] = source.NealCount,
        [NealKill] = source.NealKill,
        [AbelShopAccess] = source.AbelShopAccess,
        [PeteKill] = source.PeteKill,
        [PeteComplete] = source.PeteComplete,
        [SwampAccess] = source.SwampAccess,
        [SwampCount] = source.SwampCount,
        [TagorDungeonAccess] = source.TagorDungeonAccess,
        [Lau] = source.Lau,
        [BeltDegree] = source.BeltDegree,
        [MilethReputation] = source.MilethReputation,
        [AbelReputation] = source.AbelReputation,
        [RucesionReputation] = source.RucesionReputation,
        [SuomiReputation] = source.SuomiReputation,
        [RionnagReputation] = source.RionnagReputation,
        [OrenReputation] = source.OrenReputation,
        [PietReputation] = source.PietReputation,
        [LouresReputation] = source.LouresReputation,
        [UndineReputation] = source.UndineReputation,
        [TagorReputation] = source.TagorReputation,
        [BlackSmithing] = source.BlackSmithing,
		[BlackSmithingTier] = source.BlackSmithingTier,
        [ArmorSmithing] = source.ArmorSmithing,
		[ArmorSmithingTier] = source.ArmorSmithingTier,
        [JewelCrafting] = source.JewelCrafting,
		[JewelCraftingTier] = source.JewelCraftingTier,
        [StoneSmithing] = source.StoneSmithing,
		[StoneSmithingTier] = source.StoneSmithingTier,
        [ThievesGuildReputation] = source.ThievesGuildReputation,
        [AssassinsGuildReputation] = source.AssassinsGuildReputation,
        [AdventuresGuildReputation] = source.AdventuresGuildReputation,
        [BeltQuest] = source.BeltQuest,
        [SavedChristmas] = source.SavedChristmas,
		[RescuedReindeer] = source.RescuedReindeer,
		[YetiKilled] = source.YetiKilled,
		[UnknownStart] = source.UnknownStart,
		[PirateShipAccess] = source.PirateShipAccess,
		[ScubaSchematics] = source.ScubaSchematics,
		[ScubaMaterialsQuest] = source.ScubaMaterialsQuest,
		[ScubaGearCrafted] = source.ScubaGearCrafted,
        [EternalLove] = source.EternalLove,
        [EternalLoveStarted] = source.EternalLoveStarted,
        [UnhappyEnding] = source.UnhappyEnding,
        [HonoringTheFallen] = source.HonoringTheFallen,
        [ReadTheFallenNotes] = source.ReadTheFallenNotes,
		[GivenTarnishedBreastplate] = source.GivenTarnishedBreastplate,
		[EternalBond] = source.EternalBond,
		[ArmorCraftingCodex] = source.ArmorCraftingCodex,
		[ArmorApothecaryAccepted] = source.ArmorApothecaryAccepted,
		[ArmorCodexDeciphered] = source.ArmorCodexDeciphered,
		[ArmorCraftingCodexLearned] = source.ArmorCraftingCodexLearned,
		[ArmorCraftingAdvancedCodexLearned] = source.ArmorCraftingAdvancedCodexLearned,
        [CthonicKillTarget] = source.CthonicKillTarget,
        [CthonicFindTarget] = source.CthonicFindTarget,
        [CthonicKillCompletions] = source.CthonicKillCompletions,
        [CthonicCleansingOne] = source.CthonicCleansingOne,
        [CthonicCleansingTwo] = source.CthonicCleansingTwo,
        [CthonicDepthsCleansing] = source.CthonicDepthsCleansing,
        [CthonicRuinsAccess] = source.CthonicRuinsAccess,
        [CthonicRemainsExplorationLevel] = source.CthonicRemainsExplorationLevel,
        [EndedOmegasRein] = source.EndedOmegasRein,
        [CraftedMoonArmor] = source.CraftedMoonArmor;
END
GO

-- PlayerSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerSave]
    @Players dbo.PlayerType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    MERGE INTO [ZolianPlayers].[dbo].[Players] AS target
    USING @Players AS source 
    ON target.Serial = source.Serial

    WHEN MATCHED THEN
    UPDATE SET 
        [Created] = source.Created,
        [Username] = source.Username,
        [LoggedIn] = source.LoggedIn,
        [LastLogged] = source.LastLogged,
        [X] = source.X,
        [Y] = source.Y,
        [CurrentMapId] = source.CurrentMapId,
        [Direction] = source.Direction,
        [CurrentHp] = source.CurrentHp,
        [BaseHp] = source.BaseHp,
        [CurrentMp] = source.CurrentMp,
        [BaseMp] = source.BaseMp,
        [_ac] = source._ac,
        [_Regen] = source._Regen,
        [_Dmg] = source._Dmg,
        [_Hit] = source._Hit,
        [_Mr] = source._Mr,
        [_Str] = source._Str,
        [_Int] = source._Int,
        [_Wis] = source._Wis,
        [_Con] = source._Con,
        [_Dex] = source._Dex,
        [_Luck] = source._Luck,
        [AbpLevel] = source.AbpLevel,
        [AbpNext] = source.AbpNext,
        [AbpTotal] = source.AbpTotal,
        [ExpLevel] = source.ExpLevel,
        [ExpNext] = source.ExpNext,
        [ExpTotal] = source.ExpTotal,
        [Stage] = source.Stage,
        [JobClass] = source.JobClass,
        [Path] = source.Path,
        [PastClass] = source.PastClass,
        [Race] = source.Race,
        [Afflictions] = source.Afflictions,
        [Gender] = source.Gender,
        [HairColor] = source.HairColor,
        [HairStyle] = source.HairStyle,
        [NameColor] = source.NameColor,
        [Nation] = source.Nation,
        [Clan] = source.Clan,
        [ClanRank] = source.ClanRank,
        [ClanTitle] = source.ClanTitle,
        [MonsterForm] = source.MonsterForm,
        [ActiveStatus] = source.ActiveStatus,
        [Flags] = source.Flags,
        [CurrentWeight] = source.CurrentWeight,
        [World] = source.World,
        [Lantern] = source.Lantern,
        [Invisible] = source.Invisible,
        [Resting] = source.Resting,
        [FireImmunity] = source.FireImmunity,
        [WaterImmunity] = source.WaterImmunity,
        [WindImmunity] = source.WindImmunity,
        [EarthImmunity] = source.EarthImmunity,
        [LightImmunity] = source.LightImmunity,
        [DarkImmunity] = source.DarkImmunity,
        [PoisonImmunity] = source.PoisonImmunity,
        [EnticeImmunity] = source.EnticeImmunity,
        [PartyStatus] = source.PartyStatus,
        [RaceSkill] = source.RaceSkill,
        [RaceSpell] = source.RaceSpell,
        [GameMaster] = source.GameMaster,
        [ArenaHost] = source.ArenaHost,
        [Knight] = source.Knight,
        [GoldPoints] = source.GoldPoints,
        [StatPoints] = source.StatPoints,
        [GamePoints] = source.GamePoints,
        [BankedGold] = source.BankedGold,
        [ArmorImg] = source.ArmorImg,
        [HelmetImg] = source.HelmetImg,
        [ShieldImg] = source.ShieldImg,
        [WeaponImg] = source.WeaponImg,
        [BootsImg] = source.BootsImg,
        [HeadAccessoryImg] = source.HeadAccessoryImg,
        [Accessory1Img] = source.Accessory1Img,
        [Accessory2Img] = source.Accessory2Img,
        [Accessory3Img] = source.Accessory3Img,
        [Accessory1Color] = source.Accessory1Color,
        [Accessory2Color] = source.Accessory2Color,
        [Accessory3Color] = source.Accessory3Color,
        [BodyColor] = source.BodyColor,
        [BodySprite] = source.BodySprite,
        [FaceSprite] = source.FaceSprite,
        [OverCoatImg] = source.OverCoatImg,
        [BootColor] = source.BootColor,
        [OverCoatColor] = source.OverCoatColor,
        [Pants] = source.Pants;
END
GO

-- PlayerSaveSkills
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerSaveSkills]
    @Skills dbo.SkillType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE target
    SET    target.[Level] = source.[Level],
           target.[Slot] = source.[Slot],
           target.[SkillName] = source.[Skill],
           target.[Uses] = source.[Uses],
           target.[CurrentCooldown] = source.[Cooldown]
    FROM [ZolianPlayers].[dbo].[PlayersSkillBook] AS target
    INNER JOIN @Skills AS source
    ON target.Serial = source.Serial AND target.SkillName = source.Skill;
END
GO

-- PlayerSaveSpells
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerSaveSpells]
    @Spells dbo.SpellType READONLY
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE target
    SET    target.[Level] = source.[Level],
           target.[Slot] = source.[Slot],
           target.[SpellName] = source.[Spell],
           target.[Casts] = source.[Casts],
           target.[CurrentCooldown] = source.[Cooldown]
    FROM [ZolianPlayers].[dbo].[PlayersSpellBook] AS target
    INNER JOIN @Spells AS source
    ON target.Serial = source.Serial AND target.SpellName = source.Spell;
END
GO

-- PlayerSecurity
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerSecurity] @Name NVARCHAR(12)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT Serial, Username, [Password], PasswordAttempts, Hacked, CurrentMapId FROM ZolianPlayers.dbo.Players WHERE Username = @Name
END
GO

-- SelectBuffs
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectBuffs] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersBuffs WHERE Serial = @Serial
END
GO

-- SelectBuffsCheck
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectBuffsCheck] @Serial BIGINT, @Name VARCHAR(30)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersBuffs WHERE Serial = @Serial AND Name = @Name
END
GO

-- SelectDeBuffs
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectDeBuffs] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersDebuffs WHERE Serial = @Serial
END
GO

-- SelectDeBuffsCheck
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectDeBuffsCheck] @Serial BIGINT, @Name VARCHAR(30)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersDebuffs WHERE Serial = @Serial AND Name = @Name
END
GO

-- SelectDiscoveredMaps
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectDiscoveredMaps] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersDiscoveredMaps WHERE Serial = @Serial
END
GO

-- SelectInventory
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectInventory] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersItems WHERE Serial = @Serial AND ItemPane = 'Inventory'
END
GO

-- SelectEquipped
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectEquipped] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersItems WHERE Serial = @Serial AND ItemPane = 'Equip'
END
GO

-- SelectBanked
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectBanked] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersItems WHERE Serial = @Serial AND ItemPane = 'Bank'
END
GO

-- SelectIgnoredPlayers
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectIgnoredPlayers] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersIgnoreList WHERE Serial = @Serial
END
GO

-- SelectLegends
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectLegends] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersLegend WHERE Serial = @Serial
END
GO

-- SelectPlayer
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectPlayer]
@Name NVARCHAR (12)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT [Serial],
           [Created],
           [Username],
           [PasswordAttempts],
           [Hacked],
           [LoggedIn],
           [LastLogged],
           [X],
           [Y],
           [CurrentMapId],
           [Direction],
           [CurrentHp],
           [BaseHp],
           [CurrentMp],
           [BaseMp],
           [_ac],
           [_Regen],
           [_Dmg],
           [_Hit],
           [_Mr],
           [_Str],
           [_Int],
           [_Wis],
           [_Con],
           [_Dex],
           [_Luck],
           [AbpLevel],
           [AbpNext],
           [AbpTotal],
           [ExpLevel],
           [ExpNext],
           [ExpTotal],
           [Stage],
           [JobClass],
           [Path],
           [PastClass],
           [Race],
           [Afflictions],
           [Gender],
           [HairColor],
           [HairStyle],
           [NameColor],
           [Nation],
           [Clan],
           [ClanRank],
           [ClanTitle],
           [MonsterForm],
           [ActiveStatus],
           [Flags],
           [CurrentWeight],
           [World],
           [Lantern],
           [Invisible],
           [Resting],
           [FireImmunity],
           [WaterImmunity],
           [WindImmunity],
           [EarthImmunity],
           [LightImmunity],
           [DarkImmunity],
           [PoisonImmunity],
           [EnticeImmunity],
           [PartyStatus],
           [RaceSkill],
           [RaceSpell],
           [GameMaster],
           [ArenaHost],
           [Knight],
           [GoldPoints],
           [StatPoints],
           [GamePoints],
           [BankedGold],
           [ArmorImg],
           [HelmetImg],
           [ShieldImg],
           [WeaponImg],
           [BootsImg],
           [HeadAccessoryImg],
           [Accessory1Img],
           [Accessory2Img],
           [Accessory3Img],
           [Accessory1Color],
           [Accessory2Color],
           [Accessory3Color],
           [BodyColor],
           [BodySprite],
           [FaceSprite],
           [OverCoatImg],
           [BootColor],
           [OverCoatColor],
           [Pants]
    FROM   [ZolianPlayers].[dbo].[Players]
    WHERE  Username = @Name;
END
GO

-- SelectCombos
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectCombos]
@Serial BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT Combo1, Combo2, Combo3, Combo4, Combo5, Combo6, Combo7, Combo8, Combo9,
           Combo10, Combo11, Combo12, Combo13, Combo14, Combo15
    FROM   [ZolianPlayers].[dbo].[PlayersCombos]
    WHERE  Serial = @Serial;
END
GO

-- SelectQuests
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectQuests]
@Serial BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT MailBoxNumber, TutorialCompleted, BetaReset, StoneSmithing, StoneSmithingTier, MilethReputation, ArtursGift,
           CamilleGreetingComplete, ConnPotions, CryptTerror, CryptTerrorSlayed, CryptTerrorContinued, CryptTerrorContSlayed, 
		   NightTerror, NightTerrorSlayed, DreamWalking, DreamWalkingSlayed, Dar, DarItem, ReleasedTodesbaum, DrunkenHabit, FionaDance,
		   Keela, KeelaCount, KeelaKill, KeelaQuesting, KillerBee, Neal, NealCount, NealKill, AbelShopAccess, PeteKill,
           PeteComplete, SwampAccess, SwampCount, TagorDungeonAccess, Lau,
           AbelReputation, RucesionReputation, SuomiReputation, RionnagReputation,
           OrenReputation, PietReputation, LouresReputation, UndineReputation,
           TagorReputation, ThievesGuildReputation, AssassinsGuildReputation, AdventuresGuildReputation,
           BlackSmithing, BlackSmithingTier, ArmorSmithing, ArmorSmithingTier, JewelCrafting, JewelCraftingTier,
		   BeltDegree, BeltQuest, SavedChristmas, RescuedReindeer, YetiKilled, UnknownStart, PirateShipAccess, 
		   ScubaSchematics, ScubaMaterialsQuest, ScubaGearCrafted, EternalLove, EternalLoveStarted, UnhappyEnding,
		   HonoringTheFallen, ReadTheFallenNotes, GivenTarnishedBreastplate, EternalBond, ArmorCraftingCodex,
		   ArmorApothecaryAccepted, ArmorCodexDeciphered, ArmorCraftingCodexLearned, ArmorCraftingAdvancedCodexLearned,
           CthonicKillTarget, CthonicFindTarget, CthonicKillCompletions, CthonicCleansingOne, CthonicCleansingTwo,
		   CthonicDepthsCleansing, CthonicRuinsAccess, CthonicRemainsExplorationLevel, EndedOmegasRein, CraftedMoonArmor
    FROM   [ZolianPlayers].[dbo].[PlayersQuests]
    WHERE  Serial = @Serial;
END
GO

-- SelectSkills
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectSkills] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersSkillBook WHERE Serial = @Serial
END
GO

-- SelectSpells
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectSpells] @Serial BIGINT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersSpellBook WHERE Serial = @Serial
END
GO

-- SkillToPlayer
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SkillToPlayer]
@Serial BIGINT, @Level INT, @Slot INT,
@SkillName VARCHAR (30), @Uses INT, @CurrentCooldown INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersSkillBook]
	([Serial], [Level], [Slot], [SkillName], [Uses], [CurrentCooldown])
    VALUES	(@Serial, @Level, @Slot, @SkillName, @Uses, @CurrentCooldown);
END
GO

-- SpellToPlayer
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpellToPlayer]
@Serial BIGINT, @Level INT, @Slot INT,
@SpellName VARCHAR (30), @Casts INT, @CurrentCooldown INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersSpellBook]
	([Serial], [Level], [Slot], [SpellName], [Casts], [CurrentCooldown])
    VALUES	(@Serial, @Level, @Slot, @SpellName, @Casts, @CurrentCooldown);
END
GO