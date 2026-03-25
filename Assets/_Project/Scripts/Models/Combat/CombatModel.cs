using System.Collections.Generic;

namespace Mergistry.Models.Combat
{
    public class CombatModel
    {
        public GridModel              Grid    { get; } = new GridModel();
        public PlayerCombatModel      Player  { get; } = new PlayerCombatModel();
        public List<EnemyCombatModel> Enemies { get; } = new List<EnemyCombatModel>();

        private int _nextEntityId;
        public int NextEntityId() => _nextEntityId++;
    }
}
