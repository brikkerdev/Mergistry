using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class CombatService
    {
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
        /// Spawns enemies for the given fight index:
        /// 0 → 1 Skeleton;  1 → 2 Skeletons;  2 → 1 Skeleton + 1 Spider;
        /// 3 → 1 MushroomBomb + 2 Skeletons;
        /// 4 → 1 MagnetGolem;
        /// 5 → 1 ArmoredBeetle.
        /// </summary>
        public void SpawnEnemies(CombatModel model, int fightIndex)
        {
            switch (fightIndex)
            {
                case 0:
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                           new Vector2Int(3, 3), hp: 3));
                    break;
                case 1:
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                           new Vector2Int(3, 3), hp: 3));
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                           new Vector2Int(1, 3), hp: 3));
                    break;
                case 2:
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                           new Vector2Int(3, 3), hp: 3));
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Spider,
                                                           new Vector2Int(4, 1), hp: 2));
                    break;
                // A2 test fights
                case 3:
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.MushroomBomb,
                                                           new Vector2Int(2, 4), hp: 3, bombTimer: 3));
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                           new Vector2Int(3, 3), hp: 3));
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                           new Vector2Int(1, 3), hp: 3));
                    break;
                case 4:
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.MagnetGolem,
                                                           new Vector2Int(3, 3), hp: 6));
                    break;
                case 5:
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.ArmoredBeetle,
                                                           new Vector2Int(3, 3), hp: 4, armorPoints: 2));
                    break;
                default:
                    model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                           new Vector2Int(3, 3), hp: 3));
                    break;
            }
        }

        // ── Movement ────────────────────────────────────────────────────────────

        public List<Vector2Int> GetValidMoves(CombatModel combat)
        {
            var result = new List<Vector2Int>();
            var pos    = combat.Player.Position;
            var grid   = combat.Grid;

            var offsets = new[]
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
            model.Player.HasActed  = true;

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
        /// Returns the result for the caller to apply effects and animation.
        /// </summary>
        public PushResult PushEnemy(CombatModel model, EnemyCombatModel enemy, Vector2Int direction)
        {
            var newPos   = enemy.Position + direction;
            bool wallHit = !model.Grid.IsInBounds(newPos.x, newPos.y);

            if (!wallHit)
            {
                // Check blocked by player or another enemy
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

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static bool IsOccupiedByEnemy(Vector2Int pos, CombatModel model)
        {
            foreach (var enemy in model.Enemies)
                if (!enemy.IsDead && enemy.Position == pos) return true;
            return false;
        }
    }

    public struct PushResult
    {
        public bool Moved;
        public int  BonusDamage;
    }
}
