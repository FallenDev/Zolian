using Darkages.Network.Client;
using Darkages.Sprites;

namespace Darkages.Models;

public class DialogSession(Sprite user, uint serial)
{
    public Action<WorldClient, ushort, string> Callback { get; init; }
}