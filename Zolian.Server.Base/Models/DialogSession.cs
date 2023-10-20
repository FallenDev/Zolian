using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Models;

public class DialogSession(Sprite user, uint serial)
{
    public Action<WorldClient, ushort, string> Callback { get; init; }
    public int CurrentMapId { get; } = user.CurrentMapId;
    public ushort Sequence { get; set; } = 0;
    public uint Serial { get; } = serial;
    public Position SessionPosition { get; } = user.Position;
    public Dialog StateObject { get; set; }
}