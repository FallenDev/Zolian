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

DROP PROCEDURE [dbo].[WithdrawItemList]
DROP PROCEDURE [dbo].[SpellToPlayer]
DROP PROCEDURE [dbo].[SkillToPlayer]
DROP PROCEDURE [dbo].[SelectSpells]
DROP PROCEDURE [dbo].[SelectSkills]
DROP PROCEDURE [dbo].[SelectQuests]
DROP PROCEDURE [dbo].[SelectPlayer]
DROP PROCEDURE [dbo].[SelectLegends]
DROP PROCEDURE [dbo].[SelectInventory]
DROP PROCEDURE [dbo].[SelectIgnoredPlayers]
DROP PROCEDURE [dbo].[SelectEquipped]
DROP PROCEDURE [dbo].[SelectDiscoveredMaps]
DROP PROCEDURE [dbo].[SelectDeBuffsCheck]
DROP PROCEDURE [dbo].[SelectDeBuffs]
DROP PROCEDURE [dbo].[SelectBuffsCheck]
DROP PROCEDURE [dbo].[SelectBuffs]
DROP PROCEDURE [dbo].[SelectBanked]
DROP PROCEDURE [dbo].[PlayerSecurity]
DROP PROCEDURE [dbo].[PlayerSaveSpells]
DROP PROCEDURE [dbo].[PlayerSaveSkills]
DROP PROCEDURE [dbo].[PlayerSave]
DROP PROCEDURE [dbo].[PlayerQuickSave]
DROP PROCEDURE [dbo].[PlayerQuestSave]
DROP PROCEDURE [dbo].[PlayerCreation]
DROP PROCEDURE [dbo].[PasswordSave]
DROP PROCEDURE [dbo].[ItemToEquipped]
DROP PROCEDURE [dbo].[ItemToBank]
DROP PROCEDURE [dbo].[InventoryUpdate]
DROP PROCEDURE [dbo].[InventoryInsert]
DROP PROCEDURE [dbo].[InsertQuests]
DROP PROCEDURE [dbo].[InsertDeBuff]
DROP PROCEDURE [dbo].[InsertBuff]
DROP PROCEDURE [dbo].[IgnoredSave]
DROP PROCEDURE [dbo].[FoundMap]
DROP PROCEDURE [dbo].[DeBuffSave]
DROP PROCEDURE [dbo].[CheckIfPlayerHashExists]
DROP PROCEDURE [dbo].[CheckIfPlayerExists]
DROP PROCEDURE [dbo].[CheckIfItemExists]
DROP PROCEDURE [dbo].[CheckIfInventoryItemExists]
DROP PROCEDURE [dbo].[BuffSave]
DROP PROCEDURE [dbo].[BankItemSaveStacked]
DROP PROCEDURE [dbo].[BankItemSave]
DROP PROCEDURE [dbo].[AddLegendMark]
GO

