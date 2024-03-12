﻿using Chaos.Common.Identity;

using Dapper;

using Darkages.Enums;
using Darkages.Interfaces;
using Darkages.Sprites;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

using System.Data;
using Darkages.Models;
using Darkages.Templates;

namespace Darkages.Database;

public record AislingStorage : Sql, IAislingStorage
{
    public const string ConnectionString = "Data Source=.;Initial Catalog=ZolianPlayers;Integrated Security=True;Encrypt=False";
    public const string PersonalMailString = "Data Source=.;Initial Catalog=ZolianBoardsMail;Integrated Security=True;Encrypt=False";
    private const string EncryptedConnectionString = "Data Source=.;Initial Catalog=ZolianPlayers;Integrated Security=True;Column Encryption Setting=enabled;TrustServerCertificate=True";
    public SemaphoreSlim SaveLock { get; } = new(1, 1);
    private SemaphoreSlim BuffDebuffSaveLock { get; } = new(1, 1);
    private SemaphoreSlim PasswordSaveLock { get; } = new(1, 1);
    private SemaphoreSlim LoadLock { get; } = new(1, 1);
    private SemaphoreSlim CreateLock { get; } = new(1, 1);

    public async Task<Aisling> LoadAisling(string name, long serial)
    {
        await LoadLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        var aisling = new Aisling();

        try
        {
            var continueLoad = await CheckIfPlayerExists(name, serial);
            if (!continueLoad) return null;

            var sConn = ConnectToDatabase(ConnectionString);
            var values = new { Name = name };
            aisling = await sConn.QueryFirstAsync<Aisling>("[SelectPlayer]", values, commandType: CommandType.StoredProcedure);
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        finally
        {
            LoadLock.Release();
        }

        return aisling;
    }

    public async Task<bool> PasswordSave(Aisling obj)
    {
        if (obj == null) return false;
        if (obj.Loading) return false;
        var continueLoad = await CheckIfPlayerExists(obj.Username, obj.Serial);
        if (!continueLoad) return false;

        await PasswordSaveLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        try
        {
            var connection = ConnectToDatabase(EncryptedConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("PasswordSave", connection);
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = obj.Username;
            cmd.Parameters.Add("@Pass", SqlDbType.VarChar).Value = obj.Password;
            cmd.Parameters.Add("@Attempts", SqlDbType.Int).Value = obj.PasswordAttempts;
            cmd.Parameters.Add("@Hacked", SqlDbType.Bit).Value = obj.Hacked;
            cmd.Parameters.Add("@LastIP", SqlDbType.VarChar).Value = obj.LastIP;
            cmd.Parameters.Add("@LastAttemptIP", SqlDbType.VarChar).Value = obj.LastAttemptIP;
            ExecuteAndCloseConnection(cmd, connection);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        finally
        {
            PasswordSaveLock.Release();
        }

        return true;
    }

    public async Task AuxiliarySave(Aisling obj)
    {
        if (obj == null) return;
        if (obj.Loading) return;

        await BuffDebuffSaveLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        try
        {
            var connection = ConnectToDatabase(ConnectionString);
            SaveBuffs(obj, connection);
            SaveDebuffs(obj, connection);
            connection.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        finally
        {
            BuffDebuffSaveLock.Release();
        }
    }

    /// <summary>
    /// Saves a player's state on disconnect or error
    /// Creates a new DB connection on event
    /// </summary>
    public async Task<bool> Save(Aisling obj)
    {
        if (obj == null) return false;
        if (obj.Loading) return false;

        await SaveLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var dt = PlayerDataTable();
        var qDt = QuestDataTable();
        var cDt = ComboScrollDataTable();
        var iDt = ItemsDataTable();
        var skillDt = SkillDataTable();
        var spellDt = SpellDataTable();
        var connection = ConnectToDatabase(ConnectionString);

        try
        {
            var itemList = obj.Inventory.Items.Values.Where(i => i is not null).ToList();
            itemList.AddRange(from item in obj.EquipmentManager.Equipment.Values.Where(i => i is not null) where item.Item != null select item.Item);
            itemList.AddRange(obj.BankManager.Items.Values.Where(i => i is not null));
            var skillList = obj.SkillBook.Skills.Values.Where(i => i is { SkillName: not null }).ToList();
            var spellList = obj.SpellBook.Spells.Values.Where(i => i is { SpellName: not null }).ToList();

            dt.Rows.Add(obj.Serial, obj.Created, obj.Username, obj.LoggedIn, obj.LastLogged, obj.X, obj.Y, obj.CurrentMapId,
                obj.Direction, obj.CurrentHp, obj.BaseHp, obj.CurrentMp, obj.BaseMp, obj._ac,
                obj._Regen, obj._Dmg, obj._Hit, obj._Mr, obj._Str, obj._Int, obj._Wis, obj._Con, obj._Dex, obj._Luck, obj.AbpLevel,
                obj.AbpNext, obj.AbpTotal, obj.ExpLevel, obj.ExpNext, obj.ExpTotal, obj.Stage.ToString(), obj.JobClass.ToString(),
                obj.Path.ToString(), obj.PastClass.ToString(), obj.Race.ToString(), obj.Afflictions.ToString(), obj.Gender.ToString(),
                obj.HairColor, obj.HairStyle, obj.NameColor, obj.ProfileMessage, obj.Nation, obj.Clan, obj.ClanRank, obj.ClanTitle,
                obj.MonsterForm, obj.ActiveStatus.ToString(), obj.Flags.ToString(), obj.CurrentWeight, obj.World,
                obj.Lantern, obj.IsInvisible, obj.Resting.ToString(), obj.FireImmunity, obj.WaterImmunity, obj.WindImmunity, obj.EarthImmunity,
                obj.LightImmunity, obj.DarkImmunity, obj.PoisonImmunity, obj.EnticeImmunity, obj.PartyStatus.ToString(), obj.RaceSkill,
                obj.RaceSpell, obj.GameMaster, obj.ArenaHost, obj.Knight, obj.GoldPoints, obj.StatPoints, obj.GamePoints,
                obj.BankedGold, obj.ArmorImg, obj.HelmetImg, obj.ShieldImg, obj.WeaponImg, obj.BootsImg, obj.HeadAccessoryImg, obj.Accessory1Img,
                obj.Accessory2Img, obj.Accessory3Img, obj.Accessory1Color, obj.Accessory2Color, obj.Accessory3Color, obj.BodyColor, obj.BodySprite,
                obj.FaceSprite, obj.OverCoatImg, obj.BootColor, obj.OverCoatColor, obj.Pants);

            if (obj.QuestManager == null) return false;
            qDt.Rows.Add(obj.Serial, obj.QuestManager.MailBoxNumber, obj.QuestManager.TutorialCompleted, obj.QuestManager.BetaReset, obj.QuestManager.ArtursGift, obj.QuestManager.CamilleGreetingComplete,
            obj.QuestManager.ConnPotions, obj.QuestManager.CryptTerror, obj.QuestManager.CryptTerrorSlayed, obj.QuestManager.CryptTerrorContinued, obj.QuestManager.CryptTerrorContSlayed,
            obj.QuestManager.NightTerror, obj.QuestManager.NightTerrorSlayed, obj.QuestManager.DreamWalking, obj.QuestManager.DreamWalkingSlayed, obj.QuestManager.Dar, obj.QuestManager.DarItem, obj.QuestManager.ReleasedTodesbaum,
            obj.QuestManager.DrunkenHabit, obj.QuestManager.FionaDance, obj.QuestManager.Keela, obj.QuestManager.KeelaCount, obj.QuestManager.KeelaKill, obj.QuestManager.KeelaQuesting,
            obj.QuestManager.KillerBee, obj.QuestManager.Neal, obj.QuestManager.NealCount, obj.QuestManager.NealKill, obj.QuestManager.AbelShopAccess, obj.QuestManager.PeteKill, obj.QuestManager.PeteComplete,
            obj.QuestManager.SwampAccess, obj.QuestManager.SwampCount, obj.QuestManager.TagorDungeonAccess, obj.QuestManager.Lau, obj.QuestManager.BeltDegree, obj.QuestManager.MilethReputation,
            obj.QuestManager.AbelReputation, obj.QuestManager.RucesionReputation, obj.QuestManager.SuomiReputation, obj.QuestManager.RionnagReputation, obj.QuestManager.OrenReputation,
            obj.QuestManager.PietReputation, obj.QuestManager.LouresReputation, obj.QuestManager.UndineReputation, obj.QuestManager.TagorReputation, obj.QuestManager.BlackSmithing,
            obj.QuestManager.BlackSmithingTier, obj.QuestManager.ArmorSmithing, obj.QuestManager.ArmorSmithingTier, obj.QuestManager.JewelCrafting, obj.QuestManager.JewelCraftingTier,
            obj.QuestManager.StoneSmithing, obj.QuestManager.StoneSmithingTier, obj.QuestManager.ThievesGuildReputation, obj.QuestManager.AssassinsGuildReputation,
            obj.QuestManager.AdventuresGuildReputation, obj.QuestManager.BeltQuest, obj.QuestManager.SavedChristmas, obj.QuestManager.RescuedReindeer, obj.QuestManager.YetiKilled, obj.QuestManager.UnknownStart, obj.QuestManager.PirateShipAccess,
            obj.QuestManager.ScubaSchematics, obj.QuestManager.ScubaMaterialsQuest, obj.QuestManager.ScubaGearCrafted, obj.QuestManager.EternalLove, obj.QuestManager.EternalLoveStarted, obj.QuestManager.UnhappyEnding,
            obj.QuestManager.HonoringTheFallen, obj.QuestManager.ReadTheFallenNotes, obj.QuestManager.GivenTarnishedBreastplate, obj.QuestManager.EternalBond, obj.QuestManager.ArmorCraftingCodex,
            obj.QuestManager.ArmorApothecaryAccepted, obj.QuestManager.ArmorCodexDeciphered, obj.QuestManager.ArmorCraftingCodexLearned, obj.QuestManager.ArmorCraftingAdvancedCodexLearned);

            if (obj.ComboManager == null) return false;
            cDt.Rows.Add(obj.Serial, obj.ComboManager.Combo1, obj.ComboManager.Combo2, obj.ComboManager.Combo3, obj.ComboManager.Combo4, obj.ComboManager.Combo5,
            obj.ComboManager.Combo6, obj.ComboManager.Combo7, obj.ComboManager.Combo8, obj.ComboManager.Combo9, obj.ComboManager.Combo10, obj.ComboManager.Combo11,
            obj.ComboManager.Combo12, obj.ComboManager.Combo13, obj.ComboManager.Combo14, obj.ComboManager.Combo15);

            foreach (var item in itemList)
            {
                var pane = ItemEnumConverters.PaneToString(item.ItemPane);
                var color = ItemColors.ItemColorsToInt(item.Template.Color);
                var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
                var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
                var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
                var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);
                var gearEnhanced = ItemEnumConverters.GearEnhancementToString(item.GearEnhancement);
                var itemMaterial = ItemEnumConverters.ItemMaterialToString(item.ItemMaterial);
                var existingRow = iDt.AsEnumerable().FirstOrDefault(row => row.Field<long>("ItemId") == item.ItemId);

                // Check for duplicated ItemIds -- If an ID exists, this will overwrite it
                if (existingRow != null)
                {
                    existingRow["Name"] = item.Template.Name;
                    existingRow["Serial"] = (long)obj.Serial;
                    existingRow["ItemPane"] = pane;
                    existingRow["Slot"] = item.Slot;
                    existingRow["InventorySlot"] = item.InventorySlot;
                    existingRow["Color"] = color;
                    existingRow["Cursed"] = item.Cursed;
                    existingRow["Durability"] = item.Durability;
                    existingRow["Identified"] = item.Identified;
                    existingRow["ItemVariance"] = itemVariance;
                    existingRow["WeapVariance"] = weapVariance;
                    existingRow["ItemQuality"] = quality;
                    existingRow["OriginalQuality"] = orgQuality;
                    existingRow["Stacks"] = item.Stacks;
                    existingRow["Enchantable"] = item.Enchantable;
                    existingRow["Tarnished"] = item.Tarnished;
                    existingRow["GearEnhancement"] = gearEnhanced;
                    existingRow["ItemMaterial"] = itemMaterial;
                }
                else
                {
                    // If the item hasn't already been added to the data table, add it
                    iDt.Rows.Add(
                        item.ItemId,
                    item.Template.Name,
                        (long)obj.Serial,
                        pane,
                        item.Slot,
                        item.InventorySlot,
                        color,
                        item.Cursed,
                        item.Durability,
                        item.Identified,
                        itemVariance,
                        weapVariance,
                        quality,
                        orgQuality,
                        item.Stacks,
                        item.Enchantable,
                        item.Tarnished,
                        gearEnhanced,
                        itemMaterial
                    );
                }

                foreach (var skill in skillList)
                {
                    skillDt.Rows.Add(
                        (long)obj.Serial,
                        skill.Level,
                        skill.Slot,
                        skill.SkillName,
                        skill.Uses,
                        skill.CurrentCooldown
                    );
                }

                foreach (var spell in spellList)
                {
                    spellDt.Rows.Add(
                        (long)obj.Serial,
                        spell.Level,
                        spell.Slot,
                        spell.SpellName,
                        spell.Casts,
                        spell.CurrentCooldown
                    );
                }
            }

            using (var cmd = new SqlCommand("PlayerSave", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                var param = cmd.Parameters.AddWithValue("@Players", dt);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.PlayerType";
                cmd.ExecuteNonQuery();
            }

            using (var cmd2 = new SqlCommand("PlayerQuestSave", connection))
            {
                cmd2.CommandType = CommandType.StoredProcedure;
                var param2 = cmd2.Parameters.AddWithValue("@Quests", qDt);
                param2.SqlDbType = SqlDbType.Structured;
                param2.TypeName = "dbo.QuestType";
                cmd2.ExecuteNonQuery();
            }

            using (var cmd3 = new SqlCommand("PlayerComboSave", connection))
            {
                cmd3.CommandType = CommandType.StoredProcedure;
                var param3 = cmd3.Parameters.AddWithValue("@Combos", cDt);
                param3.SqlDbType = SqlDbType.Structured;
                param3.TypeName = "dbo.ComboType";
                cmd3.ExecuteNonQuery();
            }

            using (var cmd4 = new SqlCommand("ItemUpsert", connection))
            {
                cmd4.CommandType = CommandType.StoredProcedure;
                var param4 = cmd4.Parameters.AddWithValue("@Items", iDt);
                param4.SqlDbType = SqlDbType.Structured;
                param4.TypeName = "dbo.ItemType";
                cmd4.ExecuteNonQuery();
            }

            using (var cmd5 = new SqlCommand("PlayerSaveSkills", connection))
            {
                cmd5.CommandType = CommandType.StoredProcedure;
                var param5 = cmd5.Parameters.AddWithValue("@Skills", skillDt);
                param5.SqlDbType = SqlDbType.Structured;
                param5.TypeName = "dbo.SkillType";
                cmd5.ExecuteNonQuery();
            }

            using (var cmd6 = new SqlCommand("PlayerSaveSpells", connection))
            {
                cmd6.CommandType = CommandType.StoredProcedure;
                var param6 = cmd6.Parameters.AddWithValue("@Spells", spellDt);
                param6.SqlDbType = SqlDbType.Structured;
                param6.TypeName = "dbo.SpellType";
                cmd6.ExecuteNonQuery();
            }

            connection.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        finally
        {
            SaveLock.Release();
        }

        return true;
    }

    /// <summary>
    /// Saves all players states
    /// Utilizes an active connection that self-heals if closed
    /// </summary>
    public async Task<bool> ServerSave(List<Aisling> playerList)
    {
        if (playerList.Count == 0) return false;

        await SaveLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        var dt = PlayerDataTable();
        var qDt = QuestDataTable();
        var cDt = ComboScrollDataTable();
        var iDt = ItemsDataTable();
        var skillDt = SkillDataTable();
        var spellDt = SpellDataTable();
        var connection = ServerSetup.Instance.ServerSaveConnection;

        try
        {
            foreach (var player in playerList.Where(player => !player.Loading))
            {
                if (player?.Client == null) continue;
                if (!player.LoggedIn) continue;
                player.Client.LastSave = DateTime.UtcNow;
                var itemList = player.Inventory.Items.Values.Where(i => i is not null).ToList();
                itemList.AddRange(from item in player.EquipmentManager.Equipment.Values.Where(i => i is not null) where item.Item != null select item.Item);
                itemList.AddRange(player.BankManager.Items.Values.Where(i => i is not null));
                var skillList = player.SkillBook.Skills.Values.Where(i => i is { SkillName: not null }).ToList();
                var spellList = player.SpellBook.Spells.Values.Where(i => i is { SpellName: not null }).ToList();

                dt.Rows.Add(player.Serial, player.Created, player.Username, player.LoggedIn, player.LastLogged, player.X, player.Y, player.CurrentMapId,
                    player.Direction, player.CurrentHp, player.BaseHp, player.CurrentMp, player.BaseMp, player._ac,
                    player._Regen, player._Dmg, player._Hit, player._Mr, player._Str, player._Int, player._Wis, player._Con, player._Dex, player._Luck, player.AbpLevel,
                    player.AbpNext, player.AbpTotal, player.ExpLevel, player.ExpNext, player.ExpTotal, player.Stage.ToString(), player.JobClass.ToString(),
                    player.Path.ToString(), player.PastClass.ToString(), player.Race.ToString(), player.Afflictions.ToString(), player.Gender.ToString(),
                    player.HairColor, player.HairStyle, player.NameColor, player.ProfileMessage, player.Nation, player.Clan, player.ClanRank, player.ClanTitle,
                    player.MonsterForm, player.ActiveStatus.ToString(), player.Flags.ToString(), player.CurrentWeight, player.World,
                    player.Lantern, player.IsInvisible, player.Resting.ToString(), player.FireImmunity, player.WaterImmunity, player.WindImmunity, player.EarthImmunity,
                    player.LightImmunity, player.DarkImmunity, player.PoisonImmunity, player.EnticeImmunity, player.PartyStatus.ToString(), player.RaceSkill,
                    player.RaceSpell, player.GameMaster, player.ArenaHost, player.Knight, player.GoldPoints, player.StatPoints, player.GamePoints,
                    player.BankedGold, player.ArmorImg, player.HelmetImg, player.ShieldImg, player.WeaponImg, player.BootsImg, player.HeadAccessoryImg, player.Accessory1Img,
                    player.Accessory2Img, player.Accessory3Img, player.Accessory1Color, player.Accessory2Color, player.Accessory3Color, player.BodyColor, player.BodySprite,
                    player.FaceSprite, player.OverCoatImg, player.BootColor, player.OverCoatColor, player.Pants);

                qDt.Rows.Add(player.Serial, player.QuestManager.MailBoxNumber, player.QuestManager.TutorialCompleted, player.QuestManager.BetaReset, player.QuestManager.ArtursGift, player.QuestManager.CamilleGreetingComplete,
                    player.QuestManager.ConnPotions, player.QuestManager.CryptTerror, player.QuestManager.CryptTerrorSlayed, player.QuestManager.CryptTerrorContinued, player.QuestManager.CryptTerrorContSlayed,
                    player.QuestManager.NightTerror, player.QuestManager.NightTerrorSlayed, player.QuestManager.DreamWalking, player.QuestManager.DreamWalkingSlayed, player.QuestManager.Dar, player.QuestManager.DarItem, player.QuestManager.ReleasedTodesbaum,
                    player.QuestManager.DrunkenHabit, player.QuestManager.FionaDance, player.QuestManager.Keela, player.QuestManager.KeelaCount, player.QuestManager.KeelaKill, player.QuestManager.KeelaQuesting,
                    player.QuestManager.KillerBee, player.QuestManager.Neal, player.QuestManager.NealCount, player.QuestManager.NealKill, player.QuestManager.AbelShopAccess, player.QuestManager.PeteKill, player.QuestManager.PeteComplete,
                    player.QuestManager.SwampAccess, player.QuestManager.SwampCount, player.QuestManager.TagorDungeonAccess, player.QuestManager.Lau, player.QuestManager.BeltDegree, player.QuestManager.MilethReputation,
                    player.QuestManager.AbelReputation, player.QuestManager.RucesionReputation, player.QuestManager.SuomiReputation, player.QuestManager.RionnagReputation, player.QuestManager.OrenReputation,
                    player.QuestManager.PietReputation, player.QuestManager.LouresReputation, player.QuestManager.UndineReputation, player.QuestManager.TagorReputation, player.QuestManager.BlackSmithing,
                    player.QuestManager.BlackSmithingTier, player.QuestManager.ArmorSmithing, player.QuestManager.ArmorSmithingTier, player.QuestManager.JewelCrafting, player.QuestManager.JewelCraftingTier,
                    player.QuestManager.StoneSmithing, player.QuestManager.StoneSmithingTier, player.QuestManager.ThievesGuildReputation, player.QuestManager.AssassinsGuildReputation,
                    player.QuestManager.AdventuresGuildReputation, player.QuestManager.BeltQuest, player.QuestManager.SavedChristmas, player.QuestManager.RescuedReindeer, player.QuestManager.YetiKilled, player.QuestManager.UnknownStart, player.QuestManager.PirateShipAccess,
                    player.QuestManager.ScubaSchematics, player.QuestManager.ScubaMaterialsQuest, player.QuestManager.ScubaGearCrafted, player.QuestManager.EternalLove, player.QuestManager.EternalLoveStarted, player.QuestManager.UnhappyEnding,
                    player.QuestManager.HonoringTheFallen, player.QuestManager.ReadTheFallenNotes, player.QuestManager.GivenTarnishedBreastplate, player.QuestManager.EternalBond, player.QuestManager.ArmorCraftingCodex,
                    player.QuestManager.ArmorApothecaryAccepted, player.QuestManager.ArmorCodexDeciphered, player.QuestManager.ArmorCraftingCodexLearned, player.QuestManager.ArmorCraftingAdvancedCodexLearned);

                cDt.Rows.Add(player.Serial, player.ComboManager.Combo1, player.ComboManager.Combo2, player.ComboManager.Combo3, player.ComboManager.Combo4, player.ComboManager.Combo5,
                    player.ComboManager.Combo6, player.ComboManager.Combo7, player.ComboManager.Combo8, player.ComboManager.Combo9, player.ComboManager.Combo10, player.ComboManager.Combo11,
                    player.ComboManager.Combo12, player.ComboManager.Combo13, player.ComboManager.Combo14, player.ComboManager.Combo15);

                foreach (var item in itemList)
                {
                    var pane = ItemEnumConverters.PaneToString(item.ItemPane);
                    var color = ItemColors.ItemColorsToInt(item.Template.Color);
                    var quality = ItemEnumConverters.QualityToString(item.ItemQuality);
                    var orgQuality = ItemEnumConverters.QualityToString(item.OriginalQuality);
                    var itemVariance = ItemEnumConverters.ArmorVarianceToString(item.ItemVariance);
                    var weapVariance = ItemEnumConverters.WeaponVarianceToString(item.WeapVariance);
                    var gearEnhanced = ItemEnumConverters.GearEnhancementToString(item.GearEnhancement);
                    var itemMaterial = ItemEnumConverters.ItemMaterialToString(item.ItemMaterial);
                    var existingRow = iDt.AsEnumerable().FirstOrDefault(row => row.Field<long>("ItemId") == item.ItemId);

                    // Check for duplicated ItemIds -- If an ID exists, this will overwrite it
                    if (existingRow != null)
                    {
                        existingRow["Name"] = item.Template.Name;
                        existingRow["Serial"] = (long)player.Serial;
                        existingRow["ItemPane"] = pane;
                        existingRow["Slot"] = item.Slot;
                        existingRow["InventorySlot"] = item.InventorySlot;
                        existingRow["Color"] = color;
                        existingRow["Cursed"] = item.Cursed;
                        existingRow["Durability"] = item.Durability;
                        existingRow["Identified"] = item.Identified;
                        existingRow["ItemVariance"] = itemVariance;
                        existingRow["WeapVariance"] = weapVariance;
                        existingRow["ItemQuality"] = quality;
                        existingRow["OriginalQuality"] = orgQuality;
                        existingRow["Stacks"] = item.Stacks;
                        existingRow["Enchantable"] = item.Enchantable;
                        existingRow["Tarnished"] = item.Tarnished;
                        existingRow["GearEnhancement"] = gearEnhanced;
                        existingRow["ItemMaterial"] = itemMaterial;
                    }
                    else
                    {
                        // If the item hasn't already been added to the data table, add it
                        iDt.Rows.Add(
                            item.ItemId,
                            item.Template.Name,
                            (long)player.Serial,
                            pane,
                            item.Slot,
                            item.InventorySlot,
                            color,
                            item.Cursed,
                            item.Durability,
                            item.Identified,
                            itemVariance,
                            weapVariance,
                            quality,
                            orgQuality,
                            item.Stacks,
                            item.Enchantable,
                            item.Tarnished,
                            gearEnhanced,
                            itemMaterial
                        );
                    }
                }

                foreach (var skill in skillList)
                {
                    skillDt.Rows.Add(
                        (long)player.Serial,
                        skill.Level,
                        skill.Slot,
                        skill.SkillName,
                        skill.Uses,
                        skill.CurrentCooldown
                    );
                }

                foreach (var spell in spellList)
                {
                    spellDt.Rows.Add(
                        (long)player.Serial,
                        spell.Level,
                        spell.Slot,
                        spell.SpellName,
                        spell.Casts,
                        spell.CurrentCooldown
                    );
                }
            }

            using (var cmd = new SqlCommand("PlayerSave", connection))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                var param = cmd.Parameters.AddWithValue("@Players", dt);
                param.SqlDbType = SqlDbType.Structured;
                param.TypeName = "dbo.PlayerType";
                cmd.ExecuteNonQuery();
            }

            using (var cmd2 = new SqlCommand("PlayerQuestSave", connection))
            {
                cmd2.CommandType = CommandType.StoredProcedure;
                var param2 = cmd2.Parameters.AddWithValue("@Quests", qDt);
                param2.SqlDbType = SqlDbType.Structured;
                param2.TypeName = "dbo.QuestType";
                cmd2.ExecuteNonQuery();
            }

            using (var cmd3 = new SqlCommand("PlayerComboSave", connection))
            {
                cmd3.CommandType = CommandType.StoredProcedure;
                var param3 = cmd3.Parameters.AddWithValue("@Combos", cDt);
                param3.SqlDbType = SqlDbType.Structured;
                param3.TypeName = "dbo.ComboType";
                cmd3.ExecuteNonQuery();
            }

            using (var cmd4 = new SqlCommand("ItemUpsert", connection))
            {
                cmd4.CommandType = CommandType.StoredProcedure;
                var param4 = cmd4.Parameters.AddWithValue("@Items", iDt);
                param4.SqlDbType = SqlDbType.Structured;
                param4.TypeName = "dbo.ItemType";
                cmd4.ExecuteNonQuery();
            }

            using (var cmd5 = new SqlCommand("PlayerSaveSkills", connection))
            {
                cmd5.CommandType = CommandType.StoredProcedure;
                var param5 = cmd5.Parameters.AddWithValue("@Skills", skillDt);
                param5.SqlDbType = SqlDbType.Structured;
                param5.TypeName = "dbo.SkillType";
                cmd5.ExecuteNonQuery();
            }

            using (var cmd6 = new SqlCommand("PlayerSaveSpells", connection))
            {
                cmd6.CommandType = CommandType.StoredProcedure;
                var param6 = cmd6.Parameters.AddWithValue("@Spells", spellDt);
                param6.SqlDbType = SqlDbType.Structured;
                param6.TypeName = "dbo.SpellType";
                cmd6.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Issue with database connection");
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        finally
        {
            if (connection.State != ConnectionState.Open)
            {
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("Reconnecting Player Save-State");
                ServerSetup.Instance.ServerSaveConnection = new SqlConnection(ConnectionString);
                ServerSetup.Instance.ServerSaveConnection.Open();
            }

            SaveLock.Release();
        }

        return true;
    }

    public void SaveBuffs(Aisling aisling, SqlConnection connection)
    {
        if (aisling.Buffs.IsEmpty) return;

        try
        {
            foreach (var buff in aisling.Buffs.Values.Where(i => i is { Name: not null }))
            {
                var cmd = ConnectToDatabaseSqlCommandWithProcedure("BuffSave", connection);
                cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)aisling.Serial;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = buff.Name;
                cmd.Parameters.Add("@TimeLeft", SqlDbType.Int).Value = buff.TimeLeft;
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }

    public void SaveDebuffs(Aisling aisling, SqlConnection connection)
    {
        if (aisling.Debuffs.IsEmpty) return;

        try
        {
            foreach (var deBuff in aisling.Debuffs.Values.Where(i => i is { Name: not null }))
            {
                var cmd = ConnectToDatabaseSqlCommandWithProcedure("DeBuffSave", connection);
                cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)aisling.Serial;
                cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = deBuff.Name;
                cmd.Parameters.Add("@TimeLeft", SqlDbType.Int).Value = deBuff.TimeLeft;
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }

    public async Task<bool> CheckIfPlayerExists(string name)
    {
        try
        {
            var sConn = ConnectToDatabase(ConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("CheckIfPlayerExists", sConn);
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = name;
            var reader = await cmd.ExecuteReaderAsync();
            var userFound = false;

            while (reader.Read())
            {
                var userName = reader["Username"].ToString();
                if (!string.Equals(userName, name, StringComparison.CurrentCultureIgnoreCase)) continue;
                if (string.Equals(name, userName, StringComparison.CurrentCultureIgnoreCase))
                {
                    userFound = true;
                }
            }

            reader.Close();
            sConn.Close();
            return userFound;
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return false;
    }

    public async Task<bool> CheckIfPlayerExists(string name, long serial)
    {
        try
        {
            var sConn = ConnectToDatabase(ConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("CheckIfPlayerHashExists", sConn);
            cmd.Parameters.Add("@Name", SqlDbType.VarChar).Value = name;
            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = serial;
            var reader = await cmd.ExecuteReaderAsync();
            var userFound = false;

            while (reader.Read())
            {
                var userName = reader["Username"].ToString();
                if (!string.Equals(userName, name, StringComparison.CurrentCultureIgnoreCase)) continue;
                if (string.Equals(name, userName, StringComparison.CurrentCultureIgnoreCase))
                {
                    userFound = true;
                }
            }

            reader.Close();
            sConn.Close();
            return userFound;
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return false;
    }

    public async Task<Aisling> CheckPassword(string name)
    {
        var aisling = new Aisling();

        try
        {
            var continueLoad = await CheckIfPlayerExists(name);
            if (!continueLoad) return null;

            var sConn = ConnectToDatabase(EncryptedConnectionString);
            var values = new { Name = name };
            aisling = await sConn.QueryFirstAsync<Aisling>("[PlayerSecurity]", values, commandType: CommandType.StoredProcedure);
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return aisling;
    }

    public BoardTemplate ObtainMailboxId(long serial)
    {
        var board = new BoardTemplate();
        try
        {
            var sConn = ConnectToDatabase(ConnectionString);
            var values = new { Serial = serial };
            var quests = sConn.QueryFirst<Quests>("[ObtainMailBoxNumber]", values, commandType: CommandType.StoredProcedure);
            board.BoardId = (ushort)quests.MailBoxNumber;
            board.IsMail = true;
            board.Private = true;
            board.Serial = serial;
            sConn.Close();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }

        return board;
    }

    public List<PostTemplate> ObtainPosts(ushort boardId)
    {
        var posts = new List<PostTemplate>();

        try
        {
            var sConn = new SqlConnection(PersonalMailString);
            sConn.Open();
            const string sql = "SELECT * FROM ZolianBoardsMail.dbo.Posts";
            var cmd = new SqlCommand(sql, sConn);
            cmd.CommandTimeout = 5;
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var readBoardId = (int)reader["BoardId"];
                if (boardId != readBoardId) continue;
                var postId = (int)reader["PostId"];

                var post = new PostTemplate()
                {
                    PostId = (short)postId,
                    Highlighted = (bool)reader["Highlighted"],
                    DatePosted = (DateTime)reader["DatePosted"],
                    Owner = reader["Owner"].ToString(),
                    Sender = reader["Sender"].ToString(),
                    ReadPost = (bool)reader["ReadPost"],
                    SubjectLine = reader["SubjectLine"].ToString(),
                    Message = reader["Message"].ToString()
                };

                posts.Add(post);
            }

            reader.Close();
            sConn.Close();
        }
        catch (SqlException e)
        {
            ServerSetup.EventsLogger(e.ToString());
            Crashes.TrackError(e);
        }

        return posts;
    }

    public void SendPost(PostTemplate postInfo, ushort boardId)
    {
        try
        {
            var connection = ConnectToDatabase(PersonalMailString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("InsertPost", connection);
            cmd.Parameters.Add("@BoardId", SqlDbType.Int).Value = (int)boardId;
            cmd.Parameters.Add("@PostId", SqlDbType.Int).Value = postInfo.PostId;
            cmd.Parameters.Add("@Highlighted", SqlDbType.Bit).Value = postInfo.Highlighted;
            cmd.Parameters.Add("@DatePosted", SqlDbType.DateTime).Value = postInfo.DatePosted;
            cmd.Parameters.Add("@Owner", SqlDbType.VarChar).Value = postInfo.Owner;
            cmd.Parameters.Add("@Sender", SqlDbType.VarChar).Value = postInfo.Sender;
            cmd.Parameters.Add("@ReadPost", SqlDbType.Bit).Value = postInfo.ReadPost;
            cmd.Parameters.Add("@SubjectLine", SqlDbType.VarChar).Value = postInfo.SubjectLine;
            cmd.Parameters.Add("@Message", SqlDbType.VarChar).Value = postInfo.Message;
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
    }

    public async Task Create(Aisling obj)
    {
        await CreateLock.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

        var serial = EphemeralRandomIdGenerator<uint>.Shared.NextId;

        try
        {
            // Player
            var connection = ConnectToDatabase(EncryptedConnectionString);
            var cmd = ConnectToDatabaseSqlCommandWithProcedure("PlayerCreation", connection);
            var mailBoxNumber = EphemeralRandomIdGenerator<ushort>.Shared.NextId;

            #region Parameters

            cmd.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)serial;
            cmd.Parameters.Add("@Created", SqlDbType.DateTime).Value = obj.Created;
            cmd.Parameters.Add("@UserName", SqlDbType.VarChar).Value = obj.Username;
            cmd.Parameters.Add("@Password", SqlDbType.VarChar).Value = obj.Password;
            cmd.Parameters.Add("@LastLogged", SqlDbType.DateTime).Value = obj.LastLogged;
            cmd.Parameters.Add("@CurrentHp", SqlDbType.Int).Value = obj.CurrentHp;
            cmd.Parameters.Add("@BaseHp", SqlDbType.Int).Value = obj.BaseHp;
            cmd.Parameters.Add("@CurrentMp", SqlDbType.Int).Value = obj.CurrentMp;
            cmd.Parameters.Add("@BaseMp", SqlDbType.Int).Value = obj.BaseMp;
            cmd.Parameters.Add("@Gender", SqlDbType.VarChar).Value = SpriteMaker.GenderValue(obj.Gender);
            cmd.Parameters.Add("@HairColor", SqlDbType.TinyInt).Value = obj.HairColor;
            cmd.Parameters.Add("@HairStyle", SqlDbType.TinyInt).Value = obj.HairStyle;

            #endregion

            ExecuteAndCloseConnection(cmd, connection);

            var sConn = ConnectToDatabase(ConnectionString);
            var adapter = new SqlDataAdapter();

            #region Adapter Inserts

            // Discovered
            var playerDiscoveredMaps =
                "INSERT INTO ZolianPlayers.dbo.PlayersDiscoveredMaps (Serial, MapId) VALUES " +
                $"('{(long)serial}','{obj.CurrentMapId}')";

            var cmd2 = new SqlCommand(playerDiscoveredMaps, sConn);
            cmd2.CommandTimeout = 5;

            adapter.InsertCommand = cmd2;
            adapter.InsertCommand.ExecuteNonQuery();

            // PlayersSkills
            var playerSkillBook =
                "INSERT INTO ZolianPlayers.dbo.PlayersSkillBook (Serial, Level, Slot, SkillName, Uses, CurrentCooldown) VALUES " +
                $"('{(long)serial}','{0}','{73}','Assail','{0}','{0}')";

            var cmd3 = new SqlCommand(playerSkillBook, sConn);
            cmd3.CommandTimeout = 5;

            adapter.InsertCommand = cmd3;
            adapter.InsertCommand.ExecuteNonQuery();

            #endregion

            var cmd5 = ConnectToDatabaseSqlCommandWithProcedure("InsertQuests", sConn);

            #region Parameters

            cmd5.Parameters.Add("@Serial", SqlDbType.BigInt).Value = (long)serial;
            cmd5.Parameters.Add("@MailBoxNumber", SqlDbType.Int).Value = mailBoxNumber;
            cmd5.Parameters.Add("@TutComplete", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@BetaReset", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@StoneSmith", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@StoneSmithingTier", SqlDbType.VarChar).Value = "Novice";
            cmd5.Parameters.Add("@MilethRep", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@ArtursGift", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@CamilleGreeting", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ConnPotions", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@CryptTerror", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@CryptTerrorSlayed", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@CryptTerrorContinued", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@CryptTerrorContSlayed", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@NightTerror", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@NightTerrorSlayed", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@DreamWalking", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@DreamWalkingSlayed", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Dar", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@DarItem", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@ReleasedTodesbaum", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@DrunkenHabit", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Fiona", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Keela", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@KeelaCount", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@KeelaKill", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@KeelaQuesting", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@KillerBee", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Neal", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@NealCount", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@NealKill", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@AbelShopAccess", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@PeteKill", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@PeteComplete", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@SwampAccess", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@SwampCount", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@TagorDungeonAccess", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@Lau", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@AbelReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@RucesionReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@SuomiReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@RionnagReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@OrenReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@PietReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@LouresReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@UndineReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@TagorReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@ThievesGuildReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@AssassinsGuildReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@AdventuresGuildReputation", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@BlackSmithing", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@BlackSmithingTier", SqlDbType.VarChar).Value = "Novice";
            cmd5.Parameters.Add("@ArmorSmithing", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@ArmorSmithingTier", SqlDbType.VarChar).Value = "Novice";
            cmd5.Parameters.Add("@JewelCrafting", SqlDbType.Int).Value = 0;
            cmd5.Parameters.Add("@JewelCraftingTier", SqlDbType.VarChar).Value = "Novice";
            cmd5.Parameters.Add("@BeltDegree", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@BeltQuest", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@SavedChristmas", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@RescuedReindeer", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@YetiKilled", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@UnknownStart", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@PirateShipAccess", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ScubaSchematics", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ScubaMaterialsQuest", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ScubaGearCrafted", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@EternalLove", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@EternalLoveStarted", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@UnhappyEnding", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@HonoringTheFallen", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ReadTheFallenNotes", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@GivenTarnishedBreastplate", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@EternalBond", SqlDbType.VarChar).Value = "";
            cmd5.Parameters.Add("@ArmorCraftingCodex", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ArmorApothecaryAccepted", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ArmorCodexDeciphered", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ArmorCraftingCodexLearned", SqlDbType.Bit).Value = false;
            cmd5.Parameters.Add("@ArmorCraftingAdvancedCodexLearned", SqlDbType.Bit).Value = false;


            #endregion

            ExecuteAndCloseConnection(cmd5, sConn);
        }
        catch (Exception e)
        {
            ServerSetup.EventsLogger(e.Message, LogLevel.Error);
            ServerSetup.EventsLogger(e.StackTrace, LogLevel.Error);
            Crashes.TrackError(e);
        }
        finally
        {
            CreateLock.Release();
        }
    }

    private static DataTable PlayerDataTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("Serial", typeof(long));
        dt.Columns.Add("Created", typeof(DateTime));
        dt.Columns.Add("Username", typeof(string));
        dt.Columns.Add("LoggedIn", typeof(bool));
        dt.Columns.Add("LastLogged", typeof(DateTime));
        dt.Columns.Add("X", typeof(byte));
        dt.Columns.Add("Y", typeof(byte));
        dt.Columns.Add("CurrentMapId", typeof(int));
        dt.Columns.Add("Direction", typeof(byte));
        dt.Columns.Add("CurrentHp", typeof(int));
        dt.Columns.Add("BaseHp", typeof(int));
        dt.Columns.Add("CurrentMp", typeof(int));
        dt.Columns.Add("BaseMp", typeof(int));
        dt.Columns.Add("_ac", typeof(short));
        dt.Columns.Add("_Regen", typeof(short));
        dt.Columns.Add("_Dmg", typeof(short));
        dt.Columns.Add("_Hit", typeof(short));
        dt.Columns.Add("_Mr", typeof(short));
        dt.Columns.Add("_Str", typeof(short));
        dt.Columns.Add("_Int", typeof(short));
        dt.Columns.Add("_Wis", typeof(short));
        dt.Columns.Add("_Con", typeof(short));
        dt.Columns.Add("_Dex", typeof(short));
        dt.Columns.Add("_Luck", typeof(short));
        dt.Columns.Add("AbpLevel", typeof(int));
        dt.Columns.Add("AbpNext", typeof(int));
        dt.Columns.Add("AbpTotal", typeof(long));
        dt.Columns.Add("ExpLevel", typeof(int));
        dt.Columns.Add("ExpNext", typeof(int));
        dt.Columns.Add("ExpTotal", typeof(long));
        dt.Columns.Add("Stage", typeof(string));
        dt.Columns.Add("JobClass", typeof(string));
        dt.Columns.Add("Path", typeof(string));
        dt.Columns.Add("PastClass", typeof(string));
        dt.Columns.Add("Race", typeof(string));
        dt.Columns.Add("Afflictions", typeof(string));
        dt.Columns.Add("Gender", typeof(string));
        dt.Columns.Add("HairColor", typeof(byte));
        dt.Columns.Add("HairStyle", typeof(byte));
        dt.Columns.Add("NameColor", typeof(byte));
        dt.Columns.Add("ProfileMessage", typeof(string));
        dt.Columns.Add("Nation", typeof(string));
        dt.Columns.Add("Clan", typeof(string));
        dt.Columns.Add("ClanRank", typeof(string));
        dt.Columns.Add("ClanTitle", typeof(string));
        dt.Columns.Add("MonsterForm", typeof(short));
        dt.Columns.Add("ActiveStatus", typeof(string));
        dt.Columns.Add("Flags", typeof(string));
        dt.Columns.Add("CurrentWeight", typeof(short));
        dt.Columns.Add("World", typeof(byte));
        dt.Columns.Add("Lantern", typeof(byte));
        dt.Columns.Add("Invisible", typeof(bool));
        dt.Columns.Add("Resting", typeof(string));
        dt.Columns.Add("FireImmunity", typeof(bool));
        dt.Columns.Add("WaterImmunity", typeof(bool));
        dt.Columns.Add("WindImmunity", typeof(bool));
        dt.Columns.Add("EarthImmunity", typeof(bool));
        dt.Columns.Add("LightImmunity", typeof(bool));
        dt.Columns.Add("DarkImmunity", typeof(bool));
        dt.Columns.Add("PoisonImmunity", typeof(bool));
        dt.Columns.Add("EnticeImmunity", typeof(bool));
        dt.Columns.Add("PartyStatus", typeof(string));
        dt.Columns.Add("RaceSkill", typeof(string));
        dt.Columns.Add("RaceSpell", typeof(string));
        dt.Columns.Add("GameMaster", typeof(bool));
        dt.Columns.Add("ArenaHost", typeof(bool));
        dt.Columns.Add("Knight", typeof(bool));
        dt.Columns.Add("GoldPoints", typeof(long));
        dt.Columns.Add("StatPoints", typeof(short));
        dt.Columns.Add("GamePoints", typeof(long));
        dt.Columns.Add("BankedGold", typeof(long));
        dt.Columns.Add("ArmorImg", typeof(short));
        dt.Columns.Add("HelmetImg", typeof(short));
        dt.Columns.Add("ShieldImg", typeof(short));
        dt.Columns.Add("WeaponImg", typeof(short));
        dt.Columns.Add("BootsImg", typeof(short));
        dt.Columns.Add("HeadAccessoryImg", typeof(short));
        dt.Columns.Add("Accessory1Img", typeof(short));
        dt.Columns.Add("Accessory2Img", typeof(short));
        dt.Columns.Add("Accessory3Img", typeof(short));
        dt.Columns.Add("Accessory1Color", typeof(byte));
        dt.Columns.Add("Accessory2Color", typeof(byte));
        dt.Columns.Add("Accessory3Color", typeof(byte));
        dt.Columns.Add("BodyColor", typeof(byte));
        dt.Columns.Add("BodySprite", typeof(byte));
        dt.Columns.Add("FaceSprite", typeof(byte));
        dt.Columns.Add("OverCoatImg", typeof(short));
        dt.Columns.Add("BootColor", typeof(byte));
        dt.Columns.Add("OverCoatColor", typeof(byte));
        dt.Columns.Add("Pants", typeof(byte));
        return dt;
    }

    private static DataTable ItemsDataTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("ItemId", typeof(long));
        dt.Columns.Add("Name", typeof(string));
        dt.Columns.Add("Serial", typeof(long)); // Owner's Serial
        dt.Columns.Add("ItemPane", typeof(string));
        dt.Columns.Add("Slot", typeof(int));
        dt.Columns.Add("InventorySlot", typeof(int));
        dt.Columns.Add("Color", typeof(int));
        dt.Columns.Add("Cursed", typeof(bool));
        dt.Columns.Add("Durability", typeof(long));
        dt.Columns.Add("Identified", typeof(bool));
        dt.Columns.Add("ItemVariance", typeof(string));
        dt.Columns.Add("WeapVariance", typeof(string));
        dt.Columns.Add("ItemQuality", typeof(string));
        dt.Columns.Add("OriginalQuality", typeof(string));
        dt.Columns.Add("Stacks", typeof(int));
        dt.Columns.Add("Enchantable", typeof(bool));
        dt.Columns.Add("Tarnished", typeof(bool));
        dt.Columns.Add("GearEnhancement", typeof(string));
        dt.Columns.Add("ItemMaterial", typeof(string));
        return dt;
    }

    private static DataTable SkillDataTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("Serial", typeof(long));
        dt.Columns.Add("Level", typeof(int));
        dt.Columns.Add("Slot", typeof(int));
        dt.Columns.Add("Skill", typeof(string));
        dt.Columns.Add("Uses", typeof(int));
        dt.Columns.Add("Cooldown", typeof(int));
        return dt;
    }

    private static DataTable SpellDataTable()
    {
        var dt = new DataTable();
        dt.Columns.Add("Serial", typeof(long));
        dt.Columns.Add("Level", typeof(int));
        dt.Columns.Add("Slot", typeof(int));
        dt.Columns.Add("Spell", typeof(string));
        dt.Columns.Add("Casts", typeof(int));
        dt.Columns.Add("Cooldown", typeof(int));
        return dt;
    }

    private static DataTable QuestDataTable()
    {
        var qDt = new DataTable();
        qDt.Columns.Add("Serial", typeof(long));
        qDt.Columns.Add("MailBoxNumber", typeof(int));
        qDt.Columns.Add("TutorialCompleted", typeof(bool));
        qDt.Columns.Add("BetaReset", typeof(bool));
        qDt.Columns.Add("ArtursGift", typeof(int));
        qDt.Columns.Add("CamilleGreetingComplete", typeof(bool));
        qDt.Columns.Add("ConnPotions", typeof(bool));
        qDt.Columns.Add("CryptTerror", typeof(bool));
        qDt.Columns.Add("CryptTerrorSlayed", typeof(bool));
        qDt.Columns.Add("CryptTerrorContinued", typeof(bool));
        qDt.Columns.Add("CryptTerrorContSlayed", typeof(bool));
        qDt.Columns.Add("NightTerror", typeof(bool));
        qDt.Columns.Add("NightTerrorSlayed", typeof(bool));
        qDt.Columns.Add("DreamWalking", typeof(bool));
        qDt.Columns.Add("DreamWalkingSlayed", typeof(bool));
        qDt.Columns.Add("Dar", typeof(int));
        qDt.Columns.Add("DarItem", typeof(string));
        qDt.Columns.Add("ReleasedTodesbaum", typeof(bool));
        qDt.Columns.Add("DrunkenHabit", typeof(bool));
        qDt.Columns.Add("FionaDance", typeof(bool));
        qDt.Columns.Add("Keela", typeof(int));
        qDt.Columns.Add("KeelaCount", typeof(int));
        qDt.Columns.Add("KeelaKill", typeof(string));
        qDt.Columns.Add("KeelaQuesting", typeof(bool));
        qDt.Columns.Add("KillerBee", typeof(bool));
        qDt.Columns.Add("Neal", typeof(int));
        qDt.Columns.Add("NealCount", typeof(int));
        qDt.Columns.Add("NealKill", typeof(string));
        qDt.Columns.Add("AbelShopAccess", typeof(bool));
        qDt.Columns.Add("PeteKill", typeof(int));
        qDt.Columns.Add("PeteComplete", typeof(bool));
        qDt.Columns.Add("SwampAccess", typeof(bool));
        qDt.Columns.Add("SwampCount", typeof(int));
        qDt.Columns.Add("TagorDungeonAccess", typeof(bool));
        qDt.Columns.Add("Lau", typeof(int));
        qDt.Columns.Add("BeltDegree", typeof(string));
        qDt.Columns.Add("MilethReputation", typeof(int));
        qDt.Columns.Add("AbelReputation", typeof(int));
        qDt.Columns.Add("RucesionReputation", typeof(int));
        qDt.Columns.Add("SuomiReputation", typeof(int));
        qDt.Columns.Add("RionnagReputation", typeof(int));
        qDt.Columns.Add("OrenReputation", typeof(int));
        qDt.Columns.Add("PietReputation", typeof(int));
        qDt.Columns.Add("LouresReputation", typeof(int));
        qDt.Columns.Add("UndineReputation", typeof(int));
        qDt.Columns.Add("TagorReputation", typeof(int));
        qDt.Columns.Add("BlackSmithing", typeof(int));
        qDt.Columns.Add("BlackSmithingTier", typeof(string));
        qDt.Columns.Add("ArmorSmithing", typeof(int));
        qDt.Columns.Add("ArmorSmithingTier", typeof(string));
        qDt.Columns.Add("JewelCrafting", typeof(int));
        qDt.Columns.Add("JewelCraftingTier", typeof(string));
        qDt.Columns.Add("StoneSmithing", typeof(int));
        qDt.Columns.Add("StoneSmithingTier", typeof(string));
        qDt.Columns.Add("ThievesGuildReputation", typeof(int));
        qDt.Columns.Add("AssassinsGuildReputation", typeof(int));
        qDt.Columns.Add("AdventuresGuildReputation", typeof(int));
        qDt.Columns.Add("BeltQuest", typeof(string));
        qDt.Columns.Add("SavedChristmas", typeof(bool));
        qDt.Columns.Add("RescuedReindeer", typeof(bool));
        qDt.Columns.Add("YetiKilled", typeof(bool));
        qDt.Columns.Add("UnknownStart", typeof(bool));
        qDt.Columns.Add("PirateShipAccess", typeof(bool));
        qDt.Columns.Add("ScubaSchematics", typeof(bool));
        qDt.Columns.Add("ScubaMaterialsQuest", typeof(bool));
        qDt.Columns.Add("ScubaGearCrafted", typeof(bool));
        qDt.Columns.Add("EternalLove", typeof(bool));
        qDt.Columns.Add("EternalLoveStarted", typeof(bool));
        qDt.Columns.Add("UnhappyEnding", typeof(bool));
        qDt.Columns.Add("HonoringTheFallen", typeof(bool));
        qDt.Columns.Add("ReadTheFallenNotes", typeof(bool));
        qDt.Columns.Add("GivenTarnishedBreastplate", typeof(bool));
        qDt.Columns.Add("EternalBond", typeof(string));
        qDt.Columns.Add("ArmorCraftingCodex", typeof(bool));
        qDt.Columns.Add("ArmorApothecaryAccepted", typeof(bool));
        qDt.Columns.Add("ArmorCodexDeciphered", typeof(bool));
        qDt.Columns.Add("ArmorCraftingCodexLearned", typeof(bool));
        qDt.Columns.Add("ArmorCraftingAdvancedCodexLearned", typeof(bool));

        return qDt;
    }

    private static DataTable ComboScrollDataTable()
    {
        var cDt = new DataTable();
        cDt.Columns.Add("Serial", typeof(long));
        cDt.Columns.Add("Combo1", typeof(string));
        cDt.Columns.Add("Combo2", typeof(string));
        cDt.Columns.Add("Combo3", typeof(string));
        cDt.Columns.Add("Combo4", typeof(string));
        cDt.Columns.Add("Combo5", typeof(string));
        cDt.Columns.Add("Combo6", typeof(string));
        cDt.Columns.Add("Combo7", typeof(string));
        cDt.Columns.Add("Combo8", typeof(string));
        cDt.Columns.Add("Combo9", typeof(string));
        cDt.Columns.Add("Combo10", typeof(string));
        cDt.Columns.Add("Combo11", typeof(string));
        cDt.Columns.Add("Combo12", typeof(string));
        cDt.Columns.Add("Combo13", typeof(string));
        cDt.Columns.Add("Combo14", typeof(string));
        cDt.Columns.Add("Combo15", typeof(string));
        return cDt;
    }
}