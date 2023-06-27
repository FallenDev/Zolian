using System.Net;

using Darkages.Network.Formats.Models.ClientFormats;

namespace Darkages.Network.Client;

public class LoginClient2 : NetworkClient
{
    public ClientFormat02 CreateInfo { get; set; }
    public bool Authorized { get; set; }
    public IPEndPoint ClientIP { get; set; }
}