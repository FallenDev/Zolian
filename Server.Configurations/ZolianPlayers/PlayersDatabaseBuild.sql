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
DROP PROCEDURE [dbo].[SelectPlayer]
DROP PROCEDURE [dbo].[SelectLegends]
DROP PROCEDURE [dbo].[SelectIgnoredPlayers]
DROP PROCEDURE [dbo].[SelectDiscoveredMaps]
DROP PROCEDURE [dbo].[SelectInventory]
DROP PROCEDURE [dbo].[SelectEquipped]
DROP PROCEDURE [dbo].[SelectBanked]
DROP PROCEDURE [dbo].[SelectDeBuffsCheck]
DROP PROCEDURE [dbo].[SelectDeBuffs]
DROP PROCEDURE [dbo].[SelectBuffsCheck]
DROP PROCEDURE [dbo].[SelectBuffs]
DROP PROCEDURE [dbo].[PlayerSecurity]
DROP PROCEDURE [dbo].[PlayerSaveSpells]
DROP PROCEDURE [dbo].[PlayerSaveSkills]
DROP PROCEDURE [dbo].[PlayerSave]
DROP PROCEDURE [dbo].[PlayerQuickSave]
DROP PROCEDURE [dbo].[PlayerQuestSave]
DROP PROCEDURE [dbo].[PlayerCreation]
DROP PROCEDURE [dbo].[PasswordSave]
DROP PROCEDURE [dbo].[InsertQuests]
DROP PROCEDURE [dbo].[InsertDeBuff]
DROP PROCEDURE [dbo].[InsertBuff]
DROP PROCEDURE [dbo].[IgnoredSave]
DROP PROCEDURE [dbo].[FoundMap]
DROP PROCEDURE [dbo].[DeBuffSave]
DROP PROCEDURE [dbo].[CheckIfPlayerHashExists]
DROP PROCEDURE [dbo].[CheckIfPlayerExists]
DROP PROCEDURE [dbo].[LoadItemsToCache]
DROP PROCEDURE [dbo].[BuffSave]
DROP PROCEDURE [dbo].[AddLegendMark]
DROP PROCEDURE [dbo].[ItemUpsert]
GO

DROP TYPE dbo.ItemType
DROP TYPE dbo.SkillType
DROP TYPE dbo.SpellType
GO

