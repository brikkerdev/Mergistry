using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public interface IDamageService
    {
        List<Vector2Int> GetValidThrowRange(GridModel grid, Vector2Int playerPos, int range = 3);
        List<Vector2Int> GetAffectedCells(PotionType type, Vector2Int target, GridModel grid);
        int GetDamage(PotionType type, int level);
        void ApplyDamage(EnemyCombatModel enemy, int damage);
        void RemoveArmor(EnemyCombatModel enemy);
        void ApplyDamageToPlayer(PlayerCombatModel player, int damage);
    }
}
