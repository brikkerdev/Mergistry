using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services.Bosses
{
    /// <summary>
    /// A6: Iron Golem boss (Floor 1).
    /// Phase 1 — alternates between 2×2 area slam near player and magnetic pull.
    /// Phase 2 — area slam every turn (pillars already broken by BossState on phase change).
    /// </summary>
    public class IronGolemBehavior : IEnemyBehavior
    {
        private int _turnCounter;

        public EnemyIntent DetermineIntent(EnemyCombatModel boss, CombatModel model)
        {
            _turnCounter++;

            int distToPlayer = AIHelper.Manhattan(boss.Position, model.Player.Position);

            // Phase 2: always slam
            bool doSlam = model.CurrentBossPhase == BossPhase.Phase2 || (_turnCounter % 2 == 0);

            if (doSlam || distToPlayer <= 2)
            {
                // 2×2 area slam centred near player
                var slamCells = Box2x2Near(model.Player.Position, model.Grid);
                return new EnemyIntent
                {
                    Type        = IntentType.AreaAttack,
                    AttackCells = slamCells,
                    Damage      = model.CurrentBossPhase == BossPhase.Phase2 ? 5 : 3
                };
            }

            // Move toward player OR pull
            if (distToPlayer > 3)
            {
                var targetPos = AIHelper.StepBossToward(boss.Position, model);
                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = targetPos,
                    Damage         = 0
                };
            }

            // Pull player closer
            var pullDest = AIHelper.StepPlayerToward(model.Player.Position, boss.Position, model);
            return new EnemyIntent
            {
                Type           = IntentType.Pull,
                TargetPosition = pullDest,
                AttackCells    = new List<Vector2Int> { pullDest },
                Damage         = 2
            };
        }

        private static List<Vector2Int> Box2x2Near(Vector2Int center, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dy);
                if (grid.IsInBounds(c.x, c.y)) cells.Add(c);
            }
            return cells;
        }
    }
}
