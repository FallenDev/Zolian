using Darkages.Common;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;

namespace Darkages.Network.Components;

public class ObjectComponent(WorldServer server) : WorldServerComponent(server)
{
    protected internal override void Update(TimeSpan elapsedTime)
    {
        ZolianUpdateDelegate.Update(UpdateObjects);
    }

    private static void UpdateObjects()
    {
        var connectedUsers = Server.Aislings;
        var readyLoggedIn = connectedUsers.Where(i => i.Map is { Ready: true } && i.LoggedIn).ToArray();
        if (readyLoggedIn.Length == 0) return;

        try
        {
            Parallel.ForEach(readyLoggedIn, (user) =>
            {
                while (user.LoggedIn)
                {
                    if (user.Client == null) return;
                    UpdateClientObjects(user);
                    return;
                }
            });
        }
        catch
        {
            // ignored
        }
    }

    private static void UpdateClientObjects(Aisling user)
    {
        var payload = new List<Sprite>();
        var objects = ObjectManager.GetObjects(user.Map, selector => selector is not null, ObjectManager.Get.All).ToList();
        var objectsInView = objects.Where(s => s is not null && s.WithinRangeOf(user)).ToList();
        var objectsNotInView = objects.Where(s => s is not null && !s.WithinRangeOf(user)).ToList();

        CheckIfSpritesStillInView(user, objectsInView);
        RemoveObjects(user, objectsNotInView);
        AddObjects(payload, user, objectsInView);

        if (payload.Count <= 0) return;
        payload.Reverse();
        user.Client.SendVisibleEntities(payload);
    }

    private static void CheckIfSpritesStillInView(Aisling self, List<Sprite> objectsInView)
    {
        foreach (var spritesPair in self.SpritesInView)
        {
            if (objectsInView.Contains(spritesPair.Value)) continue;
            self.SpritesInView.TryRemove(spritesPair.Key, out _);
        }
    }

    private static void RemoveObjects(Aisling self, IReadOnlyCollection<Sprite> objectsToRemove)
    {
        if (objectsToRemove == null) return;
        if (self == null) return;

        foreach (var obj in objectsToRemove)
        {
            if (obj is not Identifiable identifiable) continue;
            if (obj.Serial == self.Serial) continue;

            if (obj is Monster monster)
            {
                var script = monster.Scripts?.Values.First();
                script?.OnLeave(self.Client);
            }

            self.SpritesInView.TryRemove(obj.Serial, out _);
            identifiable.HideFrom(self);
        }
    }

    private static void AddObjects(List<Sprite> payload, Aisling self, IReadOnlyCollection<Sprite> objectsToAdd)
    {
        payload ??= [];
        if (self == null) return;
        if (objectsToAdd == null) return;

        foreach (var obj in objectsToAdd)
        {
            if (obj == null) continue;
            // If value is in view, don't add it
            if (self.SpritesInView.TryGetValue(obj.Serial, out _)) continue;

            if (obj is Monster monster)
            {
                var script = monster.Scripts?.Values.First();
                script?.OnApproach(self.Client);
            }

            if (obj is Mundane npc)
            {
                var script = npc.Scripts?.Values.First();
                script?.OnApproach(self.Client);
            }

            if (obj is Aisling other)
            {
                // Self Check
                if (self.Serial == other.Serial) continue;

                // Invisible Checks
                if (self.CanSeeSprite(other))
                    other.ShowTo(self);

                if (other.CanSeeSprite(self))
                    self.ShowTo(other);
            }
            else
            {
                payload.Add(obj);
                self.SpritesInView.AddOrUpdate(obj.Serial, obj, (_, _) => obj);
            }
        }
    }
}