using System.Diagnostics;
using System.Runtime.CompilerServices;

using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Network.Components;

public class ObjectComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 30;

    protected internal override async Task Update()
    {
        var sw = Stopwatch.StartNew();
        var target = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            var elapsed = sw.Elapsed.TotalMilliseconds;

            if (elapsed < target)
            {
                var remaining = (int)(target - elapsed);
                if (remaining > 0)
                    await Task.Delay(Math.Min(remaining, 1));
                continue;
            }

            UpdateObjects();

            var post = sw.Elapsed.TotalMilliseconds;
            var overshoot = post - ComponentSpeed;

            target = (overshoot > 0 && overshoot < ComponentSpeed)
                ? ComponentSpeed - (int)overshoot
                : ComponentSpeed;

            sw.Restart();
        }
    }

    private static void UpdateObjects()
    {
        foreach (var user in Server.Aislings)
        {
            if (user?.Client == null) continue;
            if (!user.LoggedIn) continue;
            if (user.Map is not { Ready: true }) continue;

            try
            {
                // Remove objects that left view
                HandleObjectsOutOfView(user);

                // Add objects that entered view
                UpdateClientObjects(user);
            }
            catch
            {
                // ignored
            }
        }
    }

    private static void UpdateClientObjects(Aisling user)
    {
        // Lazy initialize, the animation payload list only if we encounter an enchantable item
        List<(ushort anim, Position pos)> animPayload = null;

        // Get nearby sprites
        var objects = ObjectManager
            .GetObjects(user.Map, s => s.WithinRangeOf(user, 13), ObjectManager.Get.All)
            .ToArray();

        var payload = new List<Sprite>(objects.Length);

        foreach (var obj in objects)
        {
            if (obj == null) continue;

            switch (obj)
            {
                case Item item:
                    {
                        if (!user.SpritesInView.ContainsKey(item.ItemVisibilityId))
                            payload.Add(item);

                        if (!user.GameSettings.GroundQualities) break;
                        if (!item.Template.Enchantable) break;

                        var anim = GetQualityAnimId(item);
                        if (anim == 0) break;

                        // Initialize once per update loop -- pre-size initially to 4
                        animPayload ??= new List<(ushort anim, Position pos)>(4);
                        var itemPos = item.Position;
                        animPayload.Add((anim, itemPos));
                    }
                    break;

                case Aisling or Monster or Mundane or Money:
                    if (!user.SpritesInView.ContainsKey(obj.Serial))
                        payload.Add(obj);
                    break;
            }
        }

        if (payload.Count > 0)
        {
            var toUpdate = AddObjects(payload, user);
            if (toUpdate.Count > 0)
                user.Client.SendVisibleEntities(toUpdate);
        }

        if (animPayload is null || animPayload.Count == 0) return;

        var animCount = animPayload.Count;

        for (var i = 0; i < animCount; i++)
        {
            var (anim, pos) = animPayload[i];
            user.Client.SendAnimation(anim, pos);
        }
    }

    private static void HandleObjectsOutOfView(Aisling user)
    {
        foreach (var (serial, sprite) in user.SpritesInView)
        {
            if (sprite == null)
            {
                user.SpritesInView.TryRemove(serial, out _);
                continue;
            }

            switch (sprite)
            {
                case Item item:
                    if (item.ItemPane != Item.ItemPanes.Ground ||
                        !sprite.WithinRangeOf(user, 13))
                    {
                        RemoveObject(user, sprite);
                    }
                    break;

                case Aisling or Monster or Mundane or Money:
                    if (!sprite.WithinRangeOf(user, 13))
                        RemoveObject(user, sprite);
                    break;
            }
        }
    }

    private static void RemoveObject(Aisling self, Sprite obj)
    {
        if (self == null || obj == null) return;
        if (obj is not Identifiable identifiable) return;
        if (obj.Serial == self.Serial) return;

        if (obj is Monster monster)
        {
            var script = monster.AIScript;
            script?.OnLeave(self.Client);
        }

        if (obj is Item item)
            self.SpritesInView.TryRemove(item.ItemVisibilityId, out _);
        else
            self.SpritesInView.TryRemove(obj.Serial, out _);

        identifiable.HideFrom(self);
    }

    private static List<Sprite> AddObjects(List<Sprite> payload, Aisling self)
    {
        var toUpdate = new List<Sprite>();
        if (self == null) return toUpdate;

        foreach (var obj in payload)
        {
            if (obj == null) continue;

            switch (obj)
            {
                case Monster monster:
                    monster.AIScript?.OnApproach(self.Client);
                    break;

                case Mundane npc:
                    npc.AIScript?.OnApproach(self.Client);
                    break;

                case Aisling other:
                    if (self.Serial == other.Serial) continue;

                    if (self.CanSeeSprite(other))
                        other.ShowTo(self);

                    if (other.CanSeeSprite(self))
                        self.ShowTo(other);

                    continue;
            }

            toUpdate.Add(obj);

            if (obj is Item item)
                self.SpritesInView[item.ItemVisibilityId] = obj;
            else
                self.SpritesInView[obj.Serial] = obj;
        }

        return toUpdate;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ushort GetQualityAnimId(Item item)
    {
        return item.ItemQuality switch
        {
            Item.Quality.Epic => 397,
            Item.Quality.Legendary => 398,
            Item.Quality.Forsaken => 399,
            Item.Quality.Mythic or Item.Quality.Primordial or Item.Quality.Transcendent => 400,
            _ => 0
        };
    }

}