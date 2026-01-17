using Darkages.Network.Server;
using Darkages.Sprites;
using Darkages.Sprites.Entity;

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
        /// <summary>
        /// Materializes a snapshot of the dictionary's values
        /// </summary>
        private static List<T> SnapshotValues<T>(this IDictionary<long, T>? dict)
        {
            if (dict is null || dict.Count == 0)
                return [];

            // Materialize to prevent mutation issues
            return dict.Values.ToList();
        }

        /// <summary>
        /// Normalizes a list to never be null or empty
        /// </summary>
        private static List<Sprite> Normalize(List<Sprite>? list) => list is null || list.Count == 0 ? [] : list;

        /// <summary>
        /// Gets all sprites at the specified coordinates.
        /// </summary>
        public static List<Sprite> GetSpritesAt(this Sprite self, int x, int y)
        {
            var map = self.Map;
            if (map is null) return [];

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && (int)i.Pos.X == x && (int)i.Pos.Y == y,
                ObjectManager.Get.All);

            return Normalize(list);
        }

        /// <summary>
        /// Gets all damageable sprites at the specified coordinates.
        /// </summary>
        public static List<Sprite> GetDamageableAt(this Sprite self, int x, int y)
        {
            var map = self.Map;
            if (map is null) return [];

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && (int)i.Pos.X == x && (int)i.Pos.Y == y,
                ObjectManager.Get.Damageable);

            return Normalize(list);
        }

        /// <summary>
        /// Gets all damageable sprites nearby
        /// </summary>
        public static List<Sprite> DamageableNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return [];

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && i.WithinRangeOf(self, range),
                ObjectManager.Get.Damageable);

            return Normalize(list);
        }

        /// <summary>
        /// Gets all damageable sprites within range of target sprite
        /// </summary>
        public static List<Sprite> DamageableWithinRangeSnapshot(this Sprite self, Sprite target, int range)
        {
            var map = self.Map;
            if (map is null || target is null) return [];

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && i.WithinRangeOf(target, range),
                ObjectManager.Get.Damageable);

            if (list is null || list.Count == 0)
                return [];

            // Filter after snapshot to avoid mutation issues
            return list.Where(s => s.Alive).ToList();
        }

        /// <summary>
        /// Gets all Aislings nearby
        /// </summary>
        public static List<Aisling> AislingsNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return [];

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            var dict = ObjectManager.GetObjects<Aisling>(
                map,
                i => i is not null && i.WithinRangeOf(self, range));

            return dict.SnapshotValues();
        }

        /// <summary>
        /// Gets all Aislings within earshot (14 tiles)
        /// </summary>
        public static List<Aisling> AislingsEarShotNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return [];

            var dict = ObjectManager.GetObjects<Aisling>(
                map,
                i => i is not null && i.WithinRangeOf(self, 14));

            return dict.SnapshotValues();
        }

        /// <summary>
        /// Gets all Aislings on the map
        /// </summary>
        public static List<Aisling> AislingsOnMapSnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return [];

            var dict = ObjectManager.GetObjects<Aisling>(
                map,
                i => i is not null && i.Map == map);

            return dict.SnapshotValues();
        }

        /// <summary>
        /// Gets all Monsters nearby
        /// </summary>
        public static List<Monster> MonstersNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return [];

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            var dict = ObjectManager.GetObjects<Monster>(
                map,
                i => i is not null && i.WithinRangeOf(self, range));

            return dict.SnapshotValues();
        }

        /// <summary>
        /// Gets all Monsters on the map
        /// </summary>
        public static List<Monster> MonstersOnMapSnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return [];

            var dict = ObjectManager.GetObjects<Monster>(
                map,
                i => i is not null);

            return dict.SnapshotValues();
        }

        /// <summary>
        /// Gets all Mundanes nearby
        /// </summary>
        public static List<Mundane> MundanesNearbySnapshot(this Sprite self)
        {
            var map = self.Map;
            if (map is null) return [];

            var range = ServerSetup.Instance.Config.WithinRangeProximity;

            var dict = ObjectManager.GetObjects<Mundane>(
                map,
                i => i is not null && i.WithinRangeOf(self, range));

            return dict.SnapshotValues();
        }

        /// <summary>
        /// Gets all sprites that are "movable" on target location
        /// </summary>
        public static List<Sprite> GetMovableAt(this Sprite self, int x, int y)
        {
            var map = self.Map;
            if (map is null) return [];

            var list = ObjectManager.GetObjects(
                map,
                i => i is not null && (int)i.Pos.X == x && (int)i.Pos.Y == y,
                ObjectManager.Get.Movable);

            return Normalize(list);
        }
    }
}
