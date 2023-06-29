using Chaos.Networking.Options;

namespace Chaos.Services.Servers.Options;

public sealed record LobbyOptions : ServerOptions
{
    /// <inheritdoc />
    public override string HostName { get; set; } = string.Empty;
    public LoginServerInfo[] Servers { get; set; } = Array.Empty<LoginServerInfo>();
}