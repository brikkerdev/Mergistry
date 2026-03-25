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

        /// <summary>Spawns M5 enemies into the model: Skeleton at (3,3) and Spider at (4,1).</summary>
        public void SpawnEnemies(CombatModel model)
        {
            model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Skeleton,
                                                   new Vector2Int(3, 3), hp: 3));
            model.Enemies.Add(new EnemyCombatModel(model.NextEntityId(), EnemyType.Spider,
                                                   new Vector2Int(4, 1), hp: 2));
        }

        // ── Movement ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all cells reachable from the player's current position:
        /// horizontal/vertical up to 2 steps + L-shapes (±1,±2) and (±2,±1).
        /// Occupied cells (by living enemies) are excluded.
        /// </summary>
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

        /// <summary>
        /// Skip action: heals +1 HP. Use StartNextPlayerTurn separately to reset flags/cooldowns.
        /// </summary>
        public void HealOnSkip(CombatModel model)
        {
            if (model.Player.HP < model.Player.MaxHP)
                model.Player.HP++;

            Debug.Log($"[CombatService] Skip heal — HP={model.Player.HP}/{model.Player.MaxHP}");
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static bool IsOccupiedByEnemy(Vector2Int pos, CombatModel model)
        {
            foreach (var enemy in model.Enemies)
                if (!enemy.IsDead && enemy.Position == pos) return true;
            return false;
        }
    }
}
