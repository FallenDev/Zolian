using Chaos.Common.Definitions;
using Darkages.Common;
using Darkages.Infrastructure;
using Darkages.Network.Server;
using Darkages.Object;
using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Network.Components;

public class ObjectComponent : WorldServerComponent
{
    private readonly WorldServerTimer _timer = new(TimeSpan.FromMilliseconds(20));

    public ObjectComponent(WorldServer server) : base(server) { }

    protected internal override void Update(TimeSpan elapsedTime)
    {
        if (_timer.Update(elapsedTime)) ZolianUpdateDelegate.Update(UpdateObjects);
    }

    private void UpdateObjects()
    {
        var connectedUsers = Server.Aislings;
        var readyLoggedIn = connectedUsers.Where(i => i.Map is { Ready: true } && i.LoggedIn).ToArray();

        foreach (var user in readyLoggedIn)
        {
            UpdateClientObjects(user);

            var onMap = user.Map.IsLocationOnMap(user);

            if (onMap) continue;
            user.Client.TransitionToMap(136, new Position(5, 7));
            user.Client.SendServerMessage(ServerMessageType.ActiveMessage, "Something grabs your hand...");
        }
    }

    private static void UpdateClientObjects(Aisling user)
    {
        var payload = new List<Sprite>();

        if (user?.Map == null) return;
        if (!user.LoggedIn || !user.Map.Ready) return;
        if (!user.Client.SerialSent) return;

        var objects = user.GetObjects(user.Map, selector => selector is not null, ObjectManager.Get.All).ToArray();
        var objectsInView = objects.Where(s => s is not null && s.WithinRangeOf(user)).ToArray();
        var objectsNotInView = objects.Where(s => s is not null && !s.WithinRangeOf(user)).ToArray();

        RemoveObjects(user, objectsNotInView);
        AddObjects(payload, user, objectsInView);

        if (payload.Count <= 0) return;
        payload.Reverse();
        user.Client.SendVisibleEntities(payload);
    }

    private static void RemoveObjects(Aisling client, Sprite[] objectsToRemove)
    {
        if (objectsToRemove == null) return;
        if (client == null) return;

        foreach (var obj in objectsToRemove)
        {
            if (obj.Serial == client.Serial) continue;
            if (!client.View.ContainsKey(obj.Serial)) continue;
            if (!client.View.TryRemove(obj.Serial, out _)) continue;

            if (obj is Monster monster)
            {
                if (monster.Summoner != null) continue;

                var valueCollection = monster.Scripts?.Values;

                if (valueCollection != null)
                    foreach (var script in valueCollection)
                        script.OnLeave(client.Client);
            }

            obj.HideFrom(client);
        }
    }

    private static void AddObjects(ICollection<Sprite> payload, Aisling self, IEnumerable<Sprite> objectsToAdd)
    {
        if (payload == null) return;
        if (self == null) return;
        if (objectsToAdd == null) return;

        foreach (var obj in objectsToAdd)
        {
            if (obj.Serial == self.Serial) continue;
            if (self.View.ContainsKey(obj.Serial)) continue;
            // If object is not an item or money, try to add it; If you cannot add it, continue
            if (obj is not Item or Money)
                if (!self.View.TryAdd(obj.Serial, obj)) continue;

            if (obj is Monster monster)
            {
                var valueCollection = monster.Scripts?.Values;

                if (monster.Template != null && monster.Map != null)
                    Monster.InitScripting(monster.Template, monster.Map, monster);

                if (valueCollection != null && valueCollection.Any())
                    foreach (var script in valueCollection)
                        script.OnApproach(self.Client);
            }

            if (obj is Aisling other)
            {
                if (self.Serial == other.Serial)
                    continue;

                if (self.CanSeeSprite(other))
                    other.ShowTo(self);

                if (other.CanSeeSprite(self))
                    self.ShowTo(other);
            }
            else
            {
                self.View.TryAdd(obj.Serial, obj);
                payload.Add(obj);
            }
        }
    }
}