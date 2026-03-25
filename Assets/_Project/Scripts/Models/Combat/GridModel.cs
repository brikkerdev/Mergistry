using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Mergistry.Models.Combat
{
    public class GridModel
    {
        public const int Width  = 5;
        public const int Height = 5;

        // A3: active zones on the field
        public List<ZoneInstance> Zones { get; } = new List<ZoneInstance>();

        public bool IsInBounds(int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height;

        /// <summary>
        /// Adds or refreshes a zone at a position.
        /// If the same type already exists there, the duration is reset to the new value.
        /// </summary>
        public void AddZone(ZoneType type, Vector2Int pos, int turns)
        {
            var existing = Zones.FirstOrDefault(z => z.Type == type && z.Position == pos);
            if (existing != null)
                existing.TurnsRemaining = turns;
            else
                Zones.Add(new ZoneInstance(type, pos, turns));
        }

        /// <summary>Returns all zones currently active at the given position.</summary>
        public List<ZoneInstance> GetZonesAt(Vector2Int pos) =>
            Zones.Where(z => z.Position == pos).ToList();
    }
}
