using Darkages.Enums;
using Darkages.Models;
using Darkages.Network.Client;
using Darkages.Types;

namespace Darkages.GameScripts.Mundanes.Evermore;

internal static class EvermoreQuestHelper
{
    public static int CountDeniedCitizens(Quests quests)
    {
        var count = 0;

        if (quests.EvermoreArchivistDeniedGuild)
            count++;
        if (quests.EvermoreYselleDeniedGuild)
            count++;
        if (quests.EvermoreOrrinDeniedGuild)
            count++;

        return count;
    }

    public static bool HasDeniedAllCitizens(Quests quests) => CountDeniedCitizens(quests) >= 3;

    public static string AssassinRankName(int reputation) => reputation switch
    {
        <= 1 => "Observer",
        2 => "Initiate",
        3 => "Blade",
        4 => "Shadow",
        5 => "Veilbound",
        _ => "Guildmaster's Chosen"
    };

    public static string ThievesRankName(int reputation) => reputation switch
    {
        <= 0 => "Outsider",
        1 => "Lookout",
        2 => "Shadow",
        _ => "Night Broker"
    };

    public static string AssassinLegendRank(int reputation) => reputation switch
    {
        <= 1 => "Assassin's Guild: Observer",
        2 => "Assassin's Guild: Initiate",
        3 => "Assassin's Guild: Blade",
        4 => "Assassin's Guild: Shadow",
        5 => "Assassin's Guild: Veilbound",
        _ => "Assassin's Guild: Guildmaster's Chosen"
    };

    public static string ThievesLegendRank(int reputation) => reputation switch
    {
        <= 0 => "Thieves' Guild: Outsider",
        1 => "Thieves' Guild: Lookout",
        2 => "Thieves' Guild: Shadow",
        _ => "Thieves' Guild: Night Broker"
    };

    public static void AddLegendIfMissing(WorldClient client, string key, LegendColor color, LegendIcon icon, string text)
    {
        if (client.Aisling.LegendBook.HasKey(key) || client.Aisling.LegendBook.Has(text))
            return;

        var legend = new Legend.LegendItem
        {
            Key = key,
            IsPublic = true,
            Time = DateTime.UtcNow,
            Color = color,
            Icon = (byte)icon,
            Text = text
        };

        client.Aisling.LegendBook.AddLegend(legend, client);
    }

    public static void AddKillMark(WorldClient client)
    {
        var legend = new Legend.LegendItem
        {
            Key = $"KOImperial",
            IsPublic = false,
            Time = DateTime.UtcNow,
            Color = LegendColor.Orange,
            Icon = (byte)LegendIcon.Victory,
            Text = $"- Murdered Imperial"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);
    }

    public static void AddTargetedKillMark(WorldClient client, string target)
    {
        var legend = new Legend.LegendItem
        {
            Key = $"KO{target}",
            IsPublic = false,
            Time = DateTime.UtcNow,
            Color = LegendColor.Orange,
            Icon = (byte)LegendIcon.Victory,
            Text = $"- Murdered {target}"
        };

        client.Aisling.LegendBook.AddLegend(legend, client);
    }
}