DROP TABLE PlayersEquipped;
DROP TABLE PlayersInventory;
DROP TABLE PlayersBanked;
DROP TABLE PlayersLegend;
DROP TABLE PlayersSkillBook;
DROP TABLE PlayersSpellBook;
DROP TABLE PlayersDebuffs;
DROP TABLE PlayersBuffs;
DROP TABLE PlayersDiscoveredMaps;
DROP TABLE PlayersQuests;
DROP TABLE PlayersIgnoreList;
DROP TABLE Players;

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE Players
(
    [Serial] INT NOT NULL PRIMARY KEY,
	[Created] DATETIME DEFAULT CURRENT_TIMESTAMP,
	[Username] VARCHAR(12) NOT NULL,
	[Password] VARCHAR(8) NOT NULL,
	[PasswordAttempts] INT NOT NULL DEFAULT 0,
	[Hacked] BIT NOT NULL DEFAULT 0,
	[LoggedIn] BIT NOT NULL DEFAULT 0,
	[LastLogged] DATETIME DEFAULT CURRENT_TIMESTAMP,
	[LastIP] VARCHAR(15) DEFAULT '127.0.0.1',
	[LastAttemptIP] VARCHAR(15) DEFAULT '127.0.0.1',
	[X] INT NOT NULL DEFAULT 0,
	[Y] INT NOT NULL DEFAULT 0,
	[CurrentMapId] INT NOT NULL DEFAULT 3029,
	[OffenseElement] VARCHAR(15) NOT NULL DEFAULT 'None',
	[DefenseElement] VARCHAR(15) NOT NULL DEFAULT 'None',
	[SecondaryOffensiveElement] VARCHAR(15) NOT NULL DEFAULT 'None',
	[SecondaryDefensiveElement] VARCHAR(15) NOT NULL DEFAULT 'None',
	[Direction] INT NOT NULL DEFAULT 0,
	[CurrentHp] INT NOT NULL DEFAULT 0,
	[BaseHp] INT NOT NULL DEFAULT 0,
	[CurrentMp] INT NOT NULL DEFAULT 0,
	[BaseMp] INT NOT NULL DEFAULT 0,
	[_ac] INT NOT NULL DEFAULT 0,
	[_Regen] INT NOT NULL DEFAULT 0,
	[_Dmg] INT NOT NULL DEFAULT 0,
	[_Hit] INT NOT NULL DEFAULT 0,
	[_Mr] INT NOT NULL DEFAULT 0,
	[_Str] INT NOT NULL DEFAULT 0,
	[_Int] INT NOT NULL DEFAULT 0,
	[_Wis] INT NOT NULL DEFAULT 0,
	[_Con] INT NOT NULL DEFAULT 0,
	[_Dex] INT NOT NULL DEFAULT 0,
	[_Luck] INT NOT NULL DEFAULT 0,
	[AbpLevel] INT NOT NULL DEFAULT 0,
	[AbpNext] INT NOT NULL DEFAULT 0,
	[AbpTotal] INT NOT NULL DEFAULT 0,
	[ExpLevel] INT NOT NULL DEFAULT 1,
	[ExpNext] INT NOT NULL DEFAULT 0,
	[ExpTotal] INT NOT NULL DEFAULT 0,
	[Stage] VARCHAR(10) NOT NULL,
	[Path] VARCHAR(10) NOT NULL DEFAULT 'Peasant',
	[PastClass] VARCHAR(10) NOT NULL DEFAULT 'Peasant',
	[Race] VARCHAR(10) NOT NULL DEFAULT 'Human',
	[Afflictions] VARCHAR(10) NOT NULL DEFAULT 'Normal',
	[Gender] VARCHAR(6) NOT NULL DEFAULT 'Both',
	[HairColor] INT NOT NULL DEFAULT 0,
	[HairStyle] INT NOT NULL DEFAULT 0,
	[OldColor] INT NOT NULL DEFAULT 0,
	[OldStyle] INT NOT NULL DEFAULT 0,
	[NameColor] INT NOT NULL DEFAULT 1,
	[ProfileMessage] VARCHAR(100) NULL,
	[Nation] VARCHAR(30) NOT NULL DEFAULT 'Mileth',
	[Clan] VARCHAR(20) NULL,
	[ClanRank] VARCHAR(20) NULL,
	[ClanTitle] VARCHAR(20) NULL,
	[AnimalForm] VARCHAR(10) NOT NULL DEFAULT 'None',
	[MonsterForm] INT NOT NULL DEFAULT 0,
	[ActiveStatus] VARCHAR(15) NOT NULL DEFAULT 'Awake',
	[Flags] VARCHAR(6) NOT NULL DEFAULT 'Normal',
	[CurrentWeight] INT NOT NULL DEFAULT 0,
	[World] INT NOT NULL DEFAULT 0,
	[Lantern] INT NOT NULL DEFAULT 0,
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
	[Developer] BIT NOT NULL DEFAULT 0,
	[Ranger] BIT NOT NULL DEFAULT 0,
	[Knight] BIT NOT NULL DEFAULT 0,
	[GoldPoints] BIGINT NOT NULL DEFAULT 0,
	[StatPoints] INT NOT NULL DEFAULT 0,
	[GamePoints] INT NOT NULL DEFAULT 0,
	[BankedGold] BIGINT NOT NULL DEFAULT 0,
	[Display] VARCHAR(12) NOT NULL DEFAULT 'None',
	[ArmorImg] INT NOT NULL DEFAULT 0,
	[HelmetImg] INT NOT NULL DEFAULT 0,
	[ShieldImg] INT NOT NULL DEFAULT 0,
	[WeaponImg] INT NOT NULL DEFAULT 0,
	[BootsImg] INT NOT NULL DEFAULT 0,
	[HeadAccessory1Img] INT NOT NULL DEFAULT 0,
	[HeadAccessory2Img] INT NOT NULL DEFAULT 0,
	[OverCoatImg] INT NOT NULL DEFAULT 0,
	[BootColor] INT NOT NULL DEFAULT 0,
	[OverCoatColor] INT NOT NULL DEFAULT 0,
	[Pants] INT NOT NULL DEFAULT 0,
	[Aegis] INT NOT NULL DEFAULT 0,
	[Bleeding] INT NOT NULL DEFAULT 0,
	[Spikes] INT NOT NULL DEFAULT 0,
	[Rending] INT NOT NULL DEFAULT 0,
	[Reaping] INT NOT NULL DEFAULT 0,
	[Vampirism] INT NOT NULL DEFAULT 0,
	[Haste] INT NOT NULL DEFAULT 0,
	[Hastened] INT NOT NULL DEFAULT 0,
	[Gust] INT NOT NULL DEFAULT 0,
	[Quake] INT NOT NULL DEFAULT 0,
	[Rain] INT NOT NULL DEFAULT 0,
	[Flame] INT NOT NULL DEFAULT 0,
	[Dusk] INT NOT NULL DEFAULT 0,
	[Dawn] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersDiscoveredMaps
(
	[DiscoveredId] INT NOT NULL PRIMARY KEY,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[MapId] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersBuffs
(
	[BuffId] INT NOT NULL PRIMARY KEY, 
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Name] VARCHAR(30) NULL,
	[TimeLeft] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersDebuffs
(
	[DebuffId] INT NOT NULL PRIMARY KEY,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Name] VARCHAR(30) NULL,
	[TimeLeft] INT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersSpellBook
(
	[SpellId] INT NOT NULL PRIMARY KEY,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Level] INT NOT NULL DEFAULT 0,
	[Slot] INT NULL,
	[SpellName] VARCHAR(30) NULL,
	[Casts] INT NOT NULL DEFAULT 0,
	[CurrentCooldown] INT NULL
)

CREATE TABLE PlayersSkillBook
(
	[SkillId] INT NOT NULL PRIMARY KEY,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Level] INT NOT NULL DEFAULT 0,
	[Slot] INT NULL,
	[SkillName] VARCHAR(30) NULL,
	[Uses] INT NOT NULL DEFAULT 0,
	[CurrentCooldown] INT NULL
)

