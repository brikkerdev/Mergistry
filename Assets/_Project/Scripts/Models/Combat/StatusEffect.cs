namespace Mergistry.Models.Combat
{
    public class StatusEffect
    {
        public StatusEffectType Type     { get; set; }
        public int              Duration { get; set; }

        public StatusEffect(StatusEffectType type, int duration)
        {
            Type     = type;
            Duration = duration;
        }
    }
}
