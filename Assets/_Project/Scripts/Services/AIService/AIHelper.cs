using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Static utility methods shared by all IEnemyBehavior implementations.
    /// </summary>
    internal static class AIHelper
    {
        internal static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        /// <summary>Steps one cell toward the player, respecting grid bounds and occupancy.</summary>
        internal static Vector2Int StepToward(EnemyCombatModel mover, CombatModel model)
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
        internal static Vector2Int StepAway(EnemyCombatModel mover, CombatModel model)
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

        internal static Vector2Int StepPlayerToward(Vector2Int playerPos, Vector2Int golemPos, CombatModel model)
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
        internal static List<Vector2Int> GetRingCells(Vector2Int center, int minR, int maxR,
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
        internal static Vector2Int? FindFreeAdjacent(Vector2Int pos, CombatModel model)
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

        internal static Vector2Int LineDirection(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;
            if (dx == 0 && dy == 0) return new Vector2Int(-1, 0);
            return Mathf.Abs(dx) >= Mathf.Abs(dy)
                ? new Vector2Int(dx > 0 ? 1 : -1, 0)
                : new Vector2Int(0, dy > 0 ? 1 : -1);
        }

        internal static List<Vector2Int> LineCells(Vector2Int origin, Vector2Int dir, int range, GridModel grid)
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

        internal static List<Vector2Int> Box3x3(Vector2Int center, GridModel grid)
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
    }
}
