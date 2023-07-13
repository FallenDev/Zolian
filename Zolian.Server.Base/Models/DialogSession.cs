using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Models;

public class DialogSession
{
    public DialogSession(Sprite user, uint serial)
    {
        Serial = serial;
        SessionPosition = user.Position;
        CurrentMapId = user.CurrentMapId;
        Sequence = 0;
    }

    public Action<WorldClient, ushort, string> Callback { get; init; }
    public int CurrentMapId { get; }
    public ushort Sequence { get; set; }
    public uint Serial { get; }
    public Position SessionPosition { get; }
    public Dialog StateObject { get; set; }
}