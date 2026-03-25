namespace Mergistry.Models.Combat
{
    public class GridModel
    {
        public const int Width  = 5;
        public const int Height = 5;

        public bool IsInBounds(int x, int y) =>
            x >= 0 && x < Width && y >= 0 && y < Height;
    }
}
