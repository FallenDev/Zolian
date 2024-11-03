using Darkages.Enums;
using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Templates;

namespace Darkages.Types;

public class Trap
{
    private int _ticks;
    public int CurrentMapId { get; init; }
    public int Duration { get; set; }
    public Position Location { get; init; }
    public Sprite Owner { get; init; }
    public int Radius { get; set; }
    public uint Serial { get; set; }
    public Item TrapItem { get; set; }
    public Action<Sprite, Sprite> Tripped { get; set; }

    public static bool Activate(Trap trap, Sprite target)
    {
        trap.Tripped?.Invoke(trap.Owner, target);
        return RemoveTrap(trap);
    }

    private static bool RemoveTrap(Trap trapToRemove)
    {
        if (!ServerSetup.Instance.Traps.TryRemove(trapToRemove.Serial, out var trap)) return false;
        trap.TrapItem?.Remove();
        return true;
    }

    public static void Set(Sprite obj, ushort image, int duration, int radius = 1, Action<Sprite, Sprite> cb = null)
    {
        var item = new Item();
        var itemTemplate = new ItemTemplate
        {
            Name = "A Hidden Trap",
            Image = image,
            DisplayImage = image,
            Flags = ItemFlags.Trap
        };

        if (obj is Aisling aisling)
            aisling.ActionUsed = "Trap";

        var trap = item.TrapCreate(obj, itemTemplate, duration, radius, cb);
        trap.TrapItem.Release(obj, trap.TrapItem.Position);
        ServerSetup.Instance.Traps.TryAdd(trap.Serial, trap);
    }

    public void Update()
    {
        if (_ticks > Duration)
        {
            RemoveTrap(this);
            _ticks = 0;
        }

        _ticks++;
    }
}