DROP TABLE PlayersItems;
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
    [Serial] BIGINT NOT NULL PRIMARY KEY,
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
	[AbpLevel] SMALLINT NOT NULL DEFAULT 0,
	[AbpNext] INT NOT NULL DEFAULT 0,
	[AbpTotal] INT NOT NULL DEFAULT 0,
	[ExpLevel] SMALLINT NOT NULL DEFAULT 1,
	[ExpNext] INT NOT NULL DEFAULT 0,
	[ExpTotal] INT NOT NULL DEFAULT 0,
	[Stage] VARCHAR(10) NOT NULL,
	[Path] VARCHAR(10) NOT NULL DEFAULT 'Peasant',
	[PastClass] VARCHAR(10) NOT NULL DEFAULT 'Peasant',
	[Race] VARCHAR(10) NOT NULL DEFAULT 'Human',
	[Afflictions] VARCHAR(10) NOT NULL DEFAULT 'Normal',
	[Gender] VARCHAR(6) NOT NULL DEFAULT 'Both',
	[HairColor] TINYINT NOT NULL DEFAULT 0,
	[HairStyle] TINYINT NOT NULL DEFAULT 0,
	[OldColor] TINYINT NOT NULL DEFAULT 0,
	[OldStyle] TINYINT NOT NULL DEFAULT 0,
	[NameColor] TINYINT NOT NULL DEFAULT 1,
	[ProfileMessage] VARCHAR(100) NULL,
	[Nation] VARCHAR(30) NOT NULL DEFAULT 'Mileth',
	[Clan] VARCHAR(20) NULL,
	[ClanRank] VARCHAR(20) NULL,
	[ClanTitle] VARCHAR(20) NULL,
	[AnimalForm] VARCHAR(10) NOT NULL DEFAULT 'None',
	[MonsterForm] SMALLINT NOT NULL DEFAULT 0,
	[ActiveStatus] VARCHAR(15) NOT NULL DEFAULT 'Awake',
	[Flags] VARCHAR(6) NOT NULL DEFAULT 'Normal',
	[CurrentWeight] INT NOT NULL DEFAULT 0,
	[World] INT NOT NULL DEFAULT 0,
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
	[Developer] BIT NOT NULL DEFAULT 0,
	[Ranger] BIT NOT NULL DEFAULT 0,
	[Knight] BIT NOT NULL DEFAULT 0,
	[GoldPoints] BIGINT NOT NULL DEFAULT 0,
	[StatPoints] INT NOT NULL DEFAULT 0,
	[GamePoints] BIGINT NOT NULL DEFAULT 0,
	[BankedGold] BIGINT NOT NULL DEFAULT 0,
	[Display] VARCHAR(12) NOT NULL DEFAULT 'None',
	[ArmorImg] INT NOT NULL DEFAULT 0,
	[HelmetImg] INT NOT NULL DEFAULT 0,
	[ShieldImg] INT NOT NULL DEFAULT 0,
	[WeaponImg] INT NOT NULL DEFAULT 0,
	[BootsImg] INT NOT NULL DEFAULT 0,
    [HeadAccessoryImg] INT NOT NULL DEFAULT 0,
	[Accessory1Img] INT NOT NULL DEFAULT 0,
	[Accessory2Img] INT NOT NULL DEFAULT 0,
    [Accessory3Img] INT NOT NULL DEFAULT 0,
    [Accessory1Color] INT NOT NULL DEFAULT 0,
    [Accessory2Color] INT NOT NULL DEFAULT 0,
    [Accessory3Color] INT NOT NULL DEFAULT 0,
    [BodyColor] TINYINT NOT NULL DEFAULT 0,
    [BodySprite] TINYINT NOT NULL DEFAULT 0,
    [FaceSprite] TINYINT NOT NULL DEFAULT 0,
    [OverCoatImg] INT NOT NULL DEFAULT 0,
	[BootColor] TINYINT NOT NULL DEFAULT 0,
	[OverCoatColor] TINYINT NOT NULL DEFAULT 0,
	[Pants] TINYINT NOT NULL DEFAULT 0,
	[Aegis] TINYINT NOT NULL DEFAULT 0,
	[Bleeding] TINYINT NOT NULL DEFAULT 0,
	[Spikes] INT NOT NULL DEFAULT 0,
	[Rending] TINYINT NOT NULL DEFAULT 0,
	[Reaping] TINYINT NOT NULL DEFAULT 0,
	[Vampirism] TINYINT NOT NULL DEFAULT 0,
	[Haste] TINYINT NOT NULL DEFAULT 0,
	[Hastened] TINYINT NOT NULL DEFAULT 0,
	[Gust] TINYINT NOT NULL DEFAULT 0,
	[Quake] TINYINT NOT NULL DEFAULT 0,
	[Rain] TINYINT NOT NULL DEFAULT 0,
	[Flame] TINYINT NOT NULL DEFAULT 0,
	[Dusk] TINYINT NOT NULL DEFAULT 0,
	[Dawn] TINYINT NOT NULL DEFAULT 0
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
	[Casts] INT NOT NULL DEFAULT 0,
	[CurrentCooldown] INT NULL
)

CREATE TABLE PlayersSkillBook
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[Level] INT NOT NULL DEFAULT 0,
	[Slot] INT NULL,
	[SkillName] VARCHAR(30) NULL,
	[Uses] INT NOT NULL DEFAULT 0,
	[CurrentCooldown] INT NULL
)

