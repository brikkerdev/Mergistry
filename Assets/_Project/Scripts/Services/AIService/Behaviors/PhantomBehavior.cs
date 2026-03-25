using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class PhantomBehavior : IEnemyBehavior
    {
        public EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model)
        {
            // Show all valid teleport positions (ring radius 1-2 around player)
            var ring = AIHelper.GetRingCells(model.Player.Position, 1, 2, model.Grid, model);

            // Pick an actual teleport destination
            var dest = ring.Count > 0
                ? ring[Random.Range(0, ring.Count)]
                : enemy.Position;

            return new EnemyIntent
            {
                Type           = IntentType.Teleport,
                TargetPosition = dest,
                AttackCells    = ring,   // shown as intent highlight on grid
                Damage         = 2
            };
        }
    }
}
