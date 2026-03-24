namespace Mergistry.Models
{
    public class BoardModel
    {
        public const int Size = 4;
        private readonly CellContent[,] _cells = new CellContent[Size, Size];

        public BoardModel()
        {
            for (int x = 0; x < Size; x++)
                for (int y = 0; y < Size; y++)
                    _cells[x, y] = CellContent.Empty();
        }

        public CellContent Get(int x, int y) => _cells[x, y];
        public void Set(int x, int y, CellContent content) => _cells[x, y] = content;
        public bool IsInBounds(int x, int y) => x >= 0 && x < Size && y >= 0 && y < Size;
    }
}
