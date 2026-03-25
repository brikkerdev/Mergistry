using Mergistry.Models.Combat;

namespace Mergistry.Services
{
    public interface IEnemyBehavior
    {
        EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model);
    }
}
