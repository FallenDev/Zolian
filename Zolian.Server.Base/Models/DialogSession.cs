using Darkages.Network.Client;

namespace Darkages.Models;

public class DialogSession()
{
    public Action<WorldClient, ushort, string> Callback { get; init; }
}