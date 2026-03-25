using System.Collections.Generic;
using UnityEngine;

namespace Mergistry.Models.Combat
{
    public class EnemyIntent
    {
        public IntentType     Type         { get; set; }
        public Vector2Int     TargetPosition { get; set; } // destination for Move intent
        public List<Vector2Int> AttackCells { get; set; } = new List<Vector2Int>();
        public int            Damage         { get; set; }
        public int            CountdownValue { get; set; } // for Countdown intent display
        public int            ReviveEntityId { get; set; } = -1; // for Revive intent

        // A6: SummonMinions intent
        public List<Vector2Int> SpawnPositions { get; set; } = new List<Vector2Int>();
        public EnemyType        MinionType     { get; set; }
        public int              MinionHP       { get; set; }
    }
}
