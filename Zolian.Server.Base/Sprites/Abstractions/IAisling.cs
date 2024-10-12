using Darkages.Network.Client;

namespace Darkages.Sprites.Abstractions;

public interface IAisling : ISprite
{
    WorldClient Client { get; set; }
}