using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat2E : NetworkFormat
{
    private readonly Aisling _user;

    /// <summary>
    /// Field Map
    /// </summary>
    /// <param name="user"></param>
    public ServerFormat2E(Aisling user) : this() => _user = user;

    private ServerFormat2E()
    {
        Command = 0x2E;
        Encrypted = true;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer)
    {
        if (_user == null) return;
        if (!ServerSetup.Instance.GlobalWorldMapTemplateCache.ContainsKey(_user.Client.Aisling.World)) return;
            
        _user.Client.MapOpen = true;
        var portal = ServerSetup.Instance.GlobalWorldMapTemplateCache[_user.Client.Aisling.World];
        var name = $"field{portal.FieldNumber:000}";

        writer.WriteStringA(name);
        writer.Write((byte)portal.Portals.Count);
        writer.Write((byte)portal.FieldNumber);

        foreach (var warps in portal.Portals.Where(warps => warps?.Destination != null))
        {
            writer.Write(warps.PointY);
            writer.Write(warps.PointX);
            writer.Write((byte)warps.DisplayName.Length);
            writer.WriteAscii(warps.DisplayName);
            writer.Write(warps.Destination.AreaID);
            writer.Write((short)warps.Destination.Location.X);
            writer.Write((short)warps.Destination.Location.Y);
        }
    }
}