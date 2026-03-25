using System;
using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models;

namespace Mergistry.Services
{
    public class DistillationService
    {
        private static readonly ElementType[] BaseElements =
            { ElementType.Ignis, ElementType.Aqua, ElementType.Toxin };

        // ── Board Generation ─────────────────────────────────────────────────

        public BoardModel GenerateBoard(int seed = 0)
        {
            var board = new BoardModel();
            var rng   = new Random(seed);

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

            // Distribute 3 elements evenly across 16 cells
            for (int i = 0; i < positions.Count; i++)
            {
                var elem     = BaseElements[i % BaseElements.Length];
                var (x, y)   = positions[i];
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

            // Mixed brews (ElementType.None) cannot be infused
            return to.ElementType != ElementType.None &&
                   from.ElementType == to.ElementType;
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
                    ElementType.Ignis => (PotionType.Flame,  ElementType.Ignis),
                    ElementType.Aqua  => (PotionType.Stream, ElementType.Aqua),
                    ElementType.Toxin => (PotionType.Poison, ElementType.Toxin),
                    _                 => (PotionType.None,   ElementType.None),
                };
            }

            // Mixed recipes (order-independent)
            if (Matches(a, b, ElementType.Aqua,  ElementType.Ignis)) return (PotionType.Steam,  ElementType.None);
            if (Matches(a, b, ElementType.Ignis, ElementType.Toxin)) return (PotionType.Napalm, ElementType.None);
            if (Matches(a, b, ElementType.Aqua,  ElementType.Toxin)) return (PotionType.Acid,   ElementType.None);

            return (PotionType.None, ElementType.None);
        }

        private static bool Matches(ElementType a, ElementType b, ElementType x, ElementType y) =>
            (a == x && b == y) || (a == y && b == x);

        private static bool IsAdjacent(int x1, int y1, int x2, int y2) =>
            Math.Abs(x1 - x2) + Math.Abs(y1 - y2) == 1;
    }
}
