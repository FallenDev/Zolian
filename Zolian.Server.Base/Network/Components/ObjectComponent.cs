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
            Parallel.ForEach(readyLoggedIn.Where(player => player is { LoggedIn: true }), (user) =>
            {
                if (user?.Client == null) return;
                UpdateClientObjects(user);
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
        var loopBreak = false;

        while (user.LoggedIn && user.Map.Ready && !loopBreak)
        {
            var objects = user.GetObjects(user.Map, selector => selector is not null, ObjectManager.Get.All).ToList();
            var objectsInView = objects.Where(s => s is not null && s.WithinRangeOf(user)).ToList();
            var objectsNotInView = objects.Where(s => s is not null && !s.WithinRangeOf(user)).ToList();

            CheckIfSpritesStillInView(user, objectsInView);
            RemoveObjects(user, objectsNotInView);
            AddObjects(payload, user, objectsInView);

            if (payload.Count <= 0) return;
            payload.Reverse();
            user.Client.SendVisibleEntities(payload);
            loopBreak = true;
        }
    }

    private static void CheckIfSpritesStillInView(Aisling self, List<Sprite> objectsInView)
    {
        Parallel.ForEach(self.SpritesInView, spritesPair =>
        {
            if (objectsInView.Contains(spritesPair.Value)) return;
            self.SpritesInView.TryRemove(spritesPair.Key, out _);
        });
    }

    private static void RemoveObjects(Aisling self, IReadOnlyCollection<Sprite> objectsToRemove)
    {
        if (objectsToRemove == null) return;
        if (self == null) return;

        Parallel.ForEach(objectsToRemove, obj =>
        {
            if (obj == null) return;
            if (obj.Serial == self.Serial) return;

            if (obj is Monster monster)
            {
                var script = monster.Scripts?.Values.First();
                script?.OnLeave(self.Client);
            }

            self.SpritesInView.TryRemove(obj.Serial, out _);
            obj.HideFrom(self);
        });
    }

    private static void AddObjects(List<Sprite> payload, Aisling self, IReadOnlyCollection<Sprite> objectsToAdd)
    {
        payload ??= []; 
        if (self == null) return;
        if (objectsToAdd == null) return;

        Parallel.ForEach(objectsToAdd, obj =>
        {
            if (obj == null) return;
            // If value is in view, don't add it
            if (self.SpritesInView.TryGetValue(obj.Serial, out _)) return;

            if (obj is Monster monster)
            {
                var script = monster.Scripts?.Values.First();
                script?.OnApproach(self.Client);
            }

            if (obj is Aisling other)
            {
                // Self Check
                if (self.Serial == other.Serial) return;

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
        });
    }
}