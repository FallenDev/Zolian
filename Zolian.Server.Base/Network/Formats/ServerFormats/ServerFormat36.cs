using Darkages.Models;
using Darkages.Network.Client;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat36 : NetworkFormat
{
    private readonly GameClient _client;

    /// <summary>
    /// World User List
    /// </summary>
    /// <param name="client"></param>
    public ServerFormat36(GameClient client)
    {
        Encrypted = true;
        OpCode = 0x36;
        _client = client;
    }

    [Flags]
    public enum ClassType : byte
    {
        Peasant = 0,
        Berserker = 1,
        Assassin = 2,
        Arcanus = 3,
        Cleric = 4,
        Defender = 5,
        Monk = 6,
        Guild = 0x88
    }

    public enum StatusIcon : byte
    {
        None,
        Busy,
        Away,
        TeamWanted,
        Team,
        SoloHunting,
        TeamHunting,
        Help
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        ListColor GetUserColor(Player user)
        {
            var color = ListColor.White;
            if (_client.Aisling.ExpLevel > user.ExpLevel)
                if (_client.Aisling.ExpLevel - user.ExpLevel < 15)
                    color = ListColor.Orange;
            if (!string.IsNullOrEmpty(user.Clan) && user.Clan == _client.Aisling.Clan)
                color = ListColor.Clan;
            if (user.GameMaster)
                color = ListColor.Red;
            if (user.Ranger)
                color = ListColor.Green;
            if (user.Knight)
                color = ListColor.Green;
            if (user.ArenaHost)
                color = ListColor.Teal;
            return color;
        }

        var users = _client.Server.Clients.Values.Where(i => i?.Aisling != null && i.Aisling.LoggedIn).Select(i => i.Aisling);
        var clansmen = _client.Server.Clients.Values.Where(i => i?.Aisling != null && i.Aisling.LoggedIn && i.Aisling.Clan == _client.Aisling.Clan && !i.Aisling.GameMaster).Select(i => i.Aisling);
        users = users.OrderByDescending(i => i.BaseHp + i.BaseMp * 2);
        clansmen = clansmen.OrderByDescending(i => i.BaseHp + i.BaseMp * 2);
        var enumerable = users.ToList();
        var count = (ushort)enumerable.Count();

        writer.Write(count);
        writer.Write(count);

        foreach (var user in enumerable)
        {
            var color = GetUserColor(user);
            var path = (byte)ClassType.Guild | (byte)user.Path;

            writer.Write((byte)path);
            writer.Write((byte)color);
            writer.Write((byte)user.ActiveStatus);
            writer.WriteStringA(user.GameMaster
                ? "Game Master"
                : $"Vit: {user.BaseHp + user.BaseMp * 2}");
            writer.Write((byte)user.Stage > 4);
            writer.WriteStringA($"{user.Username}");
        }

        foreach (var member in clansmen)
        {
            var color = GetUserColor(member);
            writer.Write((byte)ClassType.Guild);
            writer.Write((byte)color);
            writer.Write((byte)member.ActiveStatus);
            writer.WriteStringA($"Vit: {member.BaseHp + member.BaseMp * 2}");
            writer.Write((byte)member.Stage > 4);
            writer.WriteStringA(member.Username);
        }
    }
}