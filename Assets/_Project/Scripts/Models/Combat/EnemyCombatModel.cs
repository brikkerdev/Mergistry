using System.Collections.Generic;
using Mergistry.Data;
using UnityEngine;

namespace Mergistry.Models.Combat
{
    public class EnemyCombatModel
    {
        public int         EntityId    { get; set; }
        public EnemyType   Type        { get; set; }
        public Vector2Int  Position    { get; set; }
        public int         HP          { get; set; }
        public int         MaxHP       { get; set; }
        public EnemyIntent Intent      { get; set; }
        public bool        IsDead      => HP <= 0;

        // A2: armor for ArmoredBeetle
        public int         ArmorPoints { get; set; }

        // A2: countdown timer for MushroomBomb (starts at 3)
        public int         BombTimer   { get; set; }

        // A3: status effects (Slow, Stun, Poison)
        public List<StatusEffect> StatusEffects { get; } = new List<StatusEffect>();

        // A3: MirrorSlime — copied potion from last player throw
        public bool        HasCopiedPotion   { get; set; }
        public PotionType  CopiedPotionType  { get; set; }
        public int         CopiedPotionLevel { get; set; }

        public EnemyCombatModel(int id, EnemyType type, Vector2Int pos, int hp,
                                int armorPoints = 0, int bombTimer = 3)
        {
            EntityId    = id;
            Type        = type;
            Position    = pos;
            HP          = hp;
            MaxHP       = hp;
            ArmorPoints = armorPoints;
            BombTimer   = bombTimer;
        }

        // A3: status helpers
        public bool HasStatus(StatusEffectType t) =>
            StatusEffects.Exists(s => s.Type == t && s.Duration > 0);
    }
}