CREATE TABLE PlayersLegend
(
	[LegendId] INT NOT NULL PRIMARY KEY,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Category] VARCHAR(20) NOT NULL,
	[Time] DATETIME DEFAULT CURRENT_TIMESTAMP,
	[Color] VARCHAR(25) NOT NULL DEFAULT 'Blue',
	[Icon] INT NOT NULL DEFAULT 0,
	[Value] VARCHAR(50) NOT NULL
)

CREATE TABLE PlayersBanked
(
	[ItemId] INT NOT NULL PRIMARY KEY,
	[Name] VARCHAR(45) NOT NULL,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Color] INT NOT NULL DEFAULT 0, 
	[Cursed] BIT NOT NULL DEFAULT 0,
	[Durability] INT NOT NULL DEFAULT 0,
	[Identified] BIT NOT NULL DEFAULT 0,
	[ItemVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[WeapVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[ItemQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[OriginalQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[Stacks] INT NOT NULL DEFAULT 0,
	[Enchantable] BIT NOT NULL DEFAULT 0,
	[Stackable] BIT NULL
)

CREATE TABLE PlayersInventory
(
	[ItemId] INT NOT NULL PRIMARY KEY,
	[Name] VARCHAR(45) NOT NULL,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Color] INT NOT NULL DEFAULT 0, 
	[Cursed] BIT NOT NULL DEFAULT 0,
	[Durability] INT NOT NULL DEFAULT 0,
	[Identified] BIT NOT NULL DEFAULT 0,
	[ItemVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[WeapVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[ItemQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[OriginalQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[InventorySlot] INT NOT NULL DEFAULT 0,
	[Stacks] INT NOT NULL DEFAULT 0,
	[Enchantable] BIT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersEquipped
(
	[ItemId] INT NOT NULL PRIMARY KEY,
	[Name] VARCHAR(45) NOT NULL,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[Slot] INT NOT NULL DEFAULT 0,
	[Color] INT NOT NULL DEFAULT 0,
	[Cursed] BIT NOT NULL DEFAULT 0,
	[Durability] INT NOT NULL DEFAULT 0,
	[Identified] BIT NOT NULL DEFAULT 0,
	[ItemVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[WeapVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[ItemQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[OriginalQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[Stacks] INT NOT NULL DEFAULT 0,
	[Enchantable] BIT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersQuests
(
	[QuestId] INT NOT NULL PRIMARY KEY,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[TutorialCompleted] BIT NULL,
	[BetaReset] BIT NULL,
	[StoneSmithing] INT NULL,
	[MilethReputation] INT NULL,
	[ArtursGift] INT NULL,
	[CamilleGreetingComplete] BIT NULL,
	[ConnPotions] BIT NULL,
	[CryptTerror] BIT NULL,
	[CryptTerrorSlayed] BIT NULL,
	[Dar] INT NULL,
	[DarItem] VARCHAR(20) NULL,
	[DrunkenHabit] BIT NULL,
	[EternalLove] BIT NULL,
	[FionaDance] BIT NULL,
	[Keela] INT NULL,
	[KeelaCount] INT NULL,
	[KeelaKill] VARCHAR(20) NULL,
	[KeelaQuesting] BIT NULL,
	[KillerBee] BIT NULL,
	[Neal] INT NULL,
	[NealCount] INT NULL,
	[NealKill] VARCHAR(20) NULL,
	[AbelShopAccess] BIT NULL,
	[PeteKill] INT NULL,
	[PeteComplete] BIT NULL,
	[SwampAccess] BIT NULL,
	[SwampCount] INT NULL
)

CREATE TABLE PlayersIgnoreList
(
	[Id] INT NOT NULL PRIMARY KEY,
	[Serial] INT FOREIGN KEY REFERENCES Players(Serial),
	[PlayerIgnored] VARCHAR(12) NOT NULL,
)

-- AddLegendMark
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[AddLegendMark]
@LegendId INT, @Serial INT, @Category VARCHAR(20), @Time DATETIME,
@Color VARCHAR (25), @Icon INT, @Value VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersLegend]
	([LegendId], [Serial], [Category], [Time], [Color], [Icon], [Value])
    VALUES	(@LegendId, @Serial, @Category, @Time, @Color, @Icon, @Value);
END
GO

-- BankItemSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[BankItemSave]
@ItemId INT, @Name VARCHAR(45), @Serial INT, @Color INT, @Cursed BIT, @Durability INT, @Identified BIT, @ItemVariance VARCHAR(15),
@WeapVariance VARCHAR(15), @ItemQuality VARCHAR(10), @OriginalQuality VARCHAR(10), @Stacks INT, @Enchantable BIT, @CanStack BIT, @Tarnished BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersBanked]
    SET
	[Name] = @Name,
	[Color] = @Color,
	[Cursed] = @Cursed,
	[Durability] = @Durability,
	[Identified] = @Identified,
	[ItemVariance] = @ItemVariance,
	[WeapVariance] = @WeapVariance,
	[ItemQuality] = @ItemQuality,
	[OriginalQuality] = @OriginalQuality,
	[Stacks] = @Stacks,
	[Enchantable] = @Enchantable,
	[Stackable] = @CanStack,
    [Tarnished] = @Tarnished
    WHERE  Serial = @Serial AND ItemId = @ItemId;
END
GO

-- BankItemSaveStacked
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[BankItemSaveStacked]
@ItemId INT, @Name VARCHAR(45), @Serial INT, @Color INT, @Cursed BIT, @Durability INT, @Identified BIT, @ItemVariance VARCHAR(15),
@WeapVariance VARCHAR(15), @ItemQuality VARCHAR(10), @OriginalQuality VARCHAR(10), @Stacks INT, @Enchantable BIT, @CanStack BIT, @Tarnished BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersBanked]
    SET
	[Name] = @Name,
	[Color] = @Color,
	[Cursed] = @Cursed,
	[Durability] = @Durability,
	[Identified] = @Identified,
	[ItemVariance] = @ItemVariance,
	[WeapVariance] = @WeapVariance,
	[ItemQuality] = @ItemQuality,
	[OriginalQuality] = @OriginalQuality,
	[Stacks] = @Stacks,
	[Enchantable] = @Enchantable,
	[Stackable] = @CanStack,
    [Tarnished] = @Tarnished
    WHERE  Serial = @Serial AND [Name] = @Name;
END
GO

-- BuffSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[BuffSave]
@Serial INT, @Name VARCHAR(30), @TimeLeft INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersBuffs]
    SET
	[TimeLeft] = @TimeLeft
    WHERE  Serial = @Serial AND [Name] = @Name;
END
GO

-- CheckIfInventoryItemExists
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CheckIfInventoryItemExists]
@ItemId INT, @Serial INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT *
    FROM   ZolianPlayers.dbo.PlayersInventory
    WHERE  [ItemId] = @ItemId
           AND Serial = @Serial;
END
GO

-- CheckIfItemExists
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[CheckIfItemExists] @Name NVARCHAR(45), @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersBanked WHERE [Name] = @Name AND Serial = @Serial
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
CREATE PROCEDURE [dbo].[CheckIfPlayerHashExists] @Name NVARCHAR(12), @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT Username FROM ZolianPlayers.dbo.Players WHERE Username = @Name AND Serial = @Serial
END
GO

-- DeBuffSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeBuffSave]
@Serial INT, @Name VARCHAR(30), @TimeLeft INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersDebuffs]
    SET
	[TimeLeft] = @TimeLeft
    WHERE  Serial = @Serial AND [Name] = @Name;
END
GO

-- FoundMap
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[FoundMap]
@DiscoveredId INT, @Serial INT, @MapId INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersDiscoveredMaps] ([DiscoveredId], [Serial], [MapId])
    VALUES (@DiscoveredId, @Serial, @MapId);
END
GO

-- IgnoredSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[IgnoredSave]
@Id INT, @Serial INT, @PlayerIgnored VARCHAR(12)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersIgnoreList] ([Id], [Serial], [PlayerIgnored])
    VALUES (@Id, @Serial, @PlayerIgnored);
END
GO

-- InsertBuff
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InsertBuff]
@BuffId INT, @Serial INT, @Name VARCHAR(30), @TimeLeft INT
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
@DebuffId INT, @Serial INT, @Name VARCHAR(30), @TimeLeft INT
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
@QuestId INT, @Serial INT, @TutComplete BIT, @BetaReset BIT, @StoneSmith INT, @MilethRep INT, @ArtursGift INT, @CamilleGreeting BIT, @ConnPotions BIT, @CryptTerror BIT, @CryptTerrorSlayed BIT, @Dar INT, @DarItem VARCHAR (20), @EternalLove BIT, @Fiona BIT, @Keela INT, @KeelaCount INT, @KeelaKill VARCHAR (20), @KeelaQuesting BIT, @KillerBee BIT, @Neal INT, @NealCount INT, @NealKill VARCHAR (20), @AbelShopAccess BIT, @PeteKill INT, @PeteComplete BIT, @SwampAccess BIT, @SwampCount INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersQuests] ([QuestId], [Serial], [TutorialCompleted], [BetaReset], [StoneSmithing], [MilethReputation], [ArtursGift], [CamilleGreetingComplete], [ConnPotions], [CryptTerror], [CryptTerrorSlayed], [Dar], [DarItem], [EternalLove], [FionaDance], [Keela], [KeelaCount], [KeelaKill], [KeelaQuesting], [KillerBee], [Neal], [NealCount], [NealKill], [AbelShopAccess], [PeteKill], [PeteComplete], [SwampAccess], [SwampCount])
    VALUES                                            (@QuestId, @Serial, @TutComplete, @BetaReset, @StoneSmith, @MilethRep, @ArtursGift, @CamilleGreeting, @ConnPotions, @CryptTerror, @CryptTerrorSlayed, @Dar, @DarItem, @EternalLove, @Fiona, @Keela, @KeelaCount, @KeelaKill, @KeelaQuesting, @KillerBee, @Neal, @NealCount, @NealKill, @AbelShopAccess, @PeteKill, @PeteComplete, @SwampAccess, @SwampCount);
END
GO

-- InventoryInsert
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InventoryInsert]
@ItemId INT, @Name VARCHAR (45), @Serial INT, @Color INT, @Cursed BIT, @Durability INT, @Identified BIT, @ItemVariance VARCHAR (15), @WeapVariance VARCHAR (15), @ItemQuality VARCHAR (10), @OriginalQuality VARCHAR (10), @InventorySlot INT, @Stacks INT, @Enchantable BIT, @Tarnished BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersInventory] ([ItemId], [Name], [Serial], [Color], [Cursed], [Durability], [Identified], [ItemVariance], [WeapVariance], [ItemQuality], [OriginalQuality], [InventorySlot], [Stacks], [Enchantable], [Tarnished])
    VALUES                                               (@ItemId, @Name, @Serial, @Color, @Cursed, @Durability, @Identified, @ItemVariance, @WeapVariance, @ItemQuality, @OriginalQuality, @InventorySlot, @Stacks, @Enchantable, @Tarnished);
