using Darkages.Sprites;

namespace Darkages.Network.Formats.Models.ServerFormats;

public class ServerFormat42 : NetworkFormat
{
    private Item _exchangedItem;
    private string _message;
    private Aisling _player;
    private byte _stage;
    public byte Type;

    /// <summary>
    /// Exchange
    /// </summary>
    /// <param name="user"></param>
    /// <param name="type"></param>
    /// <param name="method"></param>
    /// <param name="lpMsg"></param>
    /// <param name="lpItem"></param>
    public ServerFormat42(Aisling user, byte type = 0x00, byte method = 0x00, string lpMsg = "", Item lpItem = null) : this()
    {
        if (method <= 0)
            throw new ArgumentOutOfRangeException(nameof(method));

        _stage = type;
        _player = user;
        _exchangedItem = lpItem;
        _message = lpMsg;
    }

    private ServerFormat42()
    {
        Encrypted = true;
        OpCode = 0x42;
    }

    public override void Serialize(NetworkPacketReader reader) { }

    public override void Serialize(NetworkPacketWriter writer) { }
}