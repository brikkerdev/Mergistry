namespace Mergistry.Data
{
    /// <summary>
    /// A7: Optional modifier that changes the rules of a combat room.
    /// </summary>
    public enum RoomModifierType
    {
        None,
        Flooded,   // entire grid = Water zone; Lightning ×2, Fire ×0.5
        Burning,   // 1-2 random cells ignite each turn
        Pits       // 2-3 impassable pits; push enemy into pit = instant kill
    }
}
