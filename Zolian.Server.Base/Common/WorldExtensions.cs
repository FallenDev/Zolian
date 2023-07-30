using Chaos.Common.Definitions;
using Chaos.Common.Identity;
using Chaos.Networking.Entities.Server;
using Darkages.Network.Client.Abstractions;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

namespace Darkages.Common;

public static class WorldExtensions
{
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

    public static void SendOptionsDialog(this IWorldClient worldClient, Mundane npc, string message, ushort pursuitId, params Dialog.OptionsDataItem[] options)
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

    public static void SendOptionsDialog(this IWorldClient worldClient, Mundane npc, string message, string arg, ushort pursuitId, params Dialog.OptionsDataItem[] options)
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
                Slot = 0,
                Sprite = i.Image,
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

    public static void SendItemShopDialog(this IWorldClient worldClient, Mundane npc, string message, ushort arg, IEnumerable<ItemTemplate> item)
    {
        var args = new MenuArgs
        {
            Args = arg.ToString(),
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
                Slot = 0,
                Sprite = i.Image,
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

    public static void SendItemSellDialog(this IWorldClient worldClient, Mundane npc, string message, ushort arg, IEnumerable<byte> slot)
    {
        var args = new MenuArgs
        {
            Args = arg.ToString(),
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

    public static void SendSkillLearnDialog(this IWorldClient worldClient, Mundane npc, string message, IEnumerable<SkillTemplate> skillTemplates)
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
            PursuitId = 0,
            Skills = skillTemplates.Select(s => new SkillInfo
            {
                Name = s.Name,
                PanelName = null,
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
                PanelName = null,
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

    public static void SendSpellLearnDialog(this IWorldClient worldClient, Mundane npc, string message, IEnumerable<SpellTemplate> spellTemplates)
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
            PursuitId = 0,
            Skills = null,
            Slots = null,
            SourceId = npc.Serial,
            Spells = spellTemplates.Select(s => new SpellInfo
            {
                CastLines = 0,
                Name = null,
                PanelName = null,
                Prompt = null,
                Slot = 0,
                SpellType = SpellType.None,
                Sprite = 0
            }).ToList(),
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
                CastLines = 0,
                Name = null,
                PanelName = null,
                Prompt = null,
                Slot = 0,
                SpellType = SpellType.None,
                Sprite = 0
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