using Darkages.Sprites;
using Darkages.Types;

using System.Collections;
using System.Collections.Concurrent;

namespace Darkages.Object;

public sealed class ObjectService
{
    public ObjectService()
    {
        foreach (var map in ServerSetup.Instance.GlobalMapCache.Values)
        {
            var spriteCollectionDict = new ConcurrentDictionary<Type, object>();
            spriteCollectionDict.TryAdd(typeof(Monster), new SpriteCollection<Monster>([]));
            spriteCollectionDict.TryAdd(typeof(Aisling), new SpriteCollection<Aisling>([]));
            spriteCollectionDict.TryAdd(typeof(Mundane), new SpriteCollection<Mundane>([]));
            spriteCollectionDict.TryAdd(typeof(Item), new SpriteCollection<Item>([]));
            spriteCollectionDict.TryAdd(typeof(Money), new SpriteCollection<Money>([]));
            ServerSetup.Instance.SpriteCollections.TryAdd(map.ID, spriteCollectionDict);
        }
    }

    public static void AddGameObject<T>(T obj) where T : Sprite
    {
        if (obj.Pos.X >= byte.MaxValue || obj.Pos.Y >= byte.MaxValue ||
            !ServerSetup.Instance.SpriteCollections.TryGetValue(obj.CurrentMapId, out var mapCollections) ||
            !mapCollections.TryGetValue(typeof(T), out var objCollection)) return;
        
        ((SpriteCollection<T>)objCollection).Add(obj);
    }

    public static T Query<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        var collections = map == null ? ServerSetup.Instance.SpriteCollections.Values : new[] { ServerSetup.Instance.SpriteCollections[map.ID] };

        foreach (var mapCollections in collections)
        {
            if (!mapCollections.TryGetValue(typeof(T), out var spriteCollection)) continue;
            var queriedObject = ((SpriteCollection<T>)spriteCollection).Query(predicate);
            if (queriedObject != null) return queriedObject;
        }

        return null;
    }

    public static IEnumerable<T> QueryAll<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map == null)
        {
            var collections = ServerSetup.Instance.SpriteCollections.Values
                .Select(mapDict => mapDict.TryGetValue(typeof(T), out var collection) ? (SpriteCollection<T>)collection : null)
                .Where(collection => collection != null && collection.Any());
            var stack = new List<T>();

            foreach (var obj in collections)
                stack.AddRange(obj.QueryAll(predicate));

            return stack;
        }

        if (!ServerSetup.Instance.SpriteCollections.TryGetValue(map.ID, out var mapCollections)) return null;
        return mapCollections.TryGetValue(typeof(T), out var sprites) ? ((SpriteCollection<T>)sprites).QueryAll(predicate) : null;
    }

    public static void RemoveAllGameObjects<T>(T[] objects) where T : Sprite
    {
        if (objects == null) return;
        foreach (var obj in objects)
            RemoveGameObject(obj);
    }

    public static void RemoveGameObject<T>(T obj) where T : Sprite
    {
        if (obj == null || !ServerSetup.Instance.SpriteCollections.TryGetValue(obj.CurrentMapId, out var mapCollections)) return;
        if (mapCollections.TryGetValue(typeof(T), out var objCollection))
            ((SpriteCollection<T>)objCollection).Delete(obj);
    }
}

public class SpriteCollection<T>(IEnumerable<T> values) : IEnumerable<T>
    where T : Sprite
{
    private readonly List<T> _values = [..values];

    public void Add(T obj)
    {
        if (obj == null) return;

        lock (_values)
        {
            if (_values.Any(i => i.Serial == obj.Serial))
            {
                var eObj = _values.FindIndex(idx => idx.Serial == obj.Serial);

                if (eObj < 0) return;
                _values[eObj] = obj;
                return;
            }

            _values.Add(obj);
        }
    }

    public void Delete(T obj)
    {
        if (obj == null) return;

        for (var i = _values.Count - 1; i >= 0; i--)
        {
            var subject = obj as Sprite;
            var predicate = _values[i] as Sprite;

            if (subject == predicate) _values.RemoveAt(i);
        }
    }

    public IEnumerator<T> GetEnumerator() => _values.GetEnumerator();

    public T Query(Predicate<T> predicate)
    {
        if (predicate == null) return default;

        for (var i = _values.Count - 1; i >= 0; i--)
            if (_values.Count > i)
            {
                var subject = predicate(_values[i]);

                if (subject)
                    return _values[i].Abyss ? default : _values[i];
            }

        return default;
    }

    public IEnumerable<T> QueryAll(Predicate<T> predicate)
    {
        if (predicate == null) yield return default;

        for (var i = _values.Count - 1; i >= 0; i--)
            if (i < _values.Count)
            {
                var subject = predicate != null && predicate(_values[i]);
                if (subject) yield return _values[i].Abyss ? default : _values[i];
            }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}