CREATE TABLE PlayersLegend
(
	[LegendId] INT NOT NULL PRIMARY KEY,
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[Category] VARCHAR(20) NOT NULL,
	[Time] DATETIME DEFAULT CURRENT_TIMESTAMP,
	[Color] VARCHAR(25) NOT NULL DEFAULT 'Blue',
	[Icon] INT NOT NULL DEFAULT 0,
	[Value] VARCHAR(50) NOT NULL
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
	[Durability] INT NOT NULL DEFAULT 0,
	[Identified] BIT NOT NULL DEFAULT 0,
	[ItemVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[WeapVariance] VARCHAR(15) NOT NULL DEFAULT 'None',
	[ItemQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[OriginalQuality] VARCHAR(10) NOT NULL DEFAULT 'Damaged',
	[Stacks] INT NOT NULL DEFAULT 0,
	[Enchantable] BIT NOT NULL DEFAULT 0,
    [Tarnished] BIT NOT NULL DEFAULT 0
)

CREATE TABLE PlayersQuests
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
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
	[SwampCount] INT NULL,
    [TagorDungeonAccess] BIT NULL
)

CREATE TABLE PlayersIgnoreList
(
	[Serial] BIGINT FOREIGN KEY REFERENCES Players(Serial),
	[PlayerIgnored] VARCHAR(12) NOT NULL,
)

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
    Durability INT,
    Identified BIT,
    ItemVariance VARCHAR (15),
    WeapVariance VARCHAR (15),
    ItemQuality VARCHAR (10),
    OriginalQuality VARCHAR (10),
    Stacks INT,
    Enchantable BIT,
    Tarnished BIT
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

-- AddLegendMark
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[AddLegendMark]
@LegendId INT, @Serial BIGINT, @Category VARCHAR(20), @Time DATETIME,
@Color VARCHAR (25), @Icon INT, @Value VARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT  INTO [ZolianPlayers].[dbo].[PlayersLegend]
	([LegendId], [Serial], [Category], [Time], [Color], [Icon], [Value])
    VALUES	(@LegendId, @Serial, @Category, @Time, @Color, @Icon, @Value);
END
GO

-- BuffSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[BuffSave]
@Serial BIGINT, @Name VARCHAR(30), @TimeLeft INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE [ZolianPlayers].[dbo].[PlayersBuffs]
    SET
	[TimeLeft] = @TimeLeft
    WHERE  Serial = @Serial AND [Name] = @Name;
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

-- DeBuffSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[DeBuffSave]
@Serial BIGINT, @Name VARCHAR(30), @TimeLeft INT
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
    @Serial BIGINT, @TutComplete BIT, @BetaReset BIT, @StoneSmith INT, @MilethRep INT, @ArtursGift INT,
    @CamilleGreeting BIT, @ConnPotions BIT, @CryptTerror BIT, @CryptTerrorSlayed BIT, @Dar INT, @DarItem VARCHAR (20),
    @EternalLove BIT, @Fiona BIT, @Keela INT, @KeelaCount INT, @KeelaKill VARCHAR (20), @KeelaQuesting BIT,
    @KillerBee BIT, @Neal INT, @NealCount INT, @NealKill VARCHAR (20), @AbelShopAccess BIT, @PeteKill INT,
    @PeteComplete BIT, @SwampAccess BIT, @SwampCount INT, @TagorDungeonAccess BIT, @Lau INT,
    @AbelReputation INT, @RucesionReputation INT, @SuomiReputation INT, @RionnagReputation INT,
    @OrenReputation INT, @PietReputation INT, @LouresReputation INT, @UndineReputation INT,
    @TagorReputation INT, @ThievesGuildReputation INT, @AssassinsGuildReputation INT, @AdventuresGuildReputation INT,
    @BlackSmithing INT, @ArmorSmithing INT, @JewelCrafting INT, @BeltDegree VARCHAR (6)
AS
BEGIN
    SET NOCOUNT ON;
    
    INSERT INTO [ZolianPlayers].[dbo].[PlayersQuests] (
        [Serial], [TutorialCompleted], [BetaReset], [StoneSmithing], [MilethReputation], [ArtursGift],
        [CamilleGreetingComplete], [ConnPotions], [CryptTerror], [CryptTerrorSlayed], [Dar], [DarItem],
        [EternalLove], [FionaDance], [Keela], [KeelaCount], [KeelaKill], [KeelaQuesting],
        [KillerBee], [Neal], [NealCount], [NealKill], [AbelShopAccess], [PeteKill],
        [PeteComplete], [SwampAccess], [SwampCount], [TagorDungeonAccess], [Lau],
        [AbelReputation], [RucesionReputation], [SuomiReputation], [RionnagReputation],
        [OrenReputation], [PietReputation], [LouresReputation], [UndineReputation],
        [TagorReputation], [ThievesGuildReputation], [AssassinsGuildReputation], [AdventuresGuildReputation],
        [BlackSmithing], [ArmorSmithing], [JewelCrafting], [BeltDegree]
    )
    VALUES (
        @Serial, @TutComplete, @BetaReset, @StoneSmith, @MilethRep, @ArtursGift,
        @CamilleGreeting, @ConnPotions, @CryptTerror, @CryptTerrorSlayed, @Dar, @DarItem,
        @EternalLove, @Fiona, @Keela, @KeelaCount, @KeelaKill, @KeelaQuesting,
        @KillerBee, @Neal, @NealCount, @NealKill, @AbelShopAccess, @PeteKill,
        @PeteComplete, @SwampAccess, @SwampCount, @TagorDungeonAccess, @Lau,
        @AbelReputation, @RucesionReputation, @SuomiReputation, @RionnagReputation,
        @OrenReputation, @PietReputation, @LouresReputation, @UndineReputation,
        @TagorReputation, @ThievesGuildReputation, @AssassinsGuildReputation, @AdventuresGuildReputation,
        @BlackSmithing, @ArmorSmithing, @JewelCrafting, @BeltDegree
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
    [X], [Y], [CurrentMapId], [OffenseElement], [DefenseElement], [SecondaryOffensiveElement], [SecondaryDefensiveElement], [Direction], [CurrentHp],
    [BaseHp], [CurrentMp], [BaseMp], [_ac], [_Regen], [_Dmg], [_Hit], [_Mr], [_Str], [_Int], [_Wis], [_Con], [_Dex], [_Luck], [AbpLevel], [AbpNext],
    [AbpTotal], [ExpLevel], [ExpNext], [ExpTotal], [Stage], [Path], [PastClass], [Race], [Afflictions], [Gender], [HairColor], [HairStyle], [OldColor], [OldStyle],
    [NameColor], [ProfileMessage], [Nation], [Clan], [ClanRank], [ClanTitle], [AnimalForm], [MonsterForm], [ActiveStatus], [Flags], [CurrentWeight],
    [World], [Lantern], [Invisible], [Resting], [FireImmunity], [WaterImmunity], [WindImmunity], [EarthImmunity], [LightImmunity], [DarkImmunity], [PoisonImmunity], [EnticeImmunity],
    [PartyStatus], [RaceSkill], [RaceSpell], [GameMaster], [ArenaHost], [Developer], [Ranger], [Knight], [GoldPoints], [StatPoints], [GamePoints],
	[BankedGold], [ArmorImg], [HelmetImg], [ShieldImg],	[WeaponImg], [BootsImg], [HeadAccessoryImg], [Accessory1Img], [Accessory2Img], [Accessory3Img], [Accessory1Color],
    [Accessory2Color], [Accessory3Color], [BodyColor], [BodySprite], [FaceSprite], [OverCoatImg], [BootColor], [OverCoatColor], [Pants], [Aegis], [Bleeding],
    [Spikes], [Rending], [Reaping], [Vampirism], [Haste], [Gust], [Quake], [Rain], [Flame], [Dusk], [Dawn])
    VALUES (@Serial, @Created, @UserName, @Password, '0', 'False', 'False', @LastLogged,
    '7', '23', '7000', 'None', 'None', 'None', 'None', '0', @CurrentHp,
    @BaseHp, @CurrentMp, @BaseMp, '0', '0', '0', '0', '0', '5', '5', '5', '5', '5', '0', '0', '0',
    '0', '1', '600', '0', 'Class', 'Peasant', 'Peasant', 'UnDecided', 'Normal', @Gender, @HairColor, @HairStyle, @HairColor, @HairStyle,
    '1', '', 'Mileth', '', '', '', 'None', '0', 'Awake', 'Normal', '0',
    '0', '0', 'False', 'Standing', 'False', 'False', 'False', 'False', 'False', 'False', 'False', 'False',
    'AcceptingRequests', '', '', 'False', 'False', 'False', 'False', 'False', '0', '0', '0', '0',
    '0', '0', '0', '0', '0', '0', '0', '0', '0', '0',
    '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0',
    '0', '0', '0', '0', '0', '0', '0', '0', '0', '0', '0');
END
GO

-- PlayerQuestSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerQuestSave]
    @Serial BIGINT, @TutComplete BIT, @BetaReset BIT, @StoneSmith INT, @MilethRep INT, @ArtursGift INT,
    @CamilleGreeting BIT, @ConnPotions BIT, @CryptTerror BIT, @CryptTerrorSlayed BIT, @Dar INT, @DarItem VARCHAR (20),
    @EternalLove BIT, @Fiona BIT, @Keela INT, @KeelaCount INT, @KeelaKill VARCHAR (20), @KeelaQuesting BIT,
    @KillerBee BIT, @Neal INT, @NealCount INT, @NealKill VARCHAR (20), @AbelShopAccess BIT, @PeteKill INT,
    @PeteComplete BIT, @SwampAccess BIT, @SwampCount INT, @TagorDungeonAccess BIT, @Lau INT,
    @AbelReputation INT, @RucesionReputation INT, @SuomiReputation INT, @RionnagReputation INT,
    @OrenReputation INT, @PietReputation INT, @LouresReputation INT, @UndineReputation INT,
    @TagorReputation INT, @ThievesGuildReputation INT, @AssassinsGuildReputation INT, @AdventuresGuildReputation INT,
    @BlackSmithing INT, @ArmorSmithing INT, @JewelCrafting INT, @BeltDegree VARCHAR (6)
