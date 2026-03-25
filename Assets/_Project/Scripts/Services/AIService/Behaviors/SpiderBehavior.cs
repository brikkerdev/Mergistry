using Mergistry.Models.Combat;

namespace Mergistry.Services
{
    public class SpiderBehavior : IEnemyBehavior
    {
        public EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model)
        {
            var dir         = AIHelper.LineDirection(enemy.Position, model.Player.Position);
            var attackCells = AIHelper.LineCells(enemy.Position, dir, 3, model.Grid);

            return new EnemyIntent
            {
                Type        = IntentType.Attack,
                AttackCells = attackCells,
                Damage      = 1
            };
        }
    }
}
