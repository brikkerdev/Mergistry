using System.Collections.Generic;
using System.Linq;
using Mergistry.Data;
using Mergistry.Models.Combat;
using Mergistry.Models.Map;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Generates a FloorMapModel with procedural branching paths for a given floor.
    /// Graph structure is fully random:
    ///   - 3-4 content rows (randomized)
    ///   - 2-3 nodes per row (randomized, guarantees min 2 paths)
    ///   - 1 boss row (always 1 node)
    /// Guarantees: 1 Elite (rows 1+), 1-2 Events, 1 Boss.
    /// Combat setups are procedurally generated per node based on floor-specific
    /// enemy pools and HP budgets from the design document.
    /// </summary>
    public class MapGeneratorService : IMapGeneratorService
    {
        // ── Enemy pools per floor ───────────────────────────────────────────────
        // Floor 0 (1): Skeleton, Spider, MushroomBomb
        // Floor 1 (2): + MagnetGolem, MirrorSlime, ArmoredBeetle
        // Floor 2 (3): + Phantom, Necromancer

        private static readonly EnemyType[] Floor0Pool =
            { EnemyType.Skeleton, EnemyType.Spider, EnemyType.MushroomBomb };

        private static readonly EnemyType[] Floor1Pool =
            { EnemyType.Skeleton, EnemyType.Spider, EnemyType.MushroomBomb,
              EnemyType.MagnetGolem, EnemyType.MirrorSlime, EnemyType.ArmoredBeetle };

        private static readonly EnemyType[] Floor2Pool =
            { EnemyType.Skeleton, EnemyType.Spider, EnemyType.MushroomBomb,
              EnemyType.MagnetGolem, EnemyType.MirrorSlime, EnemyType.ArmoredBeetle,
              EnemyType.Phantom, EnemyType.Necromancer };

        // ── HP budgets per floor (min, max) ─────────────────────────────────────
        // Design doc: Floor 1: 3-8, Floor 2: 6-14, Floor 3: 8-16
        private static readonly (int min, int max)[] CombatHPBudgets =
            { (3, 8), (6, 14), (8, 16) };

        private static readonly (int min, int max)[] EliteHPBudgets =
            { (6, 10), (10, 16), (14, 20) };

        // ── Base HP for each enemy type ─────────────────────────────────────────
        private static int GetBaseHP(EnemyType type) => type switch
        {
            EnemyType.Skeleton     => 3,
            EnemyType.Spider       => 2,
            EnemyType.MushroomBomb => 2,
            EnemyType.MagnetGolem  => 6,
            EnemyType.MirrorSlime  => 4,
            EnemyType.ArmoredBeetle=> 4,
            EnemyType.Phantom      => 2,
            EnemyType.Necromancer  => 5,
            _                      => 3,
        };

        private static int GetBaseArmor(EnemyType type) => type switch
        {
            EnemyType.ArmoredBeetle => 2,
            _                       => 0,
        };

        public FloorMapModel GenerateFloor(int floor)
        {
            var rng      = new System.Random(Random.Range(0, int.MaxValue));
            var rowNodes = new List<List<MapNode>>();
            int nextId   = 0;

            // ── Procedural row layout ───────────────────────────────────────
            // 3-4 content rows, each with 2-3 nodes
            int contentRowCount = rng.Next(3, 5); // 3 or 4
            var rowSizes = new int[contentRowCount];
            for (int i = 0; i < contentRowCount; i++)
                rowSizes[i] = rng.Next(2, 4); // 2 or 3

            var model = new FloorMapModel { TotalRows = contentRowCount + 1 };

            // ── Content rows ────────────────────────────────────────────────
            for (int row = 0; row < contentRowCount; row++)
            {
                var nodesInRow = new List<MapNode>();
                for (int col = 0; col < rowSizes[row]; col++)
                {
                    var node = new MapNode
                    {
                        Id        = nextId++,
                        Type      = MapNodeType.Combat,
                        Row       = row,
                        Col       = col,
                        TotalCols = rowSizes[row]
                    };
                    nodesInRow.Add(node);
                    model.Nodes.Add(node);
                }
                rowNodes.Add(nodesInRow);
            }

            // ── Boss row (always 1 node) ─────────────────────────────────────
            {
                var bossRow = new List<MapNode>();
                var boss = new MapNode
                {
                    Id        = nextId++,
                    Type      = MapNodeType.Boss,
                    Row       = contentRowCount,
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

            // ── Generate combat setups for all non-Event nodes ───────────────
            foreach (var node in model.Nodes)
            {
                if (node.Type == MapNodeType.Event) continue;
                node.CombatSetup = GenerateCombatSetup(floor, node.Type, rng);
            }

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

        // ── Type assignment ──────────────────────────────────────────────────

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

            // 1-2 Events in any row
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

        // ── Procedural combat setup generation ───────────────────────────────

        /// <summary>
        /// Generates a random enemy composition for a combat node.
        /// Rules from design doc:
        /// - Regular: 1-3 enemies, Elite: up to 4, Boss: special composition
        /// - HP budget scales by floor
        /// - Max 1 Necromancer per fight
        /// - Enemy types restricted by floor (pool unlocks progressively)
        /// </summary>
        private CombatSetup GenerateCombatSetup(int floor, MapNodeType nodeType, System.Random rng)
        {
            var setup = new CombatSetup();

            if (nodeType == MapNodeType.Boss)
            {
                GenerateBossSetup(setup, floor, rng);
                return setup;
            }

            bool isElite = nodeType == MapNodeType.Elite;
            int maxEnemies = isElite ? 4 : 3;
            var budget = isElite
                ? EliteHPBudgets[Mathf.Clamp(floor, 0, 2)]
                : CombatHPBudgets[Mathf.Clamp(floor, 0, 2)];

            int targetHP = rng.Next(budget.min, budget.max + 1);
            var pool = GetEnemyPool(floor);

            int currentHP       = 0;
            int enemyCount      = 0;
            bool hasNecromancer  = false;

            while (currentHP < targetHP && enemyCount < maxEnemies)
            {
                // Filter pool: remove types that would exceed budget too much,
                // and enforce max 1 Necromancer
                var candidates = new List<EnemyType>();
                foreach (var type in pool)
                {
                    int hp = GetBaseHP(type);
                    if (currentHP + hp > targetHP + 2) continue; // allow small overshoot
                    if (type == EnemyType.Necromancer && hasNecromancer) continue;
                    candidates.Add(type);
                }

                if (candidates.Count == 0) break;

                var chosen = candidates[rng.Next(candidates.Count)];
                int chosenHP    = GetBaseHP(chosen);
                int chosenArmor = GetBaseArmor(chosen);

                // Elite fights: scale HP up slightly
                if (isElite)
                {
                    int bonus = rng.Next(0, 2); // 0 or 1 bonus HP
                    chosenHP += bonus;
                    if (chosen == EnemyType.ArmoredBeetle)
                        chosenArmor += rng.Next(0, 2); // 0 or 1 bonus armor
                }

                setup.Enemies.Add(new EnemySpawnInfo(chosen, chosenHP, chosenArmor));

                if (chosen == EnemyType.Necromancer) hasNecromancer = true;
                currentHP += chosenHP;
                enemyCount++;
            }

            // Ensure at least 1 enemy
            if (setup.Enemies.Count == 0)
            {
                var fallback = pool[rng.Next(pool.Length)];
                setup.Enemies.Add(new EnemySpawnInfo(fallback, GetBaseHP(fallback), GetBaseArmor(fallback)));
            }

            return setup;
        }

        /// <summary>
        /// Boss compositions per floor (design doc specific bosses).
        /// Currently uses strong mixed compositions as placeholders
        /// until proper boss types (SpiderQueen, IronGolem, Alchemist) are implemented.
        /// </summary>
        private void GenerateBossSetup(CombatSetup setup, int floor, System.Random rng)
        {
            switch (floor)
            {
                case 0: // Floor 1 boss: tough encounter from floor 1 pool
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.MushroomBomb, 3));
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.Spider, 3));
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.Skeleton, 4));
                    break;

                case 1: // Floor 2 boss: strong floor 2 enemies
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.ArmoredBeetle, 6, armorPoints: 3));
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.MagnetGolem, 7));
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.MirrorSlime, 5));
                    break;

                case 2: // Floor 3 boss: full roster
                default:
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.Necromancer, 7));
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.Phantom, 4));
                    setup.Enemies.Add(new EnemySpawnInfo(EnemyType.MirrorSlime, 6));
                    break;
            }
        }

        private static EnemyType[] GetEnemyPool(int floor) => floor switch
        {
            0 => Floor0Pool,
            1 => Floor1Pool,
            _ => Floor2Pool,
        };
    }
}