END
GO

-- InventoryUpdate
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[InventoryUpdate]
@ItemId INT, @Name VARCHAR (45), @Serial INT, @Color INT, @Cursed BIT, @Durability INT, @Identified BIT, @ItemVariance VARCHAR (15), @WeapVariance VARCHAR (15), @ItemQuality VARCHAR (10), @OriginalQuality VARCHAR (10), @InventorySlot INT, @Stacks INT, @Enchantable BIT, @Tarnished BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersInventory]
    SET    [ItemId]          = @ItemId,
           [Name]            = @Name,
           [Serial]          = @Serial,
           [Color]           = @Color,
           [Cursed]          = @Cursed,
           [Durability]      = @Durability,
           [Identified]      = @Identified,
           [ItemVariance]    = @ItemVariance,
           [WeapVariance]    = @WeapVariance,
           [ItemQuality]     = @ItemQuality,
           [OriginalQuality] = @OriginalQuality,
           [InventorySlot]   = @InventorySlot,
           [Stacks]          = @Stacks,
           [Enchantable]     = @Enchantable,
           [Tarnished]       = @Tarnished
    WHERE  Serial = @Serial
           AND ItemId = @ItemId;
END
GO

-- ItemToBank
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ItemToBank]
@ItemId INT, @Name VARCHAR (45), @Serial INT, @Color INT, @Cursed BIT, @Durability INT, @Identified BIT, @ItemVariance VARCHAR (15),
@WeapVariance VARCHAR (15), @ItemQuality VARCHAR (10), @OriginalQuality VARCHAR (10), @Stacks INT, @Enchantable BIT, @CanStack BIT, @Tarnished BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersBanked] ([ItemId], [Name], [Serial], [Color], [Cursed], [Durability],
	[Identified], [ItemVariance], [WeapVariance], [ItemQuality], [OriginalQuality], [Stacks], [Enchantable], [Stackable], [Tarnished])
    VALUES	(@ItemId, @Name, @Serial, @Color, @Cursed, @Durability, 
	@Identified, @ItemVariance, @WeapVariance, @ItemQuality, @OriginalQuality, 
	@Stacks, @Enchantable, @CanStack, @Tarnished);
