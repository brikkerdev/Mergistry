using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class MagnetGolemBehavior : IEnemyBehavior
    {
        public EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model)
        {
            int dist = AIHelper.Manhattan(enemy.Position, model.Player.Position);

            if (dist <= 1)
            {
                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = new List<Vector2Int> { model.Player.Position },
                    Damage      = 2
                };
            }
            else
            {
                return new EnemyIntent
                {
                    Type        = IntentType.Pull,
                    AttackCells = new List<Vector2Int>(),
                    Damage      = 2
                };
            }
        }
    }
}
