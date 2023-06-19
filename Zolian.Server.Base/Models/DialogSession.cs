using Darkages.Network.Client;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Models;

public class DialogSession
{
    public DialogSession(Sprite user, int serial)
    {
        Serial = serial;
        SessionPosition = user.Position;
        CurrentMapId = user.CurrentMapId;
        Sequence = 0;
    }

    public Action<GameClient, ushort, string> Callback { get; init; }
    public int CurrentMapId { get; }
    public ushort Sequence { get; set; }
    public int Serial { get; }
    public Position SessionPosition { get; }
    public Dialog StateObject { get; set; }
}