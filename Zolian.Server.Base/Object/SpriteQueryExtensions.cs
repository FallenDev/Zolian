using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Sprites.Entity;
using Darkages.Types;

using ServiceStack;

namespace Darkages.Object
{
    /// <summary>
    /// Snapshot query helpers for world objects.
    /// Contract:
    ///  - Never returns null (empty list instead)
    ///  - Always returns a materialized snapshot (safe to enumerate if world mutates mid-loop)
    /// </summary>
    public static class SpriteQueryExtensions
    {
        private static readonly List<Sprite> EmptySprites = [];
        private static readonly List<Aisling> EmptyAislings = [];
        private static readonly List<Monster> EmptyMonsters = [];
        private static readonly List<Mundane> EmptyMundanes = [];
        private static readonly List<Item> EmptyItems = [];
        private static readonly List<Damageable> EmptyDamageables = [];
        private static readonly List<Movable> EmptyMovables = [];

        /// <summary>
        /// Normalizes a list to never be null.
        /// Uses a shared empty list instance for the empty case.
        /// </summary>
        private static List<Sprite> NormalizeSprites(List<Sprite>? list) => list is null || list.Count == 0 ? EmptySprites : list;
        private static List<T> Normalize<T>(List<T>? list, List<T> empty) => list is null || list.Count == 0 ? empty : list;

        #region Sprites in location

        /// <summary>
        /// Gets all sprites at the specified coordinates.
        /// </summary>
        public static List<Sprite> GetSpritesAt(this Sprite self, int x, int y)
        {
            var map = self.Map;
            if (map is null) return EmptySprites;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && (int)i.Pos.X == x && (int)i.Pos.Y == y,
                ObjectManager.Get.All);

            return NormalizeSprites(list);
        }

        /// <summary>
        /// Gets all sprites at the specified coordinates.
        /// </summary>
        public static List<Sprite> GetItemsAt(this Sprite self, int x, int y)
        {
            var map = self.Map;
            if (map is null) return EmptySprites;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && (int)i.Pos.X == x && (int)i.Pos.Y == y,
                ObjectManager.Get.Items);

