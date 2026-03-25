using System.Collections.Generic;
using Mergistry.Data;

namespace Mergistry.Models.Combat
{
    public class CombatModel
    {
        public GridModel              Grid    { get; } = new GridModel();
        public PlayerCombatModel      Player  { get; } = new PlayerCombatModel();
        public List<EnemyCombatModel> Enemies { get; } = new List<EnemyCombatModel>();

        // A3: recently killed enemies available for Necromancer to revive
        public List<EnemyCombatModel> Graveyard { get; } = new List<EnemyCombatModel>();

        // A3: last potion thrown by player — used by MirrorSlime
        public bool       HasLastThrownPotion   { get; set; }
        public PotionType LastThrownPotionType  { get; set; }
        public int        LastThrownPotionLevel { get; set; }

        private int _nextEntityId;
        public int NextEntityId() => _nextEntityId++;
    }
}
