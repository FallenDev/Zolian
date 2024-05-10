using Darkages.Sprites;
using Darkages.Types;

namespace Darkages.Object;

public class ObjectManager
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

    public static void AddObject<T>(T obj, Predicate<T> p = null) where T : Sprite
    {
        if (p == null || p(obj))
            ObjectService.AddGameObject(obj);
    }

    public static void DelObject<T>(T obj) where T : Sprite => ObjectService.RemoveGameObject(obj);

    public static T GetObject<T>(Area map, Predicate<T> p) where T : Sprite => ObjectService.Query(map, p);

    public static Sprite GetObject(Area map, Predicate<Sprite> p, Get selections) => GetObjects(map, p, selections).FirstOrDefault();

    public static IEnumerable<T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite => ObjectService.QueryAll(map, p);

    public static IEnumerable<Sprite> GetObjects(Area map, Predicate<Sprite> p, Get selections)
    {
        List<Sprite> bucket = [];

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