namespace Mergistry.Models.Combat
{
    /// <summary>
    /// A7: Identifies which potion+zone combo was triggered.
    /// </summary>
    public enum ComboType
    {
        None,
        LightningWater,  // Lightning + Water zone → ×2 damage to ALL enemies on water
        FirePoison,      // Fire/Napalm + Poison zone → Poison zone expands to adjacent cells
        StreamIce        // Stream + Water zone → Water converts to Ice, freezes enemies on water
    }
}