AS
BEGIN
    SET NOCOUNT ON;
    
    UPDATE [ZolianPlayers].[dbo].[PlayersQuests]
    SET    [TutorialCompleted] = @TutComplete,
           [BetaReset] = @BetaReset,
           [StoneSmithing] = @StoneSmith,
           [MilethReputation] = @MilethRep,
           [ArtursGift] = @ArtursGift,
           [CamilleGreetingComplete] = @CamilleGreeting,
           [ConnPotions] = @ConnPotions,
           [CryptTerror] = @CryptTerror,
           [CryptTerrorSlayed] = @CryptTerrorSlayed,
           [Dar] = @Dar,
           [DarItem] = @DarItem,
           [EternalLove] = @EternalLove,
           [FionaDance] = @Fiona,
           [Keela] = @Keela,
           [KeelaCount] = @KeelaCount,
           [KeelaKill] = @KeelaKill,
           [KeelaQuesting] = @KeelaQuesting,
           [KillerBee] = @KillerBee,
           [Neal] = @Neal,
           [NealCount] = @NealCount,
           [NealKill] = @NealKill,
           [AbelShopAccess] = @AbelShopAccess,
           [PeteKill] = @PeteKill,
           [PeteComplete] = @PeteComplete,
           [SwampAccess] = @SwampAccess,
           [SwampCount] = @SwampCount,
           [TagorDungeonAccess] = @TagorDungeonAccess,
           [Lau] = @Lau,
           [AbelReputation] = @AbelReputation,
           [RucesionReputation] = @RucesionReputation,
           [SuomiReputation] = @SuomiReputation,
           [RionnagReputation] = @RionnagReputation,
           [OrenReputation] = @OrenReputation,
           [PietReputation] = @PietReputation,
           [LouresReputation] = @LouresReputation,
           [UndineReputation] = @UndineReputation,
           [TagorReputation] = @TagorReputation,
           [ThievesGuildReputation] = @ThievesGuildReputation,
           [AssassinsGuildReputation] = @AssassinsGuildReputation,
           [AdventuresGuildReputation] = @AdventuresGuildReputation,
           [BlackSmithing] = @BlackSmithing,
           [ArmorSmithing] = @ArmorSmithing,
           [JewelCrafting] = @JewelCrafting,
           [BeltDegree] = @BeltDegree
    WHERE  Serial = @Serial;
