using System.Collections.Concurrent;
using System.Data;
using System.Numerics;
using Chaos.Common.Definitions;
using Dapper;

using Darkages.Common;
using Darkages.Database;
using Darkages.Enums;
using Darkages.GameScripts.Affects;
using Darkages.GameScripts.Formulas;
using Darkages.Infrastructure;
using Darkages.Models;
using Darkages.Network.Formats.Models.ServerFormats;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Scripting;
using Darkages.Sprites;
using Darkages.Templates;
using Darkages.Types;

using Microsoft.AppCenter.Crashes;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using EquipmentSlot = Darkages.Models.EquipmentSlot;
using MapFlags = Darkages.Enums.MapFlags;

namespace Darkages.Network.Client;

public class GameClients
{


    public void CloseDialog()
    {
        Send(new ServerFormat30());
    }
    
    public void SendMapMusic()
    {
        if (Aisling.Map == null) return;
        Send(new ServerFormat19(Aisling.Client, (byte)Aisling.Map.Music));
    }
    
    public void DisableShade()
    {
        const byte shade = 0x005;
        var format20 = new ServerFormat20 { Shade = shade };

        foreach (var client in Server.Clients.Values.Where(client => client != null))
        {
            client.Send(format20);
        }
    }

    public void Say(string message, byte type = 0x00)
    {
        var response = new ServerFormat0D
        {
            Serial = Aisling.Serial,
            Type = type,
            Text = message
        };

        Aisling.Show(Scope.NearbyAislings, response);
    }

    public void SendAnimation(ushort animation, Sprite to, Sprite from, byte speed = 100)
    {
        ServerFormat29 format;

        if (from is Aisling aisling)
            format = new ServerFormat29((uint)aisling.Serial, (uint)to.Serial, animation, 0, speed);
        else
            format = new ServerFormat29((uint)from.Serial, (uint)to.Serial, animation, 0, speed);

        Aisling.Show(Scope.NearbyAislings, format);
    }

    public void SendItemSellDialog(Mundane mundane, string text, ushort step, IEnumerable<byte> items)
    {
        if (Aisling.Map.ID != mundane.Map.ID) return;
        Send(new ServerFormat2F(mundane, text, new ItemSellData(step, items)));
    }

    public void SendItemShopDialog(Mundane mundane, string text, ushort step, IEnumerable<ItemTemplate> items)
    {
        if (Aisling.Map.ID != mundane.Map.ID) return;
        Send(new ServerFormat2F(mundane, text, new ItemShopData(step, items)));
    }


    public GameClient SendMessage(byte type, string text)
    {
        Send(new ServerFormat0A(type, text));
        LastMessageSent = DateTime.UtcNow;
        return this;
    }

    public GameClient SendMessage(string text)
    {
        Send(new ServerFormat0A(0x02, text));
        LastMessageSent = DateTime.UtcNow;
        return this;
    }

    public void SendOptionsDialog(Mundane mundane, string text, params OptionsDataItem[] options)
    {
        Send(new ServerFormat2F(mundane, text, new OptionsData(options)));
    }

    public void SendOptionsDialog(Mundane mundane, string text, string args, params OptionsDataItem[] options)
    {
        if (Aisling.Map.ID != mundane.Map.ID) return;
        Send(new ServerFormat2F(mundane, text, new OptionsPlusArgsData(options, args)));
    }

    public void SendPopupDialog(Mundane popup, string text, params OptionsDataItem[] options)
    {
        if (Aisling.Map.ID != popup.Map.ID) return;
        Send(new PopupFormat(popup, text, new OptionsData(options)));
    }
    
    public void SendSkillForgetDialog(Mundane mundane, string text, ushort step)
    {
        Send(new ServerFormat2F(mundane, text, new SkillForfeitData(step)));
    }

    public void SendSkillLearnDialog(Mundane mundane, string text, ushort step, IEnumerable<SkillTemplate> skills)
    {
        Send(new ServerFormat2F(mundane, text, new SkillAcquireData(step, skills)));
    }

    public GameClient SendSound(byte sound, Scope scope = Scope.Self)
    {
        Aisling.Show(scope, new ServerFormat19(sound));
        return this;
    }

    public GameClient SendSurroundingSound(byte sound, Scope scope = Scope.NearbyAislings)
    {
        Aisling.Show(scope, new ServerFormat19(sound));
        return this;
    }

    public GameClient SendMapWideSound(byte sound, Scope scope = Scope.AislingsOnSameMap)
    {
        Aisling.Show(scope, new ServerFormat19(sound));
        return this;
    }

    public void SendSpellForgetDialog(Mundane mundane, string text, ushort step)
    {
        Send(new ServerFormat2F(mundane, text, new SpellForfeitData(step)));
    }

    public void SendSpellLearnDialog(Mundane mundane, string text, ushort step, IEnumerable<SpellTemplate> spells)
    {
        Send(new ServerFormat2F(mundane, text, new SpellAcquireData(step, spells)));
    }


}