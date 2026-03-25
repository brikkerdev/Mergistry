using System.Collections.Generic;
using System.Linq;
using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Determines and executes enemy intents for each combat turn.
    /// A2: MushroomBomb (countdown/explode), MagnetGolem (pull), ArmoredBeetle (armored skeleton).
    /// A3: MirrorSlime (copy last potion), Phantom (teleport + attack), Necromancer (revive + flee).
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

                // A3: Stunned enemies skip their turn
                if (enemy.HasStatus(StatusEffectType.Stun))
                {
                    enemy.Intent = null;
                    continue;
                }

                enemy.Intent = enemy.Type switch
                {
                    EnemyType.Skeleton      => SkeletonIntent(enemy, model),
                    EnemyType.Spider        => SpiderIntent(enemy, model),
                    EnemyType.MushroomBomb  => MushroomBombIntent(enemy, model),
                    EnemyType.MagnetGolem   => MagnetGolemIntent(enemy, model),
                    EnemyType.ArmoredBeetle => ArmoredBeetleIntent(enemy, model),
                    EnemyType.MirrorSlime   => MirrorSlimeIntent(enemy, model),
                    EnemyType.Phantom       => PhantomIntent(enemy, model),
                    EnemyType.Necromancer   => NecromancerIntent(enemy, model),
                    _                       => null
                };
            }
        }

        /// <summary>Executes each living enemy's stored intent, applying movement and damage.</summary>
        public void ExecuteIntents(CombatModel model)
        {
            // Snapshot the list — Necromancer Revive adds enemies mid-loop
            var snapshot = new System.Collections.Generic.List<EnemyCombatModel>(model.Enemies);
            foreach (var enemy in snapshot)
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
                var target = enemy.HasStatus(StatusEffectType.Slow)
                    ? enemy.Position   // Slowed: stay in place
                    : StepToward(enemy, model);

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

        // ── ArmoredBeetle ───────────────────────────────────────────────────────

        private EnemyIntent ArmoredBeetleIntent(EnemyCombatModel enemy, CombatModel model)
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
                var target = enemy.HasStatus(StatusEffectType.Slow)
                    ? enemy.Position
                    : StepToward(enemy, model);

                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = target,
                    AttackCells    = new List<Vector2Int>()
                };
            }
        }

        // ── MirrorSlime ─────────────────────────────────────────────────────────

        private EnemyIntent MirrorSlimeIntent(EnemyCombatModel enemy, CombatModel model)
        {
            if (model.HasLastThrownPotion)
            {
                // Copy the last potion the player threw, target player's current position
                var aoe    = _damageService.GetAffectedCells(
                                 model.LastThrownPotionType,
                                 model.Player.Position,
                                 model.Grid);
                int damage = _damageService.GetDamage(
                                 model.LastThrownPotionType,
                                 model.LastThrownPotionLevel);

                // Cache the copy on the enemy model for view display
                enemy.HasCopiedPotion   = true;
                enemy.CopiedPotionType  = model.LastThrownPotionType;
                enemy.CopiedPotionLevel = model.LastThrownPotionLevel;

                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = aoe,
                    Damage      = damage
                };
            }
            else
            {
                // No potion copied yet — move toward player
                var target = enemy.HasStatus(StatusEffectType.Slow)
                    ? enemy.Position
                    : StepToward(enemy, model);

                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = target,
                    AttackCells    = new List<Vector2Int>()
                };
            }
        }

        // ── Phantom ─────────────────────────────────────────────────────────────

        private EnemyIntent PhantomIntent(EnemyCombatModel enemy, CombatModel model)
        {
            // Show all valid teleport positions (ring radius 1-2 around player)
            var ring = GetRingCells(model.Player.Position, 1, 2, model.Grid, model);

            // Pick an actual teleport destination
            var dest = ring.Count > 0
                ? ring[Random.Range(0, ring.Count)]
                : enemy.Position;

            return new EnemyIntent
            {
                Type           = IntentType.Teleport,
                TargetPosition = dest,
                AttackCells    = ring,   // shown as intent highlight on grid
                Damage         = 1
            };
        }

        // ── Necromancer ─────────────────────────────────────────────────────────

        private EnemyIntent NecromancerIntent(EnemyCombatModel enemy, CombatModel model)
        {
            // Find first revivable enemy in graveyard
            var target = model.Graveyard.FirstOrDefault();
            if (target != null)
            {
                // Find a valid position adjacent to the necromancer
                var revivePos = FindFreeAdjacent(enemy.Position, model);
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
                : StepAway(enemy, model);

            return new EnemyIntent
            {
                Type           = IntentType.Move,
                TargetPosition = fleePos,
                AttackCells    = new List<Vector2Int>()
            };
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
                        if (!other.IsDead && other.EntityId != enemy.EntityId && other.Position == target)
                            return;

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
                    Debug.Log($"[AIService] MushroomBomb({enemy.EntityId}) countdown...");
                    break;

                case IntentType.Explode:
                {
                    foreach (var other in model.Enemies)
                    {
                        if (other.IsDead || other.EntityId == enemy.EntityId) continue;
                        if (intent.AttackCells.Contains(other.Position))
                            _damageService.ApplyDamage(other, intent.Damage);
                    }
                    if (intent.AttackCells.Contains(model.Player.Position))
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

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
                    var pulled = StepPlayerToward(model.Player.Position, enemy.Position, model);
                    model.Player.Position = pulled;

                    if (Manhattan(pulled, enemy.Position) <= 1)
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                    Debug.Log($"[AIService] MagnetGolem({enemy.EntityId}) pulled player to {pulled}");
                    break;
                }

                case IntentType.Teleport:
                {
                    var from = enemy.Position;
                    enemy.Position = intent.TargetPosition;

                    EventBus.Publish(new EnemyTeleportedEvent
                    {
                        EntityId = enemy.EntityId,
                        FromPos  = from,
                        ToPos    = enemy.Position
                    });

                    // Attack after teleport if now adjacent
                    if (Manhattan(enemy.Position, model.Player.Position) <= 1)
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                    Debug.Log($"[AIService] Phantom({enemy.EntityId}) teleported {from} → {enemy.Position}");
                    break;
                }

                case IntentType.Revive:
                {
                    var deadEnemy = model.Graveyard.FirstOrDefault(e => e.EntityId == intent.ReviveEntityId);
                    if (deadEnemy == null) break;

                    model.Graveyard.Remove(deadEnemy);
                    deadEnemy.HP       = 1;
                    deadEnemy.Position = intent.TargetPosition;
                    model.Enemies.Add(deadEnemy);

                    EventBus.Publish(new EnemyRevivedEvent
                    {
                        EntityId = deadEnemy.EntityId,
                        Position = deadEnemy.Position
                    });

                    Debug.Log($"[AIService] Necromancer({enemy.EntityId}) revived enemy {deadEnemy.EntityId} ({deadEnemy.Type}) at {deadEnemy.Position}");
                    break;
                }
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static Vector2Int StepToward(EnemyCombatModel mover, CombatModel model)
        {
            var from = mover.Position;
            var to   = model.Player.Position;
            int dx   = to.x - from.x;
            int dy   = to.y - from.y;

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

        /// <summary>Steps one cell away from the player.</summary>
        private static Vector2Int StepAway(EnemyCombatModel mover, CombatModel model)
        {
            var from = mover.Position;
            var to   = model.Player.Position;
            int dx   = from.x - to.x;
            int dy   = from.y - to.y;

            var step = Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dy > 0 ? 1 : -1);

            var candidate = from + step;

            if (!model.Grid.IsInBounds(candidate.x, candidate.y)) return from;
            foreach (var other in model.Enemies)
                if (!other.IsDead && other.EntityId != mover.EntityId && other.Position == candidate)
                    return from;
            if (candidate == model.Player.Position) return from;

            return candidate;
        }

        private static Vector2Int StepPlayerToward(Vector2Int playerPos, Vector2Int golemPos, CombatModel model)
        {
            int dx = golemPos.x - playerPos.x;
            int dy = golemPos.y - playerPos.y;

            var step = Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dy > 0 ? 1 : -1);

            var candidate = playerPos + step;

            if (!model.Grid.IsInBounds(candidate.x, candidate.y)) return playerPos;
            foreach (var enemy in model.Enemies)
                if (!enemy.IsDead && enemy.Position == candidate) return playerPos;

            return candidate;
        }

        /// <summary>Returns empty grid cells at ring distance [minR, maxR] from center (no player, no enemy).</summary>
        private static List<Vector2Int> GetRingCells(Vector2Int center, int minR, int maxR,
                                                      GridModel grid, CombatModel model)
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < GridModel.Width; x++)
            for (int y = 0; y < GridModel.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                int dist = Manhattan(cell, center);
                if (dist < minR || dist > maxR) continue;
                if (cell == model.Player.Position) continue;
                if (model.Enemies.Exists(e => !e.IsDead && e.Position == cell)) continue;
                cells.Add(cell);
            }
            return cells;
        }

        /// <summary>Finds a free adjacent cell next to pos (not occupied by player or enemy).</summary>
        private static Vector2Int? FindFreeAdjacent(Vector2Int pos, CombatModel model)
        {
            var dirs = new[]
            {
                new Vector2Int(1, 0), new Vector2Int(-1, 0),
                new Vector2Int(0, 1), new Vector2Int(0, -1)
            };
            foreach (var d in dirs)
            {
                var c = pos + d;
                if (!model.Grid.IsInBounds(c.x, c.y)) continue;
                if (c == model.Player.Position) continue;
                if (model.Enemies.Exists(e => !e.IsDead && e.Position == c)) continue;
                return c;
            }
            return null;
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