            return NormalizeSprites(list);
        }

        /// <summary>
        /// Gets all damageable sprites at the specified coordinates.
        /// </summary>
        public static List<Sprite> GetDamageableAt(this Sprite self, int x, int y)
        {
            var map = self.Map;
            if (map is null) return EmptySprites;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && (int)i.Pos.X == x && (int)i.Pos.Y == y,
                ObjectManager.Get.Damageable);

            return NormalizeSprites(list);
        }

        /// <summary>
        /// Gets all sprites that are "movable" on target location
        /// </summary>
        public static List<Sprite> GetMovableAt(this Sprite self, int x, int y)
        {
            var map = self.Map;
            if (map is null) return EmptySprites;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && (int)i.Pos.X == x && (int)i.Pos.Y == y,
                ObjectManager.Get.Movable);

            return NormalizeSprites(list);
        }

        #endregion

        #region Sprites within range

        /// <summary>
        /// Gets all damageable sprites within range of target sprite
        /// </summary>
        public static List<Sprite> DamageableWithinRangeSnapshot(this Sprite self, Sprite target, int range)
        {
            var map = self.Map;
            if (map is null || target is null) return EmptySprites;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && i.WithinRangeOf(target, range),
                ObjectManager.Get.Damageable);

            if (list is null || list.Count == 0)
                return EmptySprites;

            // Filter in-place to avoid a second allocation.
            // (This list is already a snapshot, so it's safe to mutate the list structure.)
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].Alive)
                    list.RemoveAt(i);
            }

            return list.Count == 0 ? EmptySprites : list;
        }

        /// <summary>
        /// Gets all aisling sprites within range of target sprite
        /// </summary>
        public static List<Sprite> AislingsWithinRangeSnapshot(this Sprite self, Sprite target, int range)
        {
            var map = self.Map;
            if (map is null || target is null) return EmptySprites;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && i.WithinRangeOf(target, range),
                ObjectManager.Get.Aislings);

            if (list is null || list.Count == 0)
                return EmptySprites;

            // Filter in-place to avoid a second allocation.
            // (This list is already a snapshot, so it's safe to mutate the list structure.)
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i].Alive)
                    list.RemoveAt(i);
            }

            return list.Count == 0 ? EmptySprites : list;
        }

        /// <summary>
        /// Gets all damageable sprites nearby
        /// </summary>
        public static List<Sprite> DamageableNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptySprites;

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && i.WithinRangeOf(self, range),
                ObjectManager.Get.Damageable);

            return NormalizeSprites(list);
        }

        /// <summary>
        /// Gets all Aislings nearby
        /// </summary>
        public static List<Aisling> AislingsNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptyAislings;

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            List<Aisling> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null && i.WithinRangeOf(self, range),
                results);

            return Normalize(results, EmptyAislings);
        }

        /// <summary>
        /// Gets all Aislings within earshot (14 tiles)
        /// </summary>
        public static List<Aisling> AislingsEarShotNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptyAislings;

            List<Aisling> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null && i.WithinRangeOf(self, 14),
                results);

            return Normalize(results, EmptyAislings);
        }

        /// <summary>
        /// Gets all Monsters nearby
        /// </summary>
        public static List<Monster> MonstersNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptyMonsters;

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            List<Monster> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null && i.WithinRangeOf(self, range),
                results);

            return Normalize(results, EmptyMonsters);
        }

        /// <summary>
        /// Gets all Mundanes nearby
        /// </summary>
        public static List<Mundane> MundanesNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptyMundanes;

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            List<Mundane> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null && i.WithinRangeOf(self, range),
                results);

            return Normalize(results, EmptyMundanes);
        }

        /// <summary>
        /// Gets all Movable nearby
        /// </summary>
        public static List<Sprite> MovableNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptySprites;

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            // Compound assignment, cannot use FillObjects
            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && i.WithinRangeOf(self, range),
                ObjectManager.Get.Movable);

            return NormalizeSprites(list);
        }

        #endregion

        #region Sprites on map

        /// <summary>
        /// Gets all Aislings on the map
        /// </summary>
        public static List<Aisling> AislingsOnMapSnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptyAislings;

            List<Aisling> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null && i.Map == map,
                results);

            return Normalize(results, EmptyAislings);
        }

        /// <summary>
        /// Gets all Monsters on the map
        /// </summary>
        public static List<Monster> MonstersOnMapSnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return EmptyMonsters;

            List<Monster> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null,
                results);

            return Normalize(results, EmptyMonsters);
        }

        /// <summary>
        /// Gets all Monsters on the map
        /// </summary>
        public static List<Monster> MonstersOnMapSnapshot(Area map)
        {
            if (map is null) return EmptyMonsters;

            List<Monster> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null,
                results);

            return Normalize(results, EmptyMonsters);
        }

        /// <summary>
        /// Gets all Mundanes on the map
        /// </summary>
        public static List<Mundane> MundanesOnMapSnapshot(Area map)
        {
            if (map is null) return EmptyMundanes;

            List<Mundane> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null,
                results);

            return Normalize(results, EmptyMundanes);
        }

        /// <summary>
        /// Gets all Items on the map
        /// </summary>
        public static List<Item> ItemsOnMapSnapshot(Area map)
        {
            if (map is null) return EmptyItems;

            List<Item> results = [];
            ObjectManager.FillObjects(
                map,
                i => i is not null,
                results);

            return Normalize(results, EmptyItems);
        }

        /// <summary>
        /// Gets all Damageables on the map
        /// </summary>
        public static List<Sprite> DamageableOnMapSnapshot(Area map)
        {
            if (map is null) return EmptySprites;

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            // Compound assignment, cannot use FillObjects
            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && i.Map.ID == map.ID,
                ObjectManager.Get.Damageable);

            return NormalizeSprites(list);
        }

        #endregion
    }
}