END
GO

-- PlayerQuickSave
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[PlayerQuickSave]
@Name VARCHAR (12), @X INT, @Y INT, @CurrentMap INT, @OffensePrimary VARCHAR (15), @DefensePrimary VARCHAR (15), @OffenseSecondary VARCHAR (15),
@DefenseSecondary VARCHAR (15), @Direction INT, @CurrentHp INT, @BaseHp INT, @CurrentMp INT, @BaseMp INT, @AC INT, @Regen INT, @Dmg INT,
@Hit INT, @Mr INT, @Str INT, @Int INT, @Wis INT, @Con INT, @Dex INT, @Luck INT, @ABL SMALLINT, @ABN INT, @ABT INT, @EXPL SMALLINT,
@EXPN INT, @EXPT INT, @Afflix VARCHAR (10), @HairColor TINYINT, @HairStyle TINYINT, @OldColor TINYINT, @OldStyle TINYINT, @Animal VARCHAR (10),
@Monster SMALLINT, @Active VARCHAR (15), @Flags VARCHAR (6), @CurrentWeight INT, @World INT, @Lantern TINYINT, @Invisible BIT, @Resting VARCHAR (13),
@PartyStatus VARCHAR (21), @GoldPoints BIGINT, @StatPoints INT, @GamePoints BIGINT, @BankedGold BIGINT, @ArmorImg INT, @HelmetImg INT,
@ShieldImg INT, @WeaponImg INT, @BootsImg INT, @HeadAccessoryImg INT, @Accessory1Img INT, @Accessory2Img INT, @Accessory3Img INT,
@Accessory1Color INT, @Accessory2Color INT, @Accessory3Color INT, @BodyColor TINYINT, @BodySprite TINYINT, @FaceSprite TINYINT,
@OverCoatImg INT, @BootColor TINYINT, @OverCoatColor TINYINT, @Pants TINYINT, @Aegis TINYINT, @Bleeding TINYINT, @Spikes INT, @Rending TINYINT,
@Reaping TINYINT, @Vampirism TINYINT, @Haste TINYINT, @Gust TINYINT, @Quake TINYINT, @Rain TINYINT, @Flame TINYINT, @Dusk TINYINT, @Dawn TINYINT
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
           [HeadAccessoryImg]          = @HeadAccessoryImg,
           [Accessory1Img]             = @Accessory1Img,
           [Accessory2Img]             = @Accessory2Img,
           [Accessory3Img]             = @Accessory3Img,
           [Accessory1Color]           = @Accessory1Color,
           [Accessory2Color]           = @Accessory2Color,
           [Accessory3Color]           = @Accessory3Color,
           [BodyColor]                 = @BodyColor,
           [BodySprite]                = @BodySprite,
           [FaceSprite]                = @FaceSprite,
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
@Serial BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TutorialCompleted, BetaReset, StoneSmithing, MilethReputation, ArtursGift,
           CamilleGreetingComplete, ConnPotions, CryptTerror, CryptTerrorSlayed, Dar,
           DarItem, DrunkenHabit, EternalLove, FionaDance, Keela, KeelaCount, KeelaKill,
           KeelaQuesting, KillerBee, Neal, NealCount, NealKill, AbelShopAccess, PeteKill,
           PeteComplete, SwampAccess, SwampCount, TagorDungeonAccess, Lau,
           AbelReputation, RucesionReputation, SuomiReputation, RionnagReputation,
           OrenReputation, PietReputation, LouresReputation, UndineReputation,
           TagorReputation, ThievesGuildReputation, AssassinsGuildReputation, AdventuresGuildReputation,
           BlackSmithing, ArmorSmithing, JewelCrafting, BeltDegree
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
        Tarnished = source.Tarnished

    WHEN NOT MATCHED THEN
    INSERT (ItemId, Name, Serial, ItemPane, Slot, InventorySlot, Color, Cursed, Durability, Identified, ItemVariance, WeapVariance, ItemQuality, OriginalQuality, Stacks, Enchantable, Tarnished)
    VALUES (source.ItemId, source.Name, source.Serial, source.ItemPane, source.Slot, source.InventorySlot, source.Color, source.Cursed, source.Durability, source.Identified, source.ItemVariance, source.WeapVariance, source.ItemQuality, source.OriginalQuality, source.Stacks, source.Enchantable, source.Tarnished);
END