END
GO

-- ItemToEquipped
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[ItemToEquipped]
@ItemId INT, @Name VARCHAR(45), @Serial INT, @Color INT, @Cursed BIT, @Durability INT, @Identified BIT, @ItemVariance VARCHAR(15),
@WeapVariance VARCHAR(15), @ItemQuality VARCHAR(10), @OriginalQuality VARCHAR(10), @Slot INT, @Stacks INT, @Enchantable BIT, @Tarnished BIT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersEquipped] ([ItemId], [Name], [Serial], [Color], [Cursed], [Durability], [Identified],
	[ItemVariance], [WeapVariance], [ItemQuality], [OriginalQuality], [Slot], [Stacks], [Enchantable], [Tarnished])
    VALUES (@ItemId, @Name, @Serial, @Color, @Cursed, @Durability, @Identified, @ItemVariance, @WeapVariance, @ItemQuality, @OriginalQuality,
	@Slot, @Stacks, @Enchantable, @Tarnished);
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

-- PlayerCreation
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerCreation]
@Serial INT, @Created DATETIME, @UserName VARCHAR (12), @Password VARCHAR (8), @LastLogged DATETIME, @CurrentHp INT, @BaseHp INT, @CurrentMp INT, @BaseMp INT, @Gender VARCHAR (6), @HairColor INT, @HairStyle INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[Players] ([Serial], [Created], [Username], [Password], [PasswordAttempts], [Hacked], [LoggedIn], [LastLogged], [X], [Y], [CurrentMapId], [OffenseElement], [DefenseElement], [SecondaryOffensiveElement], [SecondaryDefensiveElement], [Direction], [CurrentHp], [BaseHp], [CurrentMp], [BaseMp], [_ac], [_Regen], [_Dmg], [_Hit], [_Mr], [_Str], [_Int], [_Wis], [_Con], [_Dex], [_Luck], [AbpLevel], [AbpNext], [AbpTotal], [ExpLevel], [ExpNext], [ExpTotal], [Stage], [Path], [PastClass], [Race], [Afflictions], [Gender], [HairColor], [HairStyle], [OldColor], [OldStyle], [NameColor], [ProfileMessage], [Nation], [Clan], [ClanRank], [ClanTitle], [AnimalForm], [MonsterForm], [ActiveStatus], [Flags], [CurrentWeight], [World], [Lantern], [Invisible], [Resting], [FireImmunity], [WaterImmunity], [WindImmunity], [EarthImmunity], [LightImmunity], [DarkImmunity], [PoisonImmunity], [EnticeImmunity], [PartyStatus], [RaceSkill], [RaceSpell], [GameMaster], [ArenaHost], [Developer], [Ranger], [Knight], [GoldPoints], [StatPoints], [GamePoints], [BankedGold], [ArmorImg], [HelmetImg], [ShieldImg], [WeaponImg], [BootsImg], [HeadAccessory1Img], [HeadAccessory2Img], [OverCoatImg], [BootColor], [OverCoatColor], [Pants], [Aegis], [Bleeding], [Spikes], [Rending], [Reaping], [Vampirism], [Haste], [Gust], [Quake], [Rain], [Flame], [Dusk], [Dawn])
    VALUES                                      (@Serial, @Created, @UserName, @Password, '0', 'False', 'False', @LastLogged, '7', '23', '7000', 'None', 'None', 'None', 'None', '0', @CurrentHp, @BaseHp, @CurrentMp, @BaseMp, '0', '0', '0', '0', '0', '5', '5', '5', '5', '5', '0', '0', '0', '0', '1', '600', '0', 'Class', 'Peasant', 'Peasant', 'UnDecided', 'Normal', @Gender, @HairColor, @HairStyle, @HairColor, @HairStyle, '1', '', 'Mileth', '', '', '', 'None', '0', 'Awake', 'Normal', '0', '0', '0', 'False', 'Standing', 'False', 'False', 'False', 'False', 'False', 'False', 'False', 'False', 'AcceptingRequests', '', '', 'False', 'False', 'False', 'False', 'False', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0');
END
GO

-- PlayerQuestSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerQuestSave]
@Serial INT, @TutComplete BIT, @BetaReset BIT, @StoneSmith INT, @MilethRep INT, @ArtursGift INT, @CamilleGreeting BIT, @ConnPotions BIT, @CryptTerror BIT, @CryptTerrorSlayed BIT, @Dar INT, @DarItem VARCHAR (20), @EternalLove BIT, @Fiona BIT, @Keela INT, @KeelaCount INT, @KeelaKill VARCHAR (20), @KeelaQuesting BIT, @KillerBee BIT, @Neal INT, @NealCount INT, @NealKill VARCHAR (20), @AbelShopAccess BIT, @PeteKill INT, @PeteComplete BIT, @SwampAccess BIT, @SwampCount INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersQuests]
    SET    [TutorialCompleted]       = @TutComplete,
           [BetaReset]               = @BetaReset,
           [StoneSmithing]           = @StoneSmith,
           [MilethReputation]        = @MilethRep,
           [ArtursGift]              = @ArtursGift,
           [CamilleGreetingComplete] = @CamilleGreeting,
           [ConnPotions]             = @ConnPotions,
           [CryptTerror]             = @CryptTerror,
           [CryptTerrorSlayed]       = @CryptTerrorSlayed,
           [Dar]                     = @Dar,
           [DarItem]                 = @DarItem,
           [EternalLove]             = @EternalLove,
           [FionaDance]              = @Fiona,
           [Keela]                   = @Keela,
           [KeelaCount]              = @KeelaCount,
           [KeelaKill]               = @KeelaKill,
           [KeelaQuesting]           = @KeelaQuesting,
           [KillerBee]               = @KillerBee,
           [Neal]                    = @Neal,
           [NealCount]               = @NealCount,
           [NealKill]                = @NealKill,
           [AbelShopAccess]          = @AbelShopAccess,
           [PeteKill]                = @PeteKill,
           [PeteComplete]            = @PeteComplete,
           [SwampAccess]             = @SwampAccess,
           [SwampCount]              = @SwampCount
    WHERE  Serial = @Serial;
