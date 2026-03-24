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

        public BoardModel GenerateBoard(int seed = 0)
        {
            var board = new BoardModel();
            var rng = new Random(seed);

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
                var elem = BaseElements[i % BaseElements.Length];
                var (x, y) = positions[i];
                board.Set(x, y, CellContent.Ingredient(elem));
            }

            return board;
        }
    }
}
