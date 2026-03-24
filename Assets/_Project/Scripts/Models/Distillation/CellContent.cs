using Mergistry.Data;

namespace Mergistry.Models
{
    public class CellContent
    {
        public CellContentType Type;
        public ElementType ElementType;
        public int BrewLevel;

        public static CellContent Empty() =>
            new() { Type = CellContentType.Empty };

        public static CellContent Ingredient(ElementType element) =>
            new() { Type = CellContentType.Ingredient, ElementType = element };
    }
}