END
GO

-- PlayerQuickSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerQuickSave]
@Name VARCHAR (12), @X INT, @Y INT, @CurrentMap INT, @OffensePrimary VARCHAR (15), @DefensePrimary VARCHAR (15), @OffenseSecondary VARCHAR (15), @DefenseSecondary VARCHAR (15), @Direction INT, @CurrentHp INT, @BaseHp INT, @CurrentMp INT, @BaseMp INT, @AC INT, @Regen INT, @Dmg INT, @Hit INT, @Mr INT, @Str INT, @Int INT, @Wis INT, @Con INT, @Dex INT, @Luck INT, @ABL INT, @ABN INT, @ABT INT, @EXPL INT, @EXPN INT, @EXPT INT, @Afflix VARCHAR (10), @HairColor INT, @HairStyle INT, @OldColor INT, @OldStyle INT, @Animal VARCHAR (10), @Monster INT, @Active VARCHAR (15), @Flags VARCHAR (6), @CurrentWeight INT, @World INT, @Lantern INT, @Invisible BIT, @Resting VARCHAR (13), @PartyStatus VARCHAR (21), @GoldPoints BIGINT, @StatPoints INT, @GamePoints INT, @BankedGold BIGINT, @ArmorImg INT, @HelmetImg INT, @ShieldImg INT, @WeaponImg INT, @BootsImg INT, @HeadAccessory1Img INT, @HeadAccessory2Img INT, @OverCoatImg INT, @BootColor INT, @OverCoatColor INT, @Pants INT, @Aegis INT, @Bleeding INT, @Spikes INT, @Rending INT, @Reaping INT, @Vampirism INT, @Haste INT, @Gust INT, @Quake INT, @Rain INT, @Flame INT, @Dusk INT, @Dawn INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[Players]
    SET    [X]                         = @X,
           [Y]                         = @Y,
           [CurrentMapId]              = @CurrentMap,
           [OffenseElement]            = @OffensePrimary,
           [DefenseElement]            = @DefensePrimary,
           [SecondaryOffensiveElement] = @OffenseSecondary,
           [SecondaryDefensiveElement] = @DefenseSecondary,
           [Direction]                 = @Direction,
           [CurrentHp]                 = @CurrentHp,
           [BaseHp]                    = @BaseHp,
           [CurrentMp]                 = @CurrentMp,
           [BaseMp]                    = @BaseMp,
           [_ac]                       = @AC,
           [_Regen]                    = @Regen,
           [_Dmg]                      = @Dmg,
           [_Hit]                      = @Hit,
           [_Mr]                       = @Mr,
           [_Str]                      = @Str,
           [_Int]                      = @Int,
           [_Wis]                      = @Wis,
           [_Con]                      = @Con,
           [_Dex]                      = @Dex,
           [_Luck]                     = @Luck,
           [AbpLevel]                  = @ABL,
           [AbpNext]                   = @ABN,
           [AbpTotal]                  = @ABT,
           [ExpLevel]                  = @EXPL,
           [ExpNext]                   = @EXPN,
           [ExpTotal]                  = @EXPT,
           [Afflictions]               = @Afflix,
           [HairColor]                 = @HairColor,
           [HairStyle]                 = @HairStyle,
           [OldColor]                  = @OldColor,
           [OldStyle]                  = @OldStyle,
           [AnimalForm]                = @Animal,
           [MonsterForm]               = @Monster,
           [ActiveStatus]              = @Active,
           [Flags]                     = @Flags,
           [CurrentWeight]             = @CurrentWeight,
           [World]                     = @World,
           [Lantern]                   = @Lantern,
           [Invisible]                 = @Invisible,
           [Resting]                   = @Resting,
           [PartyStatus]               = @PartyStatus,
           [GoldPoints]                = @GoldPoints,
           [StatPoints]                = @StatPoints,
           [GamePoints]                = @GamePoints,
           [BankedGold]                = @BankedGold,
           [ArmorImg]                  = @ArmorImg,
           [HelmetImg]                 = @HelmetImg,
           [ShieldImg]                 = @ShieldImg,
           [WeaponImg]                 = @WeaponImg,
           [BootsImg]                  = @BootsImg,
           [HeadAccessory1Img]         = @HeadAccessory1Img,
           [HeadAccessory2Img]         = @HeadAccessory2Img,
           [OverCoatImg]               = @OverCoatImg,
           [BootColor]                 = @BootColor,
           [OverCoatColor]             = @OverCoatColor,
           [Pants]                     = @Pants,
           [Aegis]                     = @Aegis,
           [Bleeding]                  = @Bleeding,
           [Spikes]                    = @Spikes,
           [Rending]                   = @Rending,
           [Reaping]                   = @Reaping,
           [Vampirism]                 = @Vampirism,
           [Haste]                     = @Haste,
           [Gust]                      = @Gust,
           [Quake]                     = @Quake,
           [Rain]                      = @Rain,
           [Flame]                     = @Flame,
           [Dusk]                      = @Dusk,
           [Dawn]                      = @Dawn
    WHERE  Username = @Name;
