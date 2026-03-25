using System.Collections.Generic;
using System.Linq;
using Mergistry.Data;
using Mergistry.Models.Map;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Generates a FloorMapModel with branching paths for a given floor level.
    /// Structure: 4 content rows + 1 boss row.
    /// Guarantees: 1 Elite (rows 1+), 1–2 Events (any row), 1 Boss (last row).
    /// </summary>
    public class MapGeneratorService : IMapGeneratorService
    {
        // Number of nodes per content row (boss row always has 1)
        private static readonly int[] RowSizes = { 2, 3, 2, 2 };

        public FloorMapModel GenerateFloor(int floor)
        {
            var rng   = new System.Random(Random.Range(0, int.MaxValue));
            var model = new FloorMapModel { TotalRows = RowSizes.Length + 1 };
            int nextId = 0;
            var rowNodes = new List<List<MapNode>>();

            // ── Content rows ────────────────────────────────────────────────
            for (int row = 0; row < RowSizes.Length; row++)
            {
                var nodesInRow = new List<MapNode>();
                for (int col = 0; col < RowSizes[row]; col++)
                {
                    var node = new MapNode
                    {
                        Id        = nextId++,
                        Type      = MapNodeType.Combat,
                        Row       = row,
                        Col       = col,
                        TotalCols = RowSizes[row]
                    };
                    nodesInRow.Add(node);
                    model.Nodes.Add(node);
                }
                rowNodes.Add(nodesInRow);
            }

            // ── Boss row ─────────────────────────────────────────────────────
            {
                var bossRow = new List<MapNode>();
                var boss = new MapNode
                {
                    Id        = nextId++,
                    Type      = MapNodeType.Boss,
                    Row       = RowSizes.Length,
                    Col       = 0,
                    TotalCols = 1
                };
                bossRow.Add(boss);
                model.Nodes.Add(boss);
                rowNodes.Add(bossRow);
            }

            // ── Connect rows ─────────────────────────────────────────────────
            for (int row = 0; row < rowNodes.Count - 1; row++)
                ConnectRows(rowNodes[row], rowNodes[row + 1], rng);

            // ── Assign types ─────────────────────────────────────────────────
            AssignTypes(rowNodes, floor, rng);

            // ── Row 0 starts accessible ──────────────────────────────────────
            foreach (var n in rowNodes[0])
                n.IsAccessible = true;

            return model;
        }

        // ── Connection algorithm ──────────────────────────────────────────────

        private static void ConnectRows(List<MapNode> from, List<MapNode> to, System.Random rng)
        {
            int fromCount = from.Count;
            int toCount   = to.Count;
            var toReached = new bool[toCount];

            foreach (var fromNode in from)
            {
                float fromNorm = fromCount > 1
                    ? (float)fromNode.Col / (fromCount - 1)
                    : 0.5f;

                // Primary: closest to-node by normalised column
                int   bestIdx  = 0;
                float bestDist = float.MaxValue;
                for (int i = 0; i < toCount; i++)
                {
                    float toNorm = toCount > 1 ? (float)i / (toCount - 1) : 0.5f;
                    float dist   = Mathf.Abs(fromNorm - toNorm);
                    if (dist < bestDist) { bestDist = dist; bestIdx = i; }
                }
                Connect(fromNode, to[bestIdx]);
                toReached[bestIdx] = true;

                // Optional second connection (branching ~40%)
                if (rng.NextDouble() < 0.40)
                {
                    int second = rng.NextDouble() < 0.5 && bestIdx > 0
                        ? bestIdx - 1
                        : (bestIdx < toCount - 1 ? bestIdx + 1 : bestIdx - 1);

                    if (second >= 0 && second < toCount && second != bestIdx)
                    {
                        Connect(fromNode, to[second]);
                        toReached[second] = true;
                    }
                }
            }

            // Ensure every to-node has at least one incoming connection
            for (int i = 0; i < toCount; i++)
            {
                if (toReached[i]) continue;
                float toNorm  = toCount > 1 ? (float)i / (toCount - 1) : 0.5f;
                var   closest = from.OrderBy(f =>
                {
                    float fn = fromCount > 1 ? (float)f.Col / (fromCount - 1) : 0.5f;
                    return Mathf.Abs(fn - toNorm);
                }).First();
                Connect(closest, to[i]);
            }
        }

        private static void Connect(MapNode from, MapNode to)
        {
            if (!from.NextNodeIds.Contains(to.Id))
                from.NextNodeIds.Add(to.Id);
        }

        // ── Type assignment ────────────────────────────────────────────────────

        private static void AssignTypes(List<List<MapNode>> rowNodes, int floor, System.Random rng)
        {
            // All content nodes (not boss)
            var content = rowNodes.Take(rowNodes.Count - 1).SelectMany(r => r).ToList();

            // Shuffle
            for (int i = content.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (content[i], content[j]) = (content[j], content[i]);
            }

            // 1 Elite in rows 1+
            var elitePool = content.Where(n => n.Row >= 1).ToList();
            if (elitePool.Count > 0)
            {
                int idx = rng.Next(elitePool.Count);
                elitePool[idx].Type = MapNodeType.Elite;
            }

            // 1–2 Events in any row
            int eventCount = floor >= 1 ? 2 : 1;
            var combatPool = content.Where(n => n.Type == MapNodeType.Combat).ToList();
            for (int i = 0; i < eventCount && combatPool.Count > 0; i++)
            {
                int idx = rng.Next(combatPool.Count);
                combatPool[idx].Type = MapNodeType.Event;
                combatPool.RemoveAt(idx);
            }

            // Rest remain Combat (already set as default)
        }
    }
}
