using System.Collections.Concurrent;

using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

namespace Darkages.Object;

public class ObjectManager
{
    [Flags]
    public enum Get
    {
        Aislings = 1,
        Monsters = 2,
        Damageable = Monsters | Aislings,
        Mundanes = 4,
        UpdateNonPlayerSprites = Monsters | Mundanes,
        Movable = Monsters | Aislings | Mundanes,
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
    public static ConcurrentDictionary<long, T> GetObjects<T>(Area map, Predicate<T> p) where T : Sprite => map is null ? new ConcurrentDictionary<long, T>() : ObjectService.QueryAllWithPredicate(map, p);
    public static void FillObjects<T>(Area map, Predicate<T> p, List<T> results) where T : Sprite => ObjectService.FillWithPredicate(map, p, results);

    /// <summary>
    /// This avoids allocating the intermediate List in GetObjects()
    /// </summary>
    public static void FillObjects(Area map, Predicate<Sprite> p, Get selections, List<Sprite> bucket)
    {
        if (map is null) return;

        switch (selections)
        {
            case Get.Aislings:
                ObjectService.FillSpriteBucket<Aisling>(map, p, bucket);
                break;

            case Get.Monsters:
                ObjectService.FillSpriteBucket<Monster>(map, p, bucket);
                break;

            case Get.Damageable:
                ObjectService.FillSpriteBucket<Monster>(map, p, bucket);
                ObjectService.FillSpriteBucket<Aisling>(map, p, bucket);
                break;

            case Get.Mundanes:
                ObjectService.FillSpriteBucket<Mundane>(map, p, bucket);
                break;

            case Get.UpdateNonPlayerSprites:
                ObjectService.FillSpriteBucket<Monster>(map, p, bucket);
                ObjectService.FillSpriteBucket<Mundane>(map, p, bucket);
                break;

            case Get.Movable:
                ObjectService.FillSpriteBucket<Aisling>(map, p, bucket);
                ObjectService.FillSpriteBucket<Monster>(map, p, bucket);
                ObjectService.FillSpriteBucket<Mundane>(map, p, bucket);
                break;

            case Get.Items:
                ObjectService.FillSpriteBucket<Item>(map, p, bucket);
                break;

            case Get.Money:
                ObjectService.FillSpriteBucket<Money>(map, p, bucket);
                break;

            case Get.AllButAislings:
                ObjectService.FillSpriteBucket<Monster>(map, p, bucket);
                ObjectService.FillSpriteBucket<Mundane>(map, p, bucket);
                ObjectService.FillSpriteBucket<Item>(map, p, bucket);
                ObjectService.FillSpriteBucket<Money>(map, p, bucket);
                break;

            case Get.All:
            default:
                ObjectService.FillSpriteBucket<Aisling>(map, p, bucket);
                ObjectService.FillSpriteBucket<Monster>(map, p, bucket);
                ObjectService.FillSpriteBucket<Mundane>(map, p, bucket);
                ObjectService.FillSpriteBucket<Money>(map, p, bucket);
                ObjectService.FillSpriteBucket<Item>(map, p, bucket);
                break;
        }
    }

    public static List<Sprite> GetObjects(Area map, Predicate<Sprite> p, Get selections)
    {
        List<Sprite> bucket = [];
        FillObjects(map, p, selections, bucket);

        return bucket;
    }
}