using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class ArmoredBeetleBehavior : IEnemyBehavior
    {
        public EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model)
        {
            var playerPos = model.Player.Position;
            int dist      = AIHelper.Manhattan(enemy.Position, playerPos);

            if (dist <= 1)
            {
                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = new List<Vector2Int> { playerPos },
                    Damage      = 3
                };
            }
            else
            {
                var target = enemy.HasStatus(StatusEffectType.Slow)
                    ? enemy.Position
                    : AIHelper.StepToward(enemy, model);

                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = target,
                    AttackCells    = new List<Vector2Int>()
                };
            }
        }
    }
}
