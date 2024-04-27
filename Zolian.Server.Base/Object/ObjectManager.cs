using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Object;

public interface IObjectManager
{
    void AddObject<T>(T obj, Predicate<T> p = null) where T : Sprite;
    void DelObject<T>(T obj) where T : Sprite;
    void DelObjects<T>(T[] obj) where T : Sprite;
    T GetObject<T>(Area map, Predicate<T> p) where T : Sprite;
    Sprite GetObject(Area map, Predicate<Sprite> p, ObjectManager.Get selections);
    T GetObjectByName<T>(string name, Area map = null) where T : Sprite, new();
    IEnumerable<T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite;
    IEnumerable<Sprite> GetObjects(Area map, Predicate<Sprite> p, ObjectManager.Get selections);
}

public class ObjectManager : IObjectManager
{
    [Flags]
    public enum Get
    {
        Aislings = 1,
        MonsterDamage = Aislings,
        Monsters = 2,
        AislingDamage = Monsters | Aislings,
        Mundanes = 4,
        UpdateNonPlayerSprites = Monsters | Mundanes,
        Items = 8,
        Money = 16,
        AllButAislings = Monsters | Mundanes | Items | Money,
        All = Aislings | Items | Money | Monsters | Mundanes
    }

    public void AddObject<T>(T obj, Predicate<T> p = null) where T : Sprite
    {
        if (p != null && p(obj))
            ObjectService.AddGameObject(obj);
        else if (p == null)
            ObjectService.AddGameObject(obj);
    }

    public void DelObject<T>(T obj) where T : Sprite => ObjectService.RemoveGameObject(obj);

    public void DelObjects<T>(T[] obj) where T : Sprite => ObjectService.RemoveAllGameObjects(obj);

    public T GetObject<T>(Area map, Predicate<T> p) where T : Sprite => ObjectService.Query(map, p);

    public Sprite GetObject(Area map, Predicate<Sprite> p, Get selections) => GetObjects(map, p, selections).FirstOrDefault();

    public T GetObjectByName<T>(string name, Area map = null) where T : Sprite, new()
    {
        var objType = new T();

        return objType switch
        {
            Aisling => GetObject<Aisling>(null,
                i => i != null && string.Equals(i.Username.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).CastSpriteToType<T>(),
            Monster => GetObject<Monster>(map,
                i => i != null && string.Equals(i.Template.Name.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).CastSpriteToType<T>(),
            Mundane => GetObject<Mundane>(map,
                i => i != null && string.Equals(i.Template.Name.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).CastSpriteToType<T>(),
            Item => GetObject<Item>(map,
                i => i != null && string.Equals(i.Template.Name.ToLower(), name.ToLower(),
                    StringComparison.InvariantCulture)).CastSpriteToType<T>(),
            _ => null
        };
    }

    public IEnumerable<T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite => ObjectService.QueryAll(map, p);

    public IEnumerable<Sprite> GetObjects(Area map, Predicate<Sprite> p, Get selections)
    {
        List<Sprite> bucket = new();

        switch (selections)
        {
            case Get.Aislings:
                bucket.AddRange(GetObjects<Aisling>(map, p));
                break;
            case Get.Monsters:
                bucket.AddRange(GetObjects<Monster>(map, p));
                break;
            case Get.AislingDamage:
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Aisling>(map, p));
                break;
            case Get.Mundanes:
                bucket.AddRange(GetObjects<Mundane>(map, p));
                break;
            case Get.UpdateNonPlayerSprites:
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                break;
            case Get.Items:
                bucket.AddRange(GetObjects<Item>(map, p));
                break;
            case Get.Money:
                bucket.AddRange(GetObjects<Money>(map, p));
                break;
            case Get.AllButAislings:
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                bucket.AddRange(GetObjects<Item>(map, p));
                bucket.AddRange(GetObjects<Money>(map, p));
                break;
            case Get.All:
                bucket.AddRange(GetObjects<Aisling>(map, p));
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                bucket.AddRange(GetObjects<Money>(map, p));
                bucket.AddRange(GetObjects<Item>(map, p));
                break;
            default:
                bucket.AddRange(GetObjects<Aisling>(map, p));
                bucket.AddRange(GetObjects<Monster>(map, p));
                bucket.AddRange(GetObjects<Mundane>(map, p));
                bucket.AddRange(GetObjects<Money>(map, p));
                bucket.AddRange(GetObjects<Item>(map, p));
                break;
        }

        return bucket;
    }
}