using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services.Bosses
{
    /// <summary>
    /// A6: Renegade Alchemist boss (Floor 2).
    /// Phase 1 — copies last player potion and casts it back, or moves toward player.
    ///           Teleports are disabled if all cauldrons destroyed.
    /// Phase 2 — teleports near player every turn and attacks.
    /// </summary>
    public class RenegadeAlchemistBehavior : IEnemyBehavior
    {
        private int _turnCounter;

        public EnemyIntent DetermineIntent(EnemyCombatModel boss, CombatModel model)
        {
            _turnCounter++;

            bool canTeleport = !model.CauldronsDestroyed;

            if (model.CurrentBossPhase == BossPhase.Phase2)
            {
                // Phase 2: teleport (if cauldrons alive) + attack
                if (canTeleport)
                {
                    var teleportTarget = FindTeleportTarget(boss, model);
                    return new EnemyIntent
                    {
                        Type           = IntentType.Teleport,
                        TargetPosition = teleportTarget,
                        AttackCells    = new List<Vector2Int> { model.Player.Position },
                        Damage         = 3
                    };
                }

                // No teleport — just attack from current position
                return AttackIntent(boss.Position, model, damage: 3);
            }

            // Phase 1: copy potion every other turn, teleport on others
            if (_turnCounter % 2 == 0 && model.HasLastThrownPotion && canTeleport)
            {
                var teleportTarget = FindTeleportTarget(boss, model);
                return new EnemyIntent
                {
                    Type           = IntentType.Teleport,
                    TargetPosition = teleportTarget,
                    AttackCells    = new List<Vector2Int> { model.Player.Position },
                    Damage         = 2
                };
            }

            // Copy potion attack (if player has thrown one)
            if (model.HasLastThrownPotion)
                return AttackIntent(boss.Position, model, damage: model.LastThrownPotionLevel + 1);

            // Default: move toward player
            var moveTarget = AIHelper.StepBossToward(boss.Position, model);
            return new EnemyIntent
            {
                Type           = IntentType.Move,
                TargetPosition = moveTarget,
                Damage         = 0
            };
        }

        private static Vector2Int FindTeleportTarget(EnemyCombatModel boss, CombatModel model)
        {
            // Find adjacent-ish empty spot near player
            var offsets = new[]
            {
                new Vector2Int(-2, 0), new Vector2Int(0, -2),
                new Vector2Int(-2,-2), new Vector2Int(2, 0),
                new Vector2Int( 0, 2), new Vector2Int(2, 2),
                new Vector2Int(-2, 2), new Vector2Int(2,-2),
            };

            foreach (var off in offsets)
            {
                var pos = model.Player.Position + off;
                if (model.Grid.IsInBounds(pos.x, pos.y) &&
                    model.Grid.IsInBounds(pos.x + 1, pos.y + 1) &&
                    pos != model.Player.Position)
                    return pos;
            }

            return boss.Position;
        }

        private static EnemyIntent AttackIntent(Vector2Int bossPos, CombatModel model, int damage)
        {
            var cells = new List<Vector2Int> { model.Player.Position };
            return new EnemyIntent
            {
                Type        = IntentType.Attack,
                AttackCells = cells,
                Damage      = damage
            };
        }
    }
}
