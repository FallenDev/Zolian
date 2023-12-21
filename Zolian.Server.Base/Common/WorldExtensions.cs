using Chaos.Common.Definitions;
using Chaos.Networking.Entities.Server;

using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Common;

public static class WorldExtensions
{
    #region Options Dialog

    public static void SendOptionsDialog(this IWorldClient worldClient, Mundane npc, string message, params Dialog.OptionsDataItem[] options)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.Menu,
            Name = npc.Template.Name,
            Options = options.Select(op => (op.Text, (ushort)op.Step)).ToList(),
            PursuitId = 0,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    public static void SendOptionsDialog(this IWorldClient worldClient, Mundane npc, string message, string arg, params Dialog.OptionsDataItem[] options)
    {
        var args = new MenuArgs
        {
            Args = arg,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.MenuWithArgs,
            Name = npc.Template.Name,
            Options = options.Select(op => (op.Text, (ushort)op.Step)).ToList(),
            PursuitId = 0,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    #endregion

    #region Buy/Sell Dialog

    public static void SendItemShopDialog(this IWorldClient worldClient, Mundane npc, string message, IEnumerable<ItemTemplate> item)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = item.Select(i => new ItemInfo
            {
                Color = (DisplayColor)i.Color,
                Cost = (int?)i.Value,
                Count = i.MaxStack,
                CurrentDurability = 0,
                MaxDurability = 0,
                Name = i.Name,
                Group = i.Group,
                Slot = 0,
                Sprite = i.DisplayImage,
                Stackable = i.CanStack
            }).ToList(),
            MenuType = MenuType.ShowItems,
            Name = npc.Name,
            Options = null,
            PursuitId = 0,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    public static void SendItemSellDialog(this IWorldClient worldClient, Mundane npc, string message, IEnumerable<byte> slot)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.ShowPlayerItems,
            Name = npc.Name,
            Options = null,
            PursuitId = 0,
            Skills = null,
            Slots = slot.ToList(),
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    #endregion

    #region Skills/Spells Dialog

    public static void SendSkillLearnDialog(this IWorldClient worldClient, Mundane npc, string message, ushort pursuit, IEnumerable<SkillTemplate> skillTemplates)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.ShowSkills,
            Name = npc.Name,
            Options = null,
            PursuitId = pursuit,
            Skills = skillTemplates.Select(s => new SkillInfo
            {
                Name = s.Name,
                PanelName = s.Name,
                Slot = 0,
                Sprite = s.Icon
            }).ToList(),
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    public static void SendSpellLearnDialog(this IWorldClient worldClient, Mundane npc, string message, ushort pursuit, IEnumerable<SpellTemplate> spellTemplates)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.ShowSpells,
            Name = npc.Name,
            Options = null,
            PursuitId = pursuit,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = spellTemplates.Select(s => new SpellInfo
            {
                CastLines = (byte)s.BaseLines,
                Name = s.Name,
                PanelName = s.Name,
                Prompt = null,
                Slot = 0,
                SpellType = (SpellType)s.TargetType,
                Sprite = s.Icon
            }).ToList(),
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    public static void SendForgetSkills(this IWorldClient worldClient, Mundane npc, string message, ushort pursuitId)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.ShowPlayerSkills,
            Name = npc.Name,
            Options = null,
            PursuitId = pursuitId,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    public static void SendForgetSpells(this IWorldClient worldClient, Mundane npc, string message, ushort pursuitId)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.ShowPlayerSpells,
            Name = npc.Name,
            Options = null,
            PursuitId = pursuitId,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    #endregion

    #region Banking

    public static void SendWithdrawBankDialog(this IWorldClient worldClient, Mundane npc, string message, IEnumerable<Item> items)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = items.Select(i => new ItemInfo
            {
                Color = (DisplayColor)i.Template.Color,
                Cost = i.Stacks == 0 ? 1 : i.Stacks, // Shop uses Cost, this is Stacks in Bank
                Count = 0,
                CurrentDurability = (int)i.Durability,
                MaxDurability = (int)i.MaxDurability,
                Name = i.NoColorDisplayName,
                Group = i.Template.Group,
                Slot = 0,
                Sprite = i.DisplayImage,
                Stackable = i.Template.CanStack
            }).ToList(),
            MenuType = MenuType.ShowItems,
            Name = npc.Name,
            Options = null,
            PursuitId = 0,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    #endregion

    #region PlayerInput

    public static void SendTextInput(this IWorldClient worldClient, Mundane npc, string message)
    {
        var args = new MenuArgs
        {
            Args = null,
            Color = DisplayColor.Default,
            EntityType = EntityType.Creature,
            Items = null,
            MenuType = MenuType.TextEntry,
            Name = npc.Name,
            Options = null,
            PursuitId = 0,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = null,
            Sprite = npc.Template.Image,
            Text = message
        };

        worldClient.Send(args);
    }

    /// <summary>
    /// Reactor Input NPC Dialog
    /// </summary>
    /// <param name="worldClient">Player's client</param>
    /// <param name="npc">NPC</param>
    /// <param name="message">Message on lower bar</param>
    /// <param name="textBoxMessage">Message within popup</param>
    /// <param name="textBoxLength">Length of input box</param>
    public static void SendTextInput(this IWorldClient worldClient, Mundane npc, string message, string textBoxMessage, ushort textBoxLength = 2)
    {
        var args = new DialogArgs
        {
            Color = DisplayColor.Default,
            DialogId = 0,
            EntityType = EntityType.Creature,
            HasNextButton = false,
            HasPreviousButton = false,
            DialogType = DialogType.TextEntry,
            Name = npc.Name,
            Options = null,
            PursuitId = 0,
            SourceId = npc.Serial,
            Sprite = npc.Template.Image,
            Text = message,
            TextBoxLength = textBoxLength,
            TextBoxPrompt = textBoxMessage
        };

        worldClient.Send(args);
    }

    /// <summary>
    /// Reactor Input NPC Dialog with Pursuit
    /// </summary>
    /// <summary>
    /// Reactor Input NPC Dialog
    /// </summary>
    public static void SendTextInput(this IWorldClient worldClient, Mundane npc, string message, ushort dialogId, string textBoxMessage, ushort textBoxLength = 2)
    {
        var args = new DialogArgs
        {
            Color = DisplayColor.Default,
            DialogId = dialogId,
            EntityType = EntityType.Creature,
            HasNextButton = false,
            HasPreviousButton = false,
            DialogType = DialogType.TextEntry,
            Name = npc.Name,
            Options = null,
            PursuitId = 0,
            SourceId = npc.Serial,
            Sprite = npc.Template.Image,
            Text = message,
            TextBoxLength = textBoxLength,
            TextBoxPrompt = textBoxMessage
        };

        worldClient.Send(args);
    }

    #endregion

    public static void CloseDialog(this IWorldClient worldClient)
    {
        var dialog = new DialogArgs
        {
            Color = DisplayColor.Default,
            DialogId = 0,
            DialogType = DialogType.CloseDialog,
            EntityType = 0,
            HasNextButton = false,
            HasPreviousButton = false,
            Name = null,
            Options = null,
            PursuitId = null,
            SourceId = null,
            Sprite = 0,
            Text = null,
            TextBoxLength = null
        };

        worldClient.Send(dialog);
    }
}