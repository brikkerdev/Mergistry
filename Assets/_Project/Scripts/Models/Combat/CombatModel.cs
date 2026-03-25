using System.Collections.Generic;
using Mergistry.Data;
using UnityEngine;

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

        // A6: boss combat fields
        public bool                   IsBossFight        { get; set; } = false;
        public BossPhase              CurrentBossPhase   { get; set; } = BossPhase.Phase1;
        public EnemyCombatModel       BossEnemy          { get; set; }   // NOT in Enemies list
        public List<InteractableModel> Interactables     { get; } = new List<InteractableModel>();
        public bool                   CauldronsDestroyed { get; set; } = false;

        // A7: room modifier
        public RoomModifierType   RoomModifier { get; set; } = RoomModifierType.None;
        public List<Vector2Int>   PitPositions { get; }      = new List<Vector2Int>();

        private int _nextEntityId;
        public int NextEntityId() => _nextEntityId++;
    }
}
