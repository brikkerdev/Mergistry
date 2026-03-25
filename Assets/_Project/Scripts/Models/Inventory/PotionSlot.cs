using Mergistry.Data;

namespace Mergistry.Models
{
    public class PotionSlot
    {
        public PotionType Type;
        public int Level;
        public int CooldownRemaining;

        public bool IsEmpty => Type == PotionType.None;

        public static PotionSlot Empty() => new() { Type = PotionType.None };
    }
}
