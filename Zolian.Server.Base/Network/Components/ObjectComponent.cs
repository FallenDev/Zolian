using System.Diagnostics;

using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class ObjectComponent(WorldServer server) : WorldServerComponent(server)
{
    private const int ComponentSpeed = 50;

    protected internal override async Task Update()
    {
        var componentStopWatch = new Stopwatch();
        componentStopWatch.Start();
        var variableGameSpeed = ComponentSpeed;

        while (ServerSetup.Instance.Running)
        {
            if (componentStopWatch.Elapsed.TotalMilliseconds < variableGameSpeed)
            {
                await Task.Delay(1);
                continue;
            }

            _ = UpdateObjects();
            var awaiter = (int)(ComponentSpeed - componentStopWatch.Elapsed.TotalMilliseconds);

            if (awaiter < 0)
            {
                variableGameSpeed = ComponentSpeed + awaiter;
                componentStopWatch.Restart();
                continue;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(awaiter));
            variableGameSpeed = ComponentSpeed;
            componentStopWatch.Restart();
        }
    }

    private static async Task UpdateObjects()
    {
        var connectedUsers = Server.Aislings;
        var readyLoggedIn = connectedUsers.Where(i => i.Map is { Ready: true } && i.LoggedIn).ToArray();
        if (readyLoggedIn.Length == 0) return;

        try
        {
            foreach (var user in readyLoggedIn)
            {
                user.ObjectsUpdating = true;

                while (user.LoggedIn && user.ObjectsUpdating)
                {
                    if (user.Client == null) return;
                    HandleObjectsOutOfView(user);
                    UpdateClientObjects(user);
                    await Task.Delay(50);
                    user.ObjectsUpdating = false;
                }
            }
        }
        catch
        {
            // ignored
        }
    }

    private static void UpdateClientObjects(Aisling user)
    {
        // Initialize payload
        var payload = new List<Sprite>();

        // Retrieve and categorize
        var objects = ObjectManager.GetObjects(user.Map, s => s.WithinRangeOf(user, 13), ObjectManager.Get.All).ToList();

        foreach (var obj in objects)
        {
            switch (obj)
            {
                case null:
                    continue;
                case Item item when !user.SpritesInView.ContainsKey(item.ItemVisibilityId):
                    {
                        payload.Add(obj);
                        break;
                    }
                case Aisling:
                case Monster:
                case Mundane:
                case Money:
                    {
                        if (!user.SpritesInView.ContainsKey(obj.Serial))
                            payload.Add(obj);
                        break;
                    }
            }
        }

        // Handle OnApproach visible objects
        var toUpdate = AddObjects(payload, user);

        // If within range, send visible entities
        if (payload.Count <= 0) return;
        user.Client.SendVisibleEntities(toUpdate);
    }

    private static void HandleObjectsOutOfView(Aisling user)
    {
        foreach (var (serial, sprite) in user.SpritesInView)
        {
            switch (sprite)
            {
                case null:
                    user.SpritesInView.TryRemove(serial, out _);
                    continue;
                case Item item:
                {
                    if (user.SpritesInView.ContainsKey(item.ItemVisibilityId) && (item.ItemPane != Item.ItemPanes.Ground || !sprite.WithinRangeOf(user, 13)))
                        RemoveObject(user, sprite);
                    break;
                }
                case Aisling:
                case Monster:
                case Mundane:
                case Money:
                {
                    if (!sprite.WithinRangeOf(user, 13) && user.SpritesInView.ContainsKey(sprite.Serial))
                        RemoveObject(user, sprite);
                    break;
                }
            }
        }
    }

    private static void RemoveObject(Aisling self, Sprite objectToRemove)
    {
        // Validate
        if (self == null || objectToRemove == null) return;
        if (objectToRemove is not Identifiable identifiable || objectToRemove.Serial == self.Serial) return;

        if (objectToRemove is Monster monster)
        {
            var script = monster.Scripts?.Values.FirstOrDefault();
            script?.OnLeave(self.Client);
        }

        if (objectToRemove is Item item)
            self.SpritesInView.TryRemove(item.ItemVisibilityId, out _);
        else
            self.SpritesInView.TryRemove(objectToRemove.Serial, out _);
        identifiable.HideFrom(self);
    }

    private static List<Sprite> AddObjects(List<Sprite> payload, Aisling self)
    {
        var toUpdate = new List<Sprite>();

        // Validate
        if (self == null || payload == null) return default;
        
        foreach (var obj in payload.Where(obj => obj != null))
        {
            // Handle sprite types
            switch (obj)
            {
                case Monster monster:
                    {
                        var script = monster.Scripts?.Values.FirstOrDefault();
                        script?.OnApproach(self.Client);
                        break;
                    }
                case Mundane npc:
                    {
                        var script = npc.Scripts?.Values.FirstOrDefault();
                        script?.OnApproach(self.Client);
                        break;
                    }
                case Aisling other:
                    {
                        // Self Check
                        if (self.Serial == other.Serial) continue;

                        // Invisible Checks
                        if (self.CanSeeSprite(other))
                            other.ShowTo(self);

                        if (other.CanSeeSprite(self))
                            self.ShowTo(other);
                        continue;
                    }
            }

            toUpdate.Add(obj);
            if (obj is Item item)
                self.SpritesInView.AddOrUpdate(item.ItemVisibilityId, obj, (_, _) => obj);
            else
                self.SpritesInView.AddOrUpdate(obj.Serial, obj, (_, _) => obj);
        }

        return toUpdate;
    }
}