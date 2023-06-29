using Chaos.Networking.Options;

namespace Chaos.Services.Servers.Options;

public sealed record LoginOptions : ServerOptions
{
    public string NoticeMessage { get; set; } = null!;
    public ReservedRedirectInfo[] ReservedRedirects { get; set; } = Array.Empty<ReservedRedirectInfo>();
    public string StartingMapInstanceId { get; set; } = null!;
    public Point StartingPoint { get; set; }
    public string StartingPointStr { get; set; } = null!;
    public ConnectionInfo WorldRedirect { get; set; } = null!;
}