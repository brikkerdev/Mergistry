using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Determines and executes enemy intents for each combat turn.
    /// A2: MushroomBomb (countdown/explode), MagnetGolem (pull), ArmoredBeetle (armored skeleton).
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
                    EnemyType.Skeleton      => SkeletonIntent(enemy, model),
                    EnemyType.Spider        => SpiderIntent(enemy, model),
                    EnemyType.MushroomBomb  => MushroomBombIntent(enemy, model),
                    EnemyType.MagnetGolem   => MagnetGolemIntent(enemy, model),
                    EnemyType.ArmoredBeetle => ArmoredBeetleIntent(enemy, model),
                    _                       => null
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
                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = new List<Vector2Int> { playerPos },
                    Damage      = 2
                };
            }
            else
            {
                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = StepToward(enemy, model),
                    AttackCells    = new List<Vector2Int>()
                };
            }
        }

        // ── Spider ──────────────────────────────────────────────────────────────

        private EnemyIntent SpiderIntent(EnemyCombatModel enemy, CombatModel model)
        {
            var dir         = LineDirection(enemy.Position, model.Player.Position);
            var attackCells = LineCells(enemy.Position, dir, 3, model.Grid);

            return new EnemyIntent
            {
                Type        = IntentType.Attack,
                AttackCells = attackCells,
                Damage      = 1
            };
        }

        // ── MushroomBomb ────────────────────────────────────────────────────────

        private EnemyIntent MushroomBombIntent(EnemyCombatModel enemy, CombatModel model)
        {
            if (enemy.BombTimer > 0)
            {
                // Show current timer value, then decrement for next turn
                int display = enemy.BombTimer;
                enemy.BombTimer--;
                return new EnemyIntent
                {
                    Type           = IntentType.Countdown,
                    CountdownValue = display,
                    AttackCells    = new List<Vector2Int>()
                };
            }
            else
            {
                // Timer hit 0 — explode next execution
                return new EnemyIntent
                {
                    Type        = IntentType.Explode,
                    AttackCells = Box3x3(enemy.Position, model.Grid),
                    Damage      = 5
                };
            }
        }

        // ── MagnetGolem ─────────────────────────────────────────────────────────

        private EnemyIntent MagnetGolemIntent(EnemyCombatModel enemy, CombatModel model)
        {
            int dist = Manhattan(enemy.Position, model.Player.Position);

            if (dist <= 1)
            {
                // Adjacent — contact damage
                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = new List<Vector2Int> { model.Player.Position },
                    Damage      = 2
                };
            }
            else
            {
                // Pull player 1 step toward golem
                return new EnemyIntent
                {
                    Type        = IntentType.Pull,
                    AttackCells = new List<Vector2Int>(),
                    Damage      = 2
                };
            }
        }

        // ── ArmoredBeetle ───────────────────────────────────────────────────────

        private EnemyIntent ArmoredBeetleIntent(EnemyCombatModel enemy, CombatModel model)
        {
            // Same logic as Skeleton but has armor
            var playerPos = model.Player.Position;
            int dist      = Manhattan(enemy.Position, playerPos);

            if (dist <= 1)
            {
                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = new List<Vector2Int> { playerPos },
                    Damage      = 2
                };
            }
            else
            {
                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = StepToward(enemy, model),
                    AttackCells    = new List<Vector2Int>()
                };
            }
        }

        // ── Execution ───────────────────────────────────────────────────────────

        private void ExecuteEnemyIntent(EnemyCombatModel enemy, CombatModel model)
        {
            var intent = enemy.Intent;

            switch (intent.Type)
            {
                case IntentType.Move:
                {
                    var target = intent.TargetPosition;
                    if (target == model.Player.Position) return;
                    foreach (var other in model.Enemies)
                        if (!other.IsDead && other.EntityId != enemy.EntityId && other.Position == target) return;

                    enemy.Position = target;
                    Debug.Log($"[AIService] {enemy.Type}({enemy.EntityId}) moved to {enemy.Position}");
                    break;
                }
                case IntentType.Attack:
                {
                    bool hit = intent.AttackCells.Contains(model.Player.Position);
                    if (hit)
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);
                    Debug.Log($"[AIService] {enemy.Type}({enemy.EntityId}) attacked — player hit={hit}");
                    break;
                }
                case IntentType.Countdown:
                    // Countdown tick handled in DetermineIntents; no execution action
                    Debug.Log($"[AIService] MushroomBomb({enemy.EntityId}) countdown...");
                    break;

                case IntentType.Explode:
                {
                    // Damage all enemies in AoE (including self doesn't count, but other bombs can be hit)
                    foreach (var other in model.Enemies)
                    {
                        if (other.IsDead || other.EntityId == enemy.EntityId) continue;
                        if (intent.AttackCells.Contains(other.Position))
                            _damageService.ApplyDamage(other, intent.Damage);
                    }
                    // Damage player
                    if (intent.AttackCells.Contains(model.Player.Position))
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                    // Kill the bomb itself
                    enemy.HP = 0;

                    EventBus.Publish(new BombExplodedEvent
                    {
                        EntityId      = enemy.EntityId,
                        AffectedCells = intent.AttackCells
                    });

                    Debug.Log($"[AIService] MushroomBomb({enemy.EntityId}) EXPLODED!");
                    break;
                }
                case IntentType.Pull:
                {
                    // Move player 1 step toward golem
                    var pulled = StepPlayerToward(model.Player.Position, enemy.Position, model);
                    model.Player.Position = pulled;

                    // If now adjacent → contact damage
                    if (Manhattan(pulled, enemy.Position) <= 1)
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                    Debug.Log($"[AIService] MagnetGolem({enemy.EntityId}) pulled player to {pulled}");
                    break;
                }
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

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
            if (candidate == model.Player.Position)                return from;
            foreach (var other in model.Enemies)
                if (!other.IsDead && other.EntityId != mover.EntityId && other.Position == candidate)
                    return from;

            return candidate;
        }

        /// <summary>Moves player 1 step toward the golem. Stops if would land on golem.</summary>
        private static Vector2Int StepPlayerToward(Vector2Int playerPos, Vector2Int golemPos, CombatModel model)
        {
            int dx = golemPos.x - playerPos.x;
            int dy = golemPos.y - playerPos.y;

            var step = Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dy > 0 ? 1 : -1);

            var candidate = playerPos + step;

            if (!model.Grid.IsInBounds(candidate.x, candidate.y)) return playerPos;
            // Don't land on the golem's cell
            foreach (var enemy in model.Enemies)
                if (!enemy.IsDead && enemy.Position == candidate) return playerPos;

            return candidate;
        }

        private static Vector2Int LineDirection(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            if (dx == 0 && dy == 0) return new Vector2Int(-1, 0);
            return Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dy > 0 ? 1 : -1);
        }

        private static List<Vector2Int> LineCells(Vector2Int origin, Vector2Int dir, int range, GridModel grid)
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

        private static List<Vector2Int> Box3x3(Vector2Int center, GridModel grid)
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

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
