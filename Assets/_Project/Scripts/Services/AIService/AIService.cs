using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Determines and executes enemy intents for each combat turn.
    /// </summary>
    public class AIService
    {
        private readonly DamageService _damageService;

        public AIService(DamageService damageService)
        {
            _damageService = damageService;
        }

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Calculates intent for every living enemy based on the current model state.</summary>
        public void DetermineIntents(CombatModel model)
        {
            foreach (var enemy in model.Enemies)
            {
                if (enemy.IsDead) continue;
                enemy.Intent = enemy.Type switch
                {
                    EnemyType.Skeleton => SkeletonIntent(enemy, model),
                    EnemyType.Spider   => SpiderIntent(enemy, model),
                    _                  => null
                };
            }
        }

        /// <summary>Executes each living enemy's stored intent, applying movement and damage.</summary>
        public void ExecuteIntents(CombatModel model)
        {
            foreach (var enemy in model.Enemies)
            {
                if (enemy.IsDead || enemy.Intent == null) continue;
                ExecuteEnemyIntent(enemy, model);
            }
        }

        // ── Skeleton ────────────────────────────────────────────────────────────

        private EnemyIntent SkeletonIntent(EnemyCombatModel enemy, CombatModel model)
        {
            var playerPos = model.Player.Position;
            int dist      = Manhattan(enemy.Position, playerPos);

            if (dist <= 1)
            {
                // Adjacent to player — attack
                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = new List<Vector2Int> { playerPos },
                    Damage      = 2
                };
            }
            else
            {
                // Move one step toward player
                var target = StepToward(enemy, model);
                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = target,
                    AttackCells    = new List<Vector2Int>()
                };
            }
        }

        // ── Spider ──────────────────────────────────────────────────────────────

        private EnemyIntent SpiderIntent(EnemyCombatModel enemy, CombatModel model)
        {
            // Spider always attacks along a line directed toward the player (range 3)
            var dir         = LineDirection(enemy.Position, model.Player.Position);
            var attackCells = LineCells(enemy.Position, dir, 3, model.Grid);

            return new EnemyIntent
            {
                Type        = IntentType.Attack,
                AttackCells = attackCells,
                Damage      = 1
            };
        }

        // ── Execution ───────────────────────────────────────────────────────────

        private void ExecuteEnemyIntent(EnemyCombatModel enemy, CombatModel model)
        {
            var intent = enemy.Intent;

            if (intent.Type == IntentType.Move)
            {
                var target = intent.TargetPosition;

                // Re-check: player or another enemy may have moved here since DetermineIntents
                if (target == model.Player.Position) return;
                foreach (var other in model.Enemies)
                    if (!other.IsDead && other.EntityId != enemy.EntityId && other.Position == target) return;

                enemy.Position = target;
                Debug.Log($"[AIService] {enemy.Type}({enemy.EntityId}) moved to {enemy.Position}");
            }
            else if (intent.Type == IntentType.Attack)
            {
                bool hit = intent.AttackCells.Contains(model.Player.Position);
                if (hit)
                    _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                Debug.Log($"[AIService] {enemy.Type}({enemy.EntityId}) attacked — player hit={hit}");
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Moves one step toward the target (horizontal if |dx|≥|dy|, else vertical).
        /// Falls back to current position if blocked.
        /// </summary>
        private static Vector2Int StepToward(EnemyCombatModel mover, CombatModel model)
        {
            var from   = mover.Position;
            var to     = model.Player.Position;
            int dx     = to.x - from.x;
            int dy     = to.y - from.y;

            var step = Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dy > 0 ? 1 : -1);

            var candidate = from + step;

            if (!model.Grid.IsInBounds(candidate.x, candidate.y)) return from;
            if (candidate == model.Player.Position)                return from; // attack instead
            foreach (var other in model.Enemies)
                if (!other.IsDead && other.EntityId != mover.EntityId && other.Position == candidate)
                    return from; // blocked by another enemy

            return candidate;
        }

        /// <summary>
        /// Returns the primary axis direction from 'from' toward 'to'.
        /// Prefers horizontal when |dx| >= |dy|; falls back to left if equal and both zero.
        /// </summary>
        private static Vector2Int LineDirection(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;

            if (dx == 0 && dy == 0) return new Vector2Int(-1, 0); // fallback

            return Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dy > 0 ? 1 : -1);
        }

        /// <summary>Returns up to 'range' cells starting 1 step from 'origin' in 'dir'.</summary>
        private static List<Vector2Int> LineCells(Vector2Int origin, Vector2Int dir,
                                                   int range, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int i = 1; i <= range; i++)
            {
                var cell = origin + dir * i;
                if (!grid.IsInBounds(cell.x, cell.y)) break;
                cells.Add(cell);
            }
            return cells;
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
