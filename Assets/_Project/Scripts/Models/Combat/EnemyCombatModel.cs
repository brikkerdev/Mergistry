using UnityEngine;

namespace Mergistry.Models.Combat
{
    public class EnemyCombatModel
    {
        public int         EntityId { get; set; }
        public EnemyType   Type     { get; set; }
        public Vector2Int  Position { get; set; }
        public int         HP       { get; set; }
        public int         MaxHP    { get; set; }
        public EnemyIntent Intent   { get; set; }
        public bool        IsDead   => HP <= 0;

        public EnemyCombatModel(int id, EnemyType type, Vector2Int pos, int hp)
        {
            EntityId = id;
            Type     = type;
            Position = pos;
            HP       = hp;
            MaxHP    = hp;
        }
    }
}