END
GO

-- PlayerSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerSave]
@Name VARCHAR (12), @LoggedIn BIT, @LastLogged DATETIME, @Stage VARCHAR (10), @Path VARCHAR (10), @PastClass VARCHAR (10), @Race VARCHAR (10), @Gender VARCHAR (6), @NameColor INT, @Profile VARCHAR (100), @Nation VARCHAR (30), @Clan VARCHAR (20), @ClanRank VARCHAR (20), @ClanTitle VARCHAR (20), @FireImm BIT, @WaterImm BIT, @WindImm BIT, @EarthImm BIT, @LightImm BIT, @DarkImm BIT, @PoisonImm BIT, @EnticeImm BIT, @RaceSkill VARCHAR (20), @RaceSpell VARCHAR (20), @GM BIT, @AH BIT, @DEV BIT, @Ranger BIT, @Knight BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[Players]
    SET    [Username]       = @Name,
           [LoggedIn]       = @LoggedIn,
           [LastLogged]     = @LastLogged,
           [Stage]          = @Stage,
           [Path]           = @Path,
           [PastClass]      = @PastClass,
           [Race]           = @Race,
           [Gender]         = @Gender,
           [NameColor]      = @NameColor,
           [ProfileMessage] = @Profile,
           [Nation]         = @Nation,
           [Clan]           = @Clan,
           [ClanRank]       = @ClanRank,
           [ClanTitle]      = @ClanTitle,
           [FireImmunity]   = @FireImm,
           [WaterImmunity]  = @WaterImm,
           [WindImmunity]   = @WindImm,
           [EarthImmunity]  = @EarthImm,
           [LightImmunity]  = @LightImm,
           [DarkImmunity]   = @DarkImm,
           [PoisonImmunity] = @PoisonImm,
		   [EnticeImmunity] = @EnticeImm,
           [RaceSkill]      = @RaceSkill,
           [RaceSpell]      = @RaceSpell,
           [GameMaster]     = @GM,
           [ArenaHost]      = @AH,
           [Developer]      = @DEV,
           [Ranger]         = @Ranger,
           [Knight]         = @Knight
    WHERE  Username = @Name;
