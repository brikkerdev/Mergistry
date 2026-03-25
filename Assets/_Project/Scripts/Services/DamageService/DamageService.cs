using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Pure logic service: AoE patterns and valid throw range calculations.
    /// No MonoBehaviour dependency — safe to call from any context.
    /// </summary>
    public class DamageService
    {
        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// All grid cells within Manhattan distance ≤ <paramref name="range"/> of the player
        /// (excluding the player's own cell).
        /// </summary>
        public List<Vector2Int> GetValidThrowRange(GridModel grid, Vector2Int playerPos, int range = 3)
        {
            var result = new List<Vector2Int>();
            for (int x = 0; x < GridModel.Width; x++)
            for (int y = 0; y < GridModel.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (cell != playerPos && Manhattan(cell, playerPos) <= range)
                    result.Add(cell);
            }
            return result;
        }

        /// <summary>
        /// Returns the set of grid cells hit by the AoE of the given potion type when
        /// thrown at <paramref name="target"/>.
        /// </summary>
        public List<Vector2Int> GetAffectedCells(PotionType type, Vector2Int target, GridModel grid)
        {
            return type switch
            {
                PotionType.Flame  => Cross(target, 1, grid),        // 5-cell cross
                PotionType.Stream => Row(target.y, grid),            // full row
                PotionType.Poison => Block2x2(target, grid),         // 2×2 block
                PotionType.Steam  => Cross(target, 2, grid),         // larger cross (Aqua+Ignis)
                PotionType.Napalm => Box3x3(target, grid),           // 3×3 explosion (Ignis+Toxin)
                PotionType.Acid   => Column(target.x, grid),         // full column (Aqua+Toxin)
                _                 => new List<Vector2Int> { target }
            };
        }

        /// <summary>Base damage dealt by a potion (before enemy resistance, future use).</summary>
        public int GetDamage(PotionType type, int level) =>
            type switch
            {
                PotionType.Flame  => 2 * level,
                PotionType.Stream => 1 * level,
                PotionType.Poison => 2 * level,
                PotionType.Steam  => 1 * level,
                PotionType.Napalm => 3 * level,
                PotionType.Acid   => 2 * level,
                _                 => level
            };

        // ── AoE helpers ─────────────────────────────────────────────────────────

        private static List<Vector2Int> Cross(Vector2Int center, int radius, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            AddIfInBounds(cells, center, grid);
            for (int i = 1; i <= radius; i++)
            {
                AddIfInBounds(cells, new Vector2Int(center.x + i, center.y), grid);
                AddIfInBounds(cells, new Vector2Int(center.x - i, center.y), grid);
                AddIfInBounds(cells, new Vector2Int(center.x, center.y + i), grid);
                AddIfInBounds(cells, new Vector2Int(center.x, center.y - i), grid);
            }
            return cells;
        }

        private static List<Vector2Int> Row(int row, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < GridModel.Width; x++)
                cells.Add(new Vector2Int(x, row));
            return cells;
        }

        private static List<Vector2Int> Column(int col, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < GridModel.Height; y++)
                cells.Add(new Vector2Int(col, y));
            return cells;
        }

        private static List<Vector2Int> Block2x2(Vector2Int origin, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int dx = 0; dx <= 1; dx++)
            for (int dy = 0; dy <= 1; dy++)
                AddIfInBounds(cells, new Vector2Int(origin.x + dx, origin.y + dy), grid);
            return cells;
        }

        private static List<Vector2Int> Box3x3(Vector2Int center, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                AddIfInBounds(cells, new Vector2Int(center.x + dx, center.y + dy), grid);
            return cells;
        }

        private static void AddIfInBounds(List<Vector2Int> list, Vector2Int cell, GridModel grid)
        {
            if (grid.IsInBounds(cell.x, cell.y))
                list.Add(cell);
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
