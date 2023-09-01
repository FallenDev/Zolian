using System.Collections.Concurrent;
using Chaos.Common.Identity;
using Darkages.Enums;
using Darkages.Sprites;
using Darkages.Templates;

namespace Darkages.Types;

public class Trap
{
    public static readonly ConcurrentDictionary<uint, Trap> Traps = new();
    private int _ticks;

    private Trap()
    {
        _ticks = 0;
    }

    public int CurrentMapId { get; init; }
    private int Duration { get; set; }
    public Position Location { get; init; }
    public Sprite Owner { get; init; }
    private int Radius { get; set; }
    private uint Serial { get; set; }
    private Item TrapItem { get; set; }
    private Action<Sprite, Sprite> Tripped { get; set; }

    public static bool Activate(Trap trap, Sprite target)
    {
        trap.Tripped?.Invoke(trap.Owner, target);
        return RemoveTrap(trap);
    }

    private static bool RemoveTrap(Trap trapToRemove)
    {
        if (!Traps.TryRemove(trapToRemove.Serial, out var trap)) return false;
        trap.TrapItem?.Remove();
        return true;
    }

    public static bool Set(Sprite obj, ushort image, int duration, int radius = 1, Action<Sprite, Sprite> cb = null)
    {
        var item = new Item();
        var itemTemplate = new ItemTemplate
        {
            Name = "A Hidden Trap",
            Image = image,
            DisplayImage = image,
            Flags = ItemFlags.Trap
        };

        item.Template = itemTemplate;
        var pos = obj.Position;

        if (obj is Aisling aisling)
        {
            aisling.ActionUsed = "Trap";
        }

        item = item.TrapCreate(obj, itemTemplate);
        item.Release(obj, pos, false);

        var id = EphemeralRandomIdGenerator<uint>.Shared.NextId;

        return Traps.TryAdd(id, new Trap
        {
            Radius = radius,
            Duration = duration,
            CurrentMapId = obj.CurrentMapId,
            Location = pos,
            Owner = obj,
            Tripped = cb,
            Serial = id,
            TrapItem = item
        });
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