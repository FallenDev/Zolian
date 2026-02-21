namespace Darkages.Models;

public class ServerOptions
{
    public string Location { get; init; }
    public string KeyCode { get; init; }
    public string Unlock { get; init; }
    public string ServerIp { get; init; }
    public string[] GameMastersIPs { get; init; }
    public string InternalIp { get; init; }
    public bool WebApiEnabled { get; init; }
    public string WebApiPrefix { get; init; }
    public string WebApiApiKey { get; init; }
    public string WebApiAllowedOrigin { get; init; }
}