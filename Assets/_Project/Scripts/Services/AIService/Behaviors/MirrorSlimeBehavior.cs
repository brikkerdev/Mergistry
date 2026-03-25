using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class MirrorSlimeBehavior : IEnemyBehavior
    {
        private readonly IDamageService _damageService;

        public MirrorSlimeBehavior(IDamageService damageService)
        {
            _damageService = damageService;
        }

        public EnemyIntent DetermineIntent(EnemyCombatModel enemy, CombatModel model)
        {
            if (model.HasLastThrownPotion)
            {
                // Copy the last potion the player threw, target player's current position
                var aoe    = _damageService.GetAffectedCells(
                                 model.LastThrownPotionType,
                                 model.Player.Position,
                                 model.Grid);
                int damage = _damageService.GetDamage(
                                 model.LastThrownPotionType,
                                 model.LastThrownPotionLevel);

                // Cache the copy on the enemy model for view display
                enemy.HasCopiedPotion   = true;
                enemy.CopiedPotionType  = model.LastThrownPotionType;
                enemy.CopiedPotionLevel = model.LastThrownPotionLevel;

                return new EnemyIntent
                {
                    Type        = IntentType.Attack,
                    AttackCells = aoe,
                    Damage      = damage
                };
            }
            else
            {
                // No potion copied yet — move toward player
                var target = enemy.HasStatus(StatusEffectType.Slow)
                    ? enemy.Position
                    : AIHelper.StepToward(enemy, model);

                return new EnemyIntent
                {
                    Type           = IntentType.Move,
                    TargetPosition = target,
                    AttackCells    = new List<Vector2Int>()
                };
            }
        }
    }
}
