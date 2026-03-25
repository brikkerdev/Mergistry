using UnityEngine;

namespace Mergistry.Models.Combat
{
    public class ZoneInstance
    {
        public ZoneType   Type           { get; set; }
        public Vector2Int Position       { get; set; }
        public int        TurnsRemaining { get; set; }

        public ZoneInstance(ZoneType type, Vector2Int position, int turns)
        {
            Type           = type;
            Position       = position;
            TurnsRemaining = turns;
        }
    }
}
