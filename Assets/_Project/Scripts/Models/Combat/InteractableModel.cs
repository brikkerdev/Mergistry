using UnityEngine;

namespace Mergistry.Models.Combat
{
    /// <summary>
    /// A6: Represents a static interactable on the boss arena.
    /// Pillar  — impassable, indestructible.
    /// Cauldron — passable, HP=3; destroying all cauldrons removes boss teleport.
    /// </summary>
    public class InteractableModel
    {
        public InteractableType Type        { get; set; }
        public Vector2Int       Position    { get; set; }
        public bool             IsPassable  { get; set; }
        public int              HP          { get; set; }
        public int              MaxHP       { get; set; }
        public bool             IsDestroyed => HP <= 0;

        public InteractableModel(InteractableType type, Vector2Int pos,
                                 bool isPassable = false, int hp = -1)
        {
            Type       = type;
            Position   = pos;
            IsPassable = isPassable;
            HP         = hp;
            MaxHP      = hp;
        }
    }
}
