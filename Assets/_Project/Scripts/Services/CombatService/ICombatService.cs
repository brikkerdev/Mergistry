using System.Collections.Generic;
using Mergistry.Models;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public interface ICombatService
    {
        CombatModel InitCombat();
        void SpawnEnemies(CombatModel model, int fightIndex);
        List<Vector2Int> GetValidMoves(CombatModel combat);
        bool ThrowPotion(CombatModel model, InventoryModel inventory, int slotIndex, Vector2Int targetCell);
        void StartNextPlayerTurn(CombatModel model, InventoryModel inventory);
        void HealOnSkip(CombatModel model);
        PushResult PushEnemy(CombatModel model, EnemyCombatModel enemy, Vector2Int direction);
        void ApplyZoneEffects(CombatModel model, IDamageService damageService);
        List<ZoneInstance> TickZones(GridModel grid);
        void TickStatuses(CombatModel model, IDamageService damageService);
    }
}