END
GO

-- PlayerSaveSkills
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerSaveSkills]
@Serial INT, @Level INT, @Slot INT, @Skill VARCHAR (30), @Uses INT, @Cooldown INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersSkillBook]
    SET    [Level]           = @Level,
           [Slot]            = @Slot,
           [SkillName]       = @Skill,
           [Uses]            = @Uses,
           [CurrentCooldown] = @Cooldown
    WHERE  Serial = @Serial
           AND SkillName = @Skill;
END
GO

-- PlayerSaveSpells
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerSaveSpells]
@Serial INT, @Level INT, @Slot INT, @Spell VARCHAR (30), @Casts INT, @Cooldown INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersSpellBook]
    SET    [Level]           = @Level,
           [Slot]            = @Slot,
           [SpellName]       = @Spell,
           [Casts]           = @Casts,
           [CurrentCooldown] = @Cooldown
    WHERE  Serial = @Serial
           AND SpellName = @Spell;
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

-- SelectBanked
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectBanked] @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersBanked WHERE Serial = @Serial
END
GO

-- SelectBuffs
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectBuffs] @Serial INT
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
CREATE PROCEDURE [dbo].[SelectBuffsCheck] @Serial INT, @Name VARCHAR(30)
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
CREATE PROCEDURE [dbo].[SelectDeBuffs] @Serial INT
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
CREATE PROCEDURE [dbo].[SelectDeBuffsCheck] @Serial INT, @Name VARCHAR(30)
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
CREATE PROCEDURE [dbo].[SelectDiscoveredMaps] @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersDiscoveredMaps WHERE Serial = @Serial
END
GO

-- SelectEquipped
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectEquipped] @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersEquipped WHERE Serial = @Serial
END
GO

-- SelectIgnoredPlayers
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectIgnoredPlayers] @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersIgnoreList WHERE Serial = @Serial
END
GO

-- SelectInventory
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectInventory] @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersInventory WHERE Serial = @Serial
END
GO

-- SelectLegends
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectLegends] @Serial INT
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
           [OffenseElement],
           [DefenseElement],
           [SecondaryOffensiveElement],
           [SecondaryDefensiveElement],
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
           [Path],
           [PastClass],
           [Race],
           [Afflictions],
           [Gender],
           [HairColor],
           [HairStyle],
           [OldColor],
           [OldStyle],
           [NameColor],
           [ProfileMessage],
           [Nation],
           [Clan],
           [ClanRank],
           [ClanTitle],
           [AnimalForm],
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
           [Developer],
           [Ranger],
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
           [HeadAccessory1Img],
           [HeadAccessory2Img],
           [OverCoatImg],
           [BootColor],
           [OverCoatColor],
           [Pants],
           [Aegis],
           [Bleeding],
           [Spikes],
           [Rending],
           [Reaping],
           [Vampirism],
           [Haste],
           [Gust],
           [Quake],
           [Rain],
           [Flame],
           [Dusk],
           [Dawn]
    FROM   [ZolianPlayers].[dbo].[Players]
    WHERE  Username = @Name;
END
GO

-- SelectQuests
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectQuests]
@Serial INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TutorialCompleted,
           BetaReset,
           StoneSmithing,
           MilethReputation,
           ArtursGift,
           CamilleGreetingComplete,
           ConnPotions,
           CryptTerror,
           CryptTerrorSlayed,
           Dar,
           DarItem,
           DrunkenHabit,
           EternalLove,
           FionaDance,
           Keela,
           KeelaCount,
           KeelaKill,
           KeelaQuesting,
           KillerBee,
           Neal,
           NealCount,
           NealKill,
           AbelShopAccess,
           PeteKill,
           PeteComplete,
           SwampAccess,
           SwampCount
    FROM   [ZolianPlayers].[dbo].[PlayersQuests]
    WHERE  Serial = @Serial;
END
GO

-- SelectSkills
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SelectSkills] @Serial INT
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
CREATE PROCEDURE [dbo].[SelectSpells] @Serial INT
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
@SkillId INT, @Serial INT, @Level INT, @Slot INT,
@SkillName VARCHAR (30), @Uses INT, @CurrentCooldown INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersSkillBook]
	([SkillId], [Serial], [Level], [Slot], [SkillName], [Uses], [CurrentCooldown])
    VALUES	(@SkillId, @Serial, @Level, @Slot, @SkillName, @Uses, @CurrentCooldown);
END
GO

-- SpellToPlayer
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[SpellToPlayer]
@SpellId INT, @Serial INT, @Level INT, @Slot INT,
@SpellName VARCHAR (30), @Casts INT, @CurrentCooldown INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersSpellBook]
	([SpellId], [Serial], [Level], [Slot], [SpellName], [Casts], [CurrentCooldown])
    VALUES	(@SpellId, @Serial, @Level, @Slot, @SpellName, @Casts, @CurrentCooldown);
END
GO

-- WithdrawItemList
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[WithdrawItemList] @Serial INT
AS
BEGIN
	SET NOCOUNT ON;
	SELECT * FROM ZolianPlayers.dbo.PlayersBanked WHERE Serial = @Serial
END
GO