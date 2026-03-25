using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services.Bosses
{
    /// <summary>
    /// A6: Spider Queen boss (Floor 0).
    /// Phase 1 — move toward player, create 2 web (Water) zones each turn.
    /// Phase 2 — summon 2 spiders once, then retreat from player.
    /// Boss position = top-left of 2×2 footprint.
    /// </summary>
    public class SpiderQueenBehavior : IEnemyBehavior
    {
        private bool _hasSpawnedMinions;

        public EnemyIntent DetermineIntent(EnemyCombatModel boss, CombatModel model)
        {
            _hasSpawnedMinions = _hasSpawnedMinions ||
                                 model.Enemies.Exists(e => e.Type == EnemyType.Spider);

            if (model.CurrentBossPhase == BossPhase.Phase2 && !_hasSpawnedMinions)
            {
                _hasSpawnedMinions = true;
                var spawnPositions = FindSpawnPositions(boss.Position, model);
                return new EnemyIntent
                {
                    Type           = IntentType.SummonMinions,
                    SpawnPositions = spawnPositions,
                    MinionType     = EnemyType.Spider,
                    MinionHP       = 2,
                    Damage         = 0
                };
            }

            // Move: toward player in Phase 1, away in Phase 2
            var targetPos = model.CurrentBossPhase == BossPhase.Phase1
                ? AIHelper.StepBossToward(boss.Position, model)
                : AIHelper.StepBossAway(boss.Position, model);

            // Web zones (attack preview) = 2 random horizontal/vertical strips
            var webCells = PickWebCells(boss.Position, model);

            return new EnemyIntent
            {
                Type           = webCells.Count > 0 ? IntentType.Attack : IntentType.Move,
                TargetPosition = targetPos,
                AttackCells    = webCells,
                Damage         = 1
            };
        }

        private static List<Vector2Int> PickWebCells(Vector2Int bossPos, CombatModel model)
        {
            var cells = new List<Vector2Int>();

            // Row sweep (horizontal line at boss row+2)
            int row = Mathf.Clamp(bossPos.y + 2, 0, GridModel.Height - 1);
            for (int x = 0; x < GridModel.Width; x++)
            {
                var c = new Vector2Int(x, row);
                if (model.Grid.IsInBounds(c.x, c.y)) cells.Add(c);
            }

            // Column sweep (vertical line at boss col-2)
            int col = Mathf.Clamp(bossPos.x - 2, 0, GridModel.Width - 1);
            for (int y = 0; y < GridModel.Height; y++)
            {
                var c = new Vector2Int(col, y);
                if (model.Grid.IsInBounds(c.x, c.y) && !cells.Contains(c)) cells.Add(c);
            }

            return cells;
        }

        private static List<Vector2Int> FindSpawnPositions(Vector2Int bossPos, CombatModel model)
        {
            var result  = new List<Vector2Int>();
            var offsets = new[]
            {
                new Vector2Int(-1,  0), new Vector2Int(0, -1),
                new Vector2Int(-1, -1), new Vector2Int(2,  0),
                new Vector2Int( 0,  2), new Vector2Int(2,  2),
            };

            foreach (var off in offsets)
            {
                if (result.Count >= 2) break;
                var pos = bossPos + off;
                if (!model.Grid.IsInBounds(pos.x, pos.y)) continue;
                if (pos == model.Player.Position) continue;
                if (model.Enemies.Exists(e => !e.IsDead && e.Position == pos)) continue;
                result.Add(pos);
            }
            return result;
        }
    }
}
