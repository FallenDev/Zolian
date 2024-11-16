using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

namespace Darkages.Network.Components;

public class ObjectComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdateObjects);
    }

    private static async void UpdateObjects()
    {
        var connectedUsers = Server.Aislings;
        var readyLoggedIn = connectedUsers.Where(i => i.Map is { Ready: true } && i.LoggedIn).ToArray();
        if (readyLoggedIn.Length == 0) return;

        try
        {
            foreach (var user in readyLoggedIn)
            {
                if (user.ObjectsUpdating) continue;
                user.ObjectsUpdating = true;

                while (user.LoggedIn)
                {
                    if (user.Client == null) return;
                    UpdateClientObjects(user);
                    await Task.Delay(50);
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
        var objects = ObjectManager.GetObjects(user.Map, selector => selector is not null, ObjectManager.Get.All).ToList();

        foreach (var obj in objects)
        {
            if (obj == null) continue;

            if (obj.WithinRangeOf(user))
                payload.Add(obj);
            else
                RemoveObject(user, obj);
        }

        // Handle OnApproach visible objects
        var toUpdate = AddObjects(payload, user);

        // If within range, send visible entities
        if (payload.Count <= 0) return;
        user.Client.SendVisibleEntities(toUpdate);
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

        self.SpritesInView.TryRemove(objectToRemove.Serial, out _);
        identifiable.HideFrom(self);
    }

    private static List<Sprite> AddObjects(List<Sprite> payload, Aisling self)
    {
        var toUpdate = new List<Sprite>();

        // Validate
        if (self == null || payload == null) return default;

        // HashSet to avoid multiple lookups
        var spritesInViewSet = new HashSet<uint>(self.SpritesInView.Keys);

        foreach (var obj in payload.Where(obj => obj != null && !spritesInViewSet.Contains(obj.Serial)))
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
            self.SpritesInView.AddOrUpdate(obj.Serial, obj, (_, _) => obj);
        }

        return toUpdate;
    }
}