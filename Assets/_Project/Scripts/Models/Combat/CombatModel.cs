namespace Mergistry.Models.Combat
{
    public class CombatModel
    {
        public GridModel          Grid   { get; } = new GridModel();
        public PlayerCombatModel  Player { get; } = new PlayerCombatModel();
    }
}
