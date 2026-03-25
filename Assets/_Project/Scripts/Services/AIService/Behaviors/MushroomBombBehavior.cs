using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class MushroomBombBehavior : IEnemyBehavior
    {
        public EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model)
        {
            if (enemy.BombTimer > 0)
            {
                int display = enemy.BombTimer;
                enemy.BombTimer--;
                return new EnemyIntent
                {
                    Type           = IntentType.Countdown,
                    CountdownValue = display,
                    AttackCells    = new List<Vector2Int>()
                };
            }
            else
            {
                return new EnemyIntent
                {
                    Type        = IntentType.Explode,
                    AttackCells = AIHelper.Box3x3(enemy.Position, model.Grid),
                    Damage      = 5
                };
            }
        }
    }
}
