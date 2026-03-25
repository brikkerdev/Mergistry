using Mergistry.Data;

namespace Mergistry.Models
{
    public class CellContent
    {
        public CellContentType Type;
        public ElementType ElementType;
        public PotionType PotionType;
        public int BrewLevel;

        public static CellContent Empty() =>
            new() { Type = CellContentType.Empty };

        public static CellContent Ingredient(ElementType element) =>
            new() { Type = CellContentType.Ingredient, ElementType = element };

        public static CellContent Brew(PotionType potionType, ElementType element, int level = 1) =>
            new() { Type = CellContentType.Brew, PotionType = potionType, ElementType = element, BrewLevel = level };
    }
}
