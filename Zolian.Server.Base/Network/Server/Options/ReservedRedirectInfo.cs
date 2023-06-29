namespace Chaos.Services.Servers.Options;

public record ReservedRedirectInfo
{
    public byte Id { get; set; }
    public string Name { get; set; } = null!;
}