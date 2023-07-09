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

public class GameClient
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
}