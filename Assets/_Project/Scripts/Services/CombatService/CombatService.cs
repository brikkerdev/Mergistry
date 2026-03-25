using System.Collections.Generic;
using System.Linq;
using Mergistry.Core;
using Mergistry.Data;
using Mergistry.Events;
using Mergistry.Models;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class CombatService : ICombatService
    {
        private readonly IRelicService _relicService;

        public CombatService(IRelicService relicService) { _relicService = relicService; }

        // ── Init ────────────────────────────────────────────────────────────────

        /// <summary>Returns a fresh combat model with the player placed at (1,1).</summary>
        public CombatModel InitCombat()
        {
            var model = new CombatModel();
            model.Player.Position = new Vector2Int(1, 1);
            model.Player.HasMoved = false;
            model.Player.HasActed = false;
            return model;
        }

        /// <summary>
        /// Spawns enemies from a procedurally generated CombatSetup.
        /// Positions are distributed across the grid to avoid overlap.
        /// </summary>
        public void SpawnEnemies(CombatModel model, CombatSetup setup)
        {
            if (setup == null || setup.Enemies.Count == 0)
            {
                model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                       new Vector2Int(3, 3), hp: 3));
                return;
            }

            var positions = GetSpawnPositions(setup.Enemies.Count, model.Player.Position);

            for (int i = 0; i < setup.Enemies.Count; i++)
            {
                var info = setup.Enemies[i];
                var pos  = i < positions.Count ? positions[i] : new Vector2Int(3, 3);
                model.Enemies.Add(new EnemyCombatModel(
                    model.NextEntityId(), info.Type, pos,
                    hp: info.HP, armorPoints: info.ArmorPoints, bombTimer: info.BombTimer));
            }
        }

        /// <summary>
        /// Returns spawn positions distributed in the far half of the 5x5 grid,
        /// away from the player start position. No two enemies on the same cell.
        /// </summary>
        private static List<Vector2Int> GetSpawnPositions(int count, Vector2Int playerPos)
        {
            // Candidate cells: prefer rows 2-4, columns 1-4 (away from player at 1,1)
            var candidates = new List<Vector2Int>
            {
                new Vector2Int(3, 3),
                new Vector2Int(1, 3),
                new Vector2Int(3, 1),
                new Vector2Int(4, 4),
                new Vector2Int(2, 4),
                new Vector2Int(4, 2),
                new Vector2Int(0, 3),
                new Vector2Int(3, 0),
            };

            // Remove player position
            candidates.RemoveAll(c => c == playerPos);

            var result = new List<Vector2Int>();
            for (int i = 0; i < count && i < candidates.Count; i++)
                result.Add(candidates[i]);

            // Fallback if we need more positions than candidates
            while (result.Count < count)
                result.Add(new Vector2Int(3, 3));

            return result;
        }

        // ── Movement ────────────────────────────────────────────────────────────

        public List<Vector2Int> GetValidMoves(CombatModel combat)
        {
            var result = new List<Vector2Int>();
            var pos    = combat.Player.Position;
            var grid   = combat.Grid;

            // When the player is Slowed, restrict to Manhattan distance 1 only (no 2-cell or L-shape moves)
            bool isSlowed = combat.Player.HasStatus(StatusEffectType.Slow);

            var offsets = isSlowed
                ? new[]
                {
                    new Vector2Int( 1,  0),
                    new Vector2Int(-1,  0),
                    new Vector2Int( 0,  1),
                    new Vector2Int( 0, -1),
                }
                : new[]
                {
                    new Vector2Int( 1,  0), new Vector2Int( 2,  0),
                    new Vector2Int(-1,  0), new Vector2Int(-2,  0),
                    new Vector2Int( 0,  1), new Vector2Int( 0,  2),
                    new Vector2Int( 0, -1), new Vector2Int( 0, -2),
                    new Vector2Int( 1,  2), new Vector2Int(-1,  2),
                    new Vector2Int( 1, -2), new Vector2Int(-1, -2),
                    new Vector2Int( 2,  1), new Vector2Int(-2,  1),
                    new Vector2Int( 2, -1), new Vector2Int(-2, -1),
                };

            foreach (var off in offsets)
            {
                var target = pos + off;
                if (!grid.IsInBounds(target.x, target.y)) continue;
                if (IsOccupiedByEnemy(target, combat)) continue;
                result.Add(target);
            }

            return result;
        }

        // ── Combat actions ──────────────────────────────────────────────────────

        /// <summary>Throws the potion at targetCell. Sets cooldown, marks HasActed, publishes event.</summary>
        public bool ThrowPotion(CombatModel model, InventoryModel inventory,
                                int slotIndex, Vector2Int targetCell)
        {
            var slot = inventory.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty || slot.CooldownRemaining > 0) return false;

            slot.CooldownRemaining = slot.Level >= 3 ? 1 : 2;

            // A5: Thermos — 25% chance to not consume cooldown
            if (_relicService != null && _relicService.HasRelic(RelicType.Thermos))
            {
                if (Random.value < 0.25f)
                {
                    slot.CooldownRemaining = 0;
                    Debug.Log("[CombatService] Thermos activated — cooldown preserved!");
                }
            }

            model.Player.HasActed  = true;

            // A3: track last thrown potion for MirrorSlime
            model.HasLastThrownPotion   = true;
            model.LastThrownPotionType  = slot.Type;
            model.LastThrownPotionLevel = slot.Level;

            EventBus.Publish(new PotionThrownEvent
            {
                Type       = slot.Type,
                Level      = slot.Level,
                TargetCell = targetCell
            });

            Debug.Log($"[CombatService] Potion thrown: {slot.Type} lv{slot.Level} → {targetCell}, cooldown={slot.CooldownRemaining}");
            return true;
        }

        /// <summary>Ticks all potion cooldowns down by 1 and resets per-turn player flags.</summary>
        public void StartNextPlayerTurn(CombatModel model, InventoryModel inventory)
        {
            model.Player.HasMoved = false;
            model.Player.HasActed = false;

            for (int i = 0; i < InventoryModel.SlotCount; i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot != null && slot.CooldownRemaining > 0)
                    slot.CooldownRemaining--;
            }
        }

        /// <summary>Skip action: heals +1 HP.</summary>
        public void HealOnSkip(CombatModel model)
        {
            if (model.Player.HP < model.Player.MaxHP)
                model.Player.HP++;

            Debug.Log($"[CombatService] Skip heal — HP={model.Player.HP}/{model.Player.MaxHP}");
        }

        // ── A2: Enemy push ──────────────────────────────────────────────────────

        /// <summary>
        /// Pushes an adjacent enemy one cell in the given direction.
        /// If the destination is out of bounds or blocked → wall hit (+1 bonus damage).
        /// </summary>
        public PushResult PushEnemy(CombatModel model, EnemyCombatModel enemy, Vector2Int direction)
        {
            var newPos   = enemy.Position + direction;
            bool wallHit = !model.Grid.IsInBounds(newPos.x, newPos.y);

            if (!wallHit)
            {
                if (newPos == model.Player.Position)
                {
                    wallHit = true;
                }
                else
                {
                    foreach (var other in model.Enemies)
                    {
                        if (!other.IsDead && other.EntityId != enemy.EntityId && other.Position == newPos)
                        {
                            wallHit = true;
                            break;
                        }
                    }
                }
            }

            var fromPos = enemy.Position;
            if (!wallHit)
                enemy.Position = newPos;

            EventBus.Publish(new EnemyPushedEvent
            {
                EntityId = enemy.EntityId,
                FromPos  = fromPos,
                ToPos    = enemy.Position,
                WallHit  = wallHit
            });

            Debug.Log($"[CombatService] Pushed enemy {enemy.EntityId} " +
                      $"{fromPos} → {enemy.Position} (wallHit={wallHit})");

            return new PushResult { Moved = !wallHit, BonusDamage = wallHit ? 1 : 0 };
        }

        // ── A3: Zone & status management ────────────────────────────────────────

        /// <summary>
        /// Applies zone DoT effects to all entities standing in Fire or Poison zones,
        /// and applies Slow status to entities standing in Water zones.
        /// </summary>
        public void ApplyZoneEffects(CombatModel model, IDamageService damageService)
        {
            foreach (var zone in model.Grid.Zones)
            {
                if (zone.Type == ZoneType.Fire || zone.Type == ZoneType.Poison)
                {
                    if (model.Player.Position == zone.Position)
                        damageService.ApplyDamageToPlayer(model.Player, 1);

                    foreach (var enemy in model.Enemies.ToList())
                    {
                        if (!enemy.IsDead && enemy.Position == zone.Position)
                            damageService.ApplyDamage(enemy, 1);
                    }
                }
                else if (zone.Type == ZoneType.Water)
                {
                    if (model.Player.Position == zone.Position)
                        ApplyOrRefreshStatus(model.Player.StatusEffects, StatusEffectType.Slow, 1);

                    foreach (var enemy in model.Enemies)
                    {
                        if (!enemy.IsDead && enemy.Position == zone.Position)
                            ApplyOrRefreshStatus(enemy.StatusEffects, StatusEffectType.Slow, 1);
                    }
                }
            }
        }

        /// <summary>
        /// Decrements all zone durations; removes expired ones and returns them.
        /// </summary>
        public List<ZoneInstance> TickZones(GridModel grid)
        {
            var expired = new List<ZoneInstance>();
            for (int i = grid.Zones.Count - 1; i >= 0; i--)
            {
                grid.Zones[i].TurnsRemaining--;
                if (grid.Zones[i].TurnsRemaining <= 0)
                {
                    expired.Add(grid.Zones[i]);
                    grid.Zones.RemoveAt(i);
                }
            }

            foreach (var z in expired)
            {
                EventBus.Publish(new ZoneExpiredEvent { Position = z.Position, Type = z.Type });
                Debug.Log($"[CombatService] Zone {z.Type} at {z.Position} expired");
            }

            return expired;
        }

        /// <summary>
        /// Ticks all status effect durations. Applies Poison DoT (1 damage/turn).
        /// </summary>
        public void TickStatuses(CombatModel model, IDamageService damageService)
        {
            // Player statuses
            TickEntityStatuses(model.Player.StatusEffects);
            if (model.Player.HasStatus(StatusEffectType.Poison))
                damageService.ApplyDamageToPlayer(model.Player, 1);

            // Enemy statuses
            foreach (var enemy in model.Enemies)
            {
                if (enemy.IsDead) continue;
                TickEntityStatuses(enemy.StatusEffects);
                if (enemy.HasStatus(StatusEffectType.Poison))
                    damageService.ApplyDamage(enemy, 1);
            }
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static bool IsOccupiedByEnemy(Vector2Int pos, CombatModel model)
        {
            foreach (var enemy in model.Enemies)
                if (!enemy.IsDead && enemy.Position == pos) return true;
            return false;
        }

        private static void ApplyOrRefreshStatus(List<StatusEffect> effects, StatusEffectType type, int duration)
        {
            var existing = effects.Find(s => s.Type == type);
            if (existing != null)
                existing.Duration = Mathf.Max(existing.Duration, duration);
            else
                effects.Add(new StatusEffect(type, duration));
        }

        private static void TickEntityStatuses(List<StatusEffect> effects)
        {
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                effects[i].Duration--;
                if (effects[i].Duration <= 0)
                    effects.RemoveAt(i);
            }
        }
    }

    public struct PushResult
    {
        public bool Moved;
        public int  BonusDamage;
    }
}
