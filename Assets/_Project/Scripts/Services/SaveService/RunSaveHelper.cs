using System;
using Mergistry.Data;
using Mergistry.Models;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Converts RunModel + InventoryModel → RunSaveData and back.
    /// Keeps SaveService free of game-model references.
    /// </summary>
    public static class RunSaveHelper
    {
        // ── Snapshot ─────────────────────────────────────────────────────────

        public static RunSaveData Snapshot(RunModel run, InventoryModel inventory)
        {
            var data = new RunSaveData
            {
                CurrentFloor  = run.CurrentFloor,
                CurrentFight  = run.CurrentFight,
                CurrentNodeId = run.CurrentNodeId,
                PersistentHP  = run.PersistentHP,
                MaxHP         = run.MaxHP,
            };

            // Relics
            foreach (var relic in run.Relics.ActiveRelics)
                data.ActiveRelics.Add((int)relic);

            // Inventory slots
            for (int s = 0; s < InventoryModel.SlotCount; s++)
            {
                var slot = inventory.GetSlot(s);
                data.InventorySlots.Add(new PotionSlotSaveData
                {
                    PotionTypeInt = slot.IsEmpty ? -1 : (int)slot.Type,
                    Level         = slot.Level,
                    Cooldown      = slot.CooldownRemaining,
                });
            }

            // Map visited nodes
            if (run.FloorMap != null)
            {
                foreach (var node in run.FloorMap.Nodes)
                {
                    data.MapNodeIds.Add(node.Id);
                    data.MapNodeVisited.Add(node.IsVisited);
                }
            }

            return data;
        }

        // ── Restore ──────────────────────────────────────────────────────────

        public static void Restore(RunSaveData data, RunModel run, InventoryModel inventory)
        {
            run.CurrentFloor  = data.CurrentFloor;
            run.CurrentFight  = data.CurrentFight;
            run.CurrentNodeId = data.CurrentNodeId;
            run.PersistentHP  = data.PersistentHP;
            run.MaxHP         = data.MaxHP;

            // Relics
            run.Relics.Clear();
            foreach (int ri in data.ActiveRelics)
            {
                try { run.Relics.Add((RelicType)ri); }
                catch { Debug.LogWarning($"[RunSaveHelper] Unknown relic int: {ri}"); }
            }

            // Inventory
            inventory.Clear();
            for (int s = 0; s < data.InventorySlots.Count && s < InventoryModel.SlotCount; s++)
            {
                var sd = data.InventorySlots[s];
                if (sd.PotionTypeInt < 0) continue;
                try
                {
                    var potionType = (PotionType)sd.PotionTypeInt;
                    inventory.Replace(s, potionType, sd.Level);
                    inventory.GetSlot(s).CooldownRemaining = sd.Cooldown;
                }
                catch { Debug.LogWarning($"[RunSaveHelper] Unknown potion int: {sd.PotionTypeInt}"); }
            }

            // Map node visited flags (FloorMap must already be regenerated before this call)
            if (run.FloorMap != null && data.MapNodeIds.Count > 0)
            {
                for (int n = 0; n < data.MapNodeIds.Count; n++)
                {
                    var node = run.FloorMap.GetNode(data.MapNodeIds[n]);
                    if (node != null) node.IsVisited = data.MapNodeVisited[n];
                }
            }
        }
    }
}
