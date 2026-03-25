using System.Collections.Generic;
using UnityEngine;

namespace Mergistry.Models.Combat
{
    public class EnemyIntent
    {
        public IntentType     Type         { get; set; }
        public Vector2Int     TargetPosition { get; set; } // destination for Move intent
        public List<Vector2Int> AttackCells { get; set; } = new List<Vector2Int>();
        public int            Damage       { get; set; }
    }
}
