using Darkages.Sprites;
using Darkages.Types;

using System.Collections.Concurrent;
using JetBrains.Annotations;
using Darkages.Sprites.Entity;
using Darkages.Network.Server;

namespace Darkages.Object;

public abstract class ObjectService
{
    public static void AddGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null) return;
        if (obj.Pos.X >= byte.MaxValue || obj.Pos.Y >= byte.MaxValue) return;

        // Key Generation
        var mapId = obj.CurrentMapId;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!obj.Map.SpriteCollections.TryGetValue(key, out var objCollection)) return;

        var spriteCollection = (SpriteCollection<T>)objCollection;
        spriteCollection.AddOrUpdate(obj);
    }

    public static void RemoveGameObject<T>(T obj) where T : Sprite
    {
        if (obj is null) return;

        // Key Generation
        var mapId = obj.CurrentMapId;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!obj.Map.SpriteCollections.TryGetValue(key, out var objCollection)) return;

        var spriteCollection = (SpriteCollection<T>)objCollection;
        spriteCollection.Delete(obj);
    }

    public static T Query<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null) return default;

        // Key Generation
        var spriteType = typeof(T);
        var key = Tuple.Create(map.ID, spriteType);

        return !map.SpriteCollections.TryGetValue(key, out var objCollection) ? default : ((SpriteCollection<T>)objCollection).Query(predicate);
    }

    public static ConcurrentDictionary<long, T> QueryAll<T>(Area map, Predicate<T> predicate) where T : Sprite
    {
        if (map is null) return default;

        // Key Generation
        var mapId = map.ID;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!map.SpriteCollections.TryGetValue(key, out var objCollection) ||
            objCollection is not SpriteCollection<T> spriteCollection) return default;

        return spriteCollection.QueryAll(predicate);
    }

    public static ConcurrentDictionary<long, T> QueryAll<T>(Predicate<T> predicate) where T : Sprite
    {
        if (predicate is not Sprite sprite) return default;
        var map = sprite.Map;
        if (map is null) return default;

        // Key Generation
        var mapId = map.ID;
        var spriteType = typeof(T);
        var key = Tuple.Create(mapId, spriteType);

        if (!map.SpriteCollections.TryGetValue(key, out var objCollection) ||
            objCollection is not SpriteCollection<T> spriteCollection) return default;

        return spriteCollection.QueryAll(predicate);
    }
}

public class SpriteCollection<T> : ConcurrentDictionary<long, T> where T : Sprite
{
    private readonly ConcurrentDictionary<long, T> _values = [];

    public void AddOrUpdate(T obj)
    {
        if (obj is null) return;
        if (obj is Item item)
        {
            _values.AddOrUpdate(item.ItemVisibilityId, obj, (_, _) => obj);
            return;
        }

        _values.AddOrUpdate(obj.Serial, obj, (_, _) => obj);
    }

    public void Delete(T obj)
    {
        if (obj is null) return;
        if (obj is Item item)
        {
            _values.TryRemove(item.ItemVisibilityId, out _);
            return;
        }

        var deleted = _values.TryRemove(obj.Serial, out _);
        if (deleted) return;

        _values.TryGetValue(obj.Serial, out var objToDelete);
        if (objToDelete == null) return;
        ServerSetup.EventsLogger($"Object Service could not delete sprite {obj.Serial} attempting to delete again.");
        _values.TryRemove(objToDelete.Serial, out _);
    }

    [CanBeNull] public T Query(Predicate<T> predicate) => _values.Values.FirstOrDefault(item => predicate(item));

    public ConcurrentDictionary<long, T> QueryAll(Predicate<T> predicate)
    {
        var result = new ConcurrentDictionary<long, T>();

        foreach (var (key, sprite) in _values)
        {
            if (sprite != null && predicate(sprite))
            {
                result.TryAdd(key, sprite);
            }
        }

        return result;
    } 
}