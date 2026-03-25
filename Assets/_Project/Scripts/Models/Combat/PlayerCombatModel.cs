using System.Collections.Generic;
using UnityEngine;

namespace Mergistry.Models.Combat
{
    public class PlayerCombatModel
    {
        public Vector2Int Position { get; set; }
        public bool       HasMoved { get; set; }
        public bool       HasActed { get; set; }
        public int        HP       { get; set; } = 5;
        public int        MaxHP    { get; set; } = 5;

        // A3: status effects
        public List<StatusEffect> StatusEffects { get; } = new List<StatusEffect>();

        public bool HasStatus(StatusEffectType t) =>
            StatusEffects.Exists(s => s.Type == t && s.Duration > 0);
    }
}
