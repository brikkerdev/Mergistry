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

        // ── Movement ────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns all cells reachable from the player's current position:
        /// horizontal/vertical up to 2 steps + L-shapes (±1,±2) and (±2,±1).
        /// </summary>
        public List<Vector2Int> GetValidMoves(CombatModel combat)
        {
            var result  = new List<Vector2Int>();
            var pos     = combat.Player.Position;
            var grid    = combat.Grid;

            var offsets = new[]
            {
                // Horizontal / vertical (1 and 2 steps)
                new Vector2Int( 1,  0), new Vector2Int( 2,  0),
                new Vector2Int(-1,  0), new Vector2Int(-2,  0),
                new Vector2Int( 0,  1), new Vector2Int( 0,  2),
                new Vector2Int( 0, -1), new Vector2Int( 0, -2),
                // L-shapes (knight-style)
                new Vector2Int( 1,  2), new Vector2Int(-1,  2),
                new Vector2Int( 1, -2), new Vector2Int(-1, -2),
                new Vector2Int( 2,  1), new Vector2Int(-2,  1),
                new Vector2Int( 2, -1), new Vector2Int(-2, -1),
            };

            foreach (var off in offsets)
            {
                var target = pos + off;
                if (grid.IsInBounds(target.x, target.y))
                    result.Add(target);
            }

            return result;
        }

        // ── Combat actions ──────────────────────────────────────────────────────

        /// <summary>
        /// Throws the potion in <paramref name="slotIndex"/> at <paramref name="targetCell"/>.
        /// Sets cooldown on the slot and marks the player as having acted.
        /// Publishes <see cref="PotionThrownEvent"/>.
        /// </summary>
        public bool ThrowPotion(CombatModel model, InventoryModel inventory,
                                int slotIndex, Vector2Int targetCell)
        {
            var slot = inventory.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty || slot.CooldownRemaining > 0) return false;

            // Cooldown: 2 turns normally, 1 turn at level 3
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

        /// <summary>
        /// Skips the current turn: resets movement/action flags, ticks all cooldowns down
        /// by 1, and heals the player for 1 HP (capped at MaxHP).
        /// </summary>
        public void SkipTurn(CombatModel model, InventoryModel inventory)
        {
            // Reset per-turn flags
            model.Player.HasMoved = false;
            model.Player.HasActed = false;

            // Heal +1 HP
            if (model.Player.HP < model.Player.MaxHP)
                model.Player.HP++;

            // Tick cooldowns
            for (int i = 0; i < InventoryModel.SlotCount; i++)
            {
                var slot = inventory.GetSlot(i);
                if (slot != null && slot.CooldownRemaining > 0)
                    slot.CooldownRemaining--;
            }

            Debug.Log($"[CombatService] Turn skipped — HP={model.Player.HP}/{model.Player.MaxHP}");
        }
    }
}
