namespace Mergistry.Data
{
    public enum PotionType
    {
        None,
        // Base brews (same element)
        Flame,   // Ignis + Ignis
        Stream,  // Aqua + Aqua
        Poison,  // Toxin + Toxin
        // Recipe brews (mixed elements)
        Steam,   // Aqua + Ignis
        Napalm,  // Ignis + Toxin
        Acid,    // Aqua + Toxin
    }
}
