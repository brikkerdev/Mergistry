using System.Collections.Generic;
using System.Linq;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class NecromancerBehavior : IEnemyBehavior
    {
        public EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model)
        {
            // Find first revivable enemy in graveyard
            var target = model.Graveyard.FirstOrDefault();
            if (target != null)
            {
                // Find a valid position adjacent to the necromancer
                var revivePos = AIHelper.FindFreeAdjacent(enemy.Position, model);
                if (revivePos.HasValue)
                {
                    return new EnemyIntent
                    {
                        Type           = IntentType.Revive,
                        TargetPosition = revivePos.Value,
                        AttackCells    = new List<Vector2Int>(),
                        ReviveEntityId = target.EntityId
                    };
                }
            }

            // No revive possible — flee from player
            var fleePos = enemy.HasStatus(StatusEffectType.Slow)
                ? enemy.Position
                : AIHelper.StepAway(enemy, model);

            return new EnemyIntent
            {
                Type           = IntentType.Move,
                TargetPosition = fleePos,
                AttackCells    = new List<Vector2Int>()
            };
        }
    }
}
