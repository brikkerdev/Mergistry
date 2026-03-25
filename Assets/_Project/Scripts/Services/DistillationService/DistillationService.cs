using System;
using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models;

namespace Mergistry.Services
{
    public class DistillationService
    {
        // Element pools per floor: floor 0 = 3 elements, floor 1 = 4, floor 2+ = 5
        private static readonly ElementType[] FloorElements0 =
            { ElementType.Ignis, ElementType.Aqua, ElementType.Toxin };
        private static readonly ElementType[] FloorElements1 =
            { ElementType.Ignis, ElementType.Aqua, ElementType.Toxin, ElementType.Lux };
        private static readonly ElementType[] FloorElements2 =
            { ElementType.Ignis, ElementType.Aqua, ElementType.Toxin, ElementType.Lux, ElementType.Umbra };

        // ── Board Generation ─────────────────────────────────────────────────

        public BoardModel GenerateBoard(int seed = 0, int floor = 0)
        {
            var elements = floor switch { 0 => FloorElements0, 1 => FloorElements1, _ => FloorElements2 };
            var board    = new BoardModel();
            var rng      = new Random(seed);

            var positions = new List<(int x, int y)>();
            for (int x = 0; x < BoardModel.Size; x++)
                for (int y = 0; y < BoardModel.Size; y++)
                    positions.Add((x, y));

            // Fisher-Yates shuffle
            for (int i = positions.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (positions[i], positions[j]) = (positions[j], positions[i]);
            }

            // Distribute elements evenly: 16 cells / N elements (cycle via modulo)
            for (int i = 0; i < positions.Count; i++)
            {
                var elem   = elements[i % elements.Length];
                var (x, y) = positions[i];
                board.Set(x, y, CellContent.Ingredient(elem));
            }

            return board;
        }

        // ── Merge ────────────────────────────────────────────────────────────

        public bool CanMerge(BoardModel board, int fromX, int fromY, int toX, int toY)
        {
            if (!IsAdjacent(fromX, fromY, toX, toY)) return false;
            var from = board.Get(fromX, fromY);
            var to   = board.Get(toX, toY);
            return from.Type == CellContentType.Ingredient &&
                   to.Type   == CellContentType.Ingredient;
        }

        /// <summary>Performs merge; updates BoardModel. Returns resulting PotionType and ElementType.</summary>
        public (PotionType potionType, ElementType element) PerformMerge(
            BoardModel board, int fromX, int fromY, int toX, int toY)
        {
            var from = board.Get(fromX, fromY);
            var to   = board.Get(toX, toY);

            var (potionType, element) = GetMergeResult(from.ElementType, to.ElementType);

            board.Set(fromX, fromY, CellContent.Empty());
            board.Set(toX,   toY,   CellContent.Brew(potionType, element, 1));

            return (potionType, element);
        }

        // ── Infuse ───────────────────────────────────────────────────────────

        public bool CanInfuse(BoardModel board, int fromX, int fromY, int toX, int toY)
        {
            if (!IsAdjacent(fromX, fromY, toX, toY)) return false;
            var from = board.Get(fromX, fromY);
            var to   = board.Get(toX, toY);

            if (from.Type != CellContentType.Ingredient) return false;
            if (to.Type   != CellContentType.Brew)       return false;
            if (to.BrewLevel >= 3)                        return false;

            // Base brew: ingredient element must match the brew's element
            if (to.ElementType != ElementType.None)
                return from.ElementType == to.ElementType;

            // Recipe brew: ingredient must be one of the two elements used in the recipe
            foreach (var entry in PotionDatabase.All)
            {
                if (entry.Type == to.PotionType)
                    return from.ElementType == entry.IngredientA ||
                           from.ElementType == entry.IngredientB;
            }
            return false;
        }

        /// <summary>Performs infuse; updates BoardModel. Returns new brew level.</summary>
        public int PerformInfuse(BoardModel board, int fromX, int fromY, int toX, int toY)
        {
            var to       = board.Get(toX, toY);
            int newLevel = to.BrewLevel + 1;

            board.Set(fromX, fromY, CellContent.Empty());
            board.Set(toX, toY, CellContent.Brew(to.PotionType, to.ElementType, newLevel));

            return newLevel;
        }

        // ── Collect Brews ────────────────────────────────────────────────────

        public struct BrewEntry
        {
            public int X, Y;
            public PotionType PotionType;
            public ElementType Element;
            public int Level;
        }

        public List<BrewEntry> CollectBrews(BoardModel board)
        {
            var result = new List<BrewEntry>();
            for (int x = 0; x < BoardModel.Size; x++)
                for (int y = 0; y < BoardModel.Size; y++)
                {
                    var cell = board.Get(x, y);
                    if (cell.Type == CellContentType.Brew)
                        result.Add(new BrewEntry
                            { X = x, Y = y, PotionType = cell.PotionType,
                              Element = cell.ElementType, Level = cell.BrewLevel });
                }
            return result;
        }

        // ── Recipes ──────────────────────────────────────────────────────────

        private static (PotionType type, ElementType element) GetMergeResult(ElementType a, ElementType b)
        {
            if (a == b)
            {
                return a switch
                {
                    ElementType.Ignis => (PotionType.Flame,    ElementType.Ignis),
                    ElementType.Aqua  => (PotionType.Stream,   ElementType.Aqua),
                    ElementType.Toxin => (PotionType.Poison,   ElementType.Toxin),
                    ElementType.Lux   => (PotionType.Radiance, ElementType.Lux),
                    ElementType.Umbra => (PotionType.Gloom,    ElementType.Umbra),
                    _                 => (PotionType.None,     ElementType.None),
                };
            }

            // Mixed recipes (order-independent) ── existing ───────────────────
            if (Matches(a, b, ElementType.Aqua,  ElementType.Ignis)) return (PotionType.Steam,  ElementType.None);
            if (Matches(a, b, ElementType.Ignis, ElementType.Toxin)) return (PotionType.Napalm, ElementType.None);
            if (Matches(a, b, ElementType.Aqua,  ElementType.Toxin)) return (PotionType.Acid,   ElementType.None);

            // Mixed recipes ── A1: Lux combinations ───────────────────────────
            if (Matches(a, b, ElementType.Aqua,  ElementType.Lux))   return (PotionType.Lightning, ElementType.None);
            if (Matches(a, b, ElementType.Ignis, ElementType.Lux))   return (PotionType.Flare,     ElementType.None);
            if (Matches(a, b, ElementType.Toxin, ElementType.Lux))   return (PotionType.Spore,     ElementType.None);

            // Mixed recipes ── A1: Umbra combinations ─────────────────────────
            if (Matches(a, b, ElementType.Ignis, ElementType.Umbra)) return (PotionType.Curse,  ElementType.None);
            if (Matches(a, b, ElementType.Aqua,  ElementType.Umbra)) return (PotionType.Mist,   ElementType.None);
            if (Matches(a, b, ElementType.Toxin, ElementType.Umbra)) return (PotionType.Miasma, ElementType.None);
            if (Matches(a, b, ElementType.Lux,   ElementType.Umbra)) return (PotionType.Chaos,  ElementType.None);

            return (PotionType.None, ElementType.None);
        }

        private static bool Matches(ElementType a, ElementType b, ElementType x, ElementType y) =>
            (a == x && b == y) || (a == y && b == x);

        private static bool IsAdjacent(int x1, int y1, int x2, int y2) =>
            Math.Abs(x1 - x2) + Math.Abs(y1 - y2) == 1;
    }
}
