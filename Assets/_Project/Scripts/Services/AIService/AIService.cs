using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models.Combat;
using Mergistry.Services.Bosses;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Determines and executes enemy intents for each combat turn.
    /// A2: MushroomBomb, MagnetGolem, ArmoredBeetle.
    /// A3: MirrorSlime, Phantom, Necromancer.
    /// A6: SpiderQueen, IronGolem, RenegadeAlchemist (boss behaviors). SummonMinions execution.
    /// </summary>
    public class AIService : IAIService
    {
        private readonly IDamageService _damageService;
        private readonly Dictionary<EnemyType, IEnemyBehavior> _behaviors;

        public AIService(IDamageService damageService)
        {
            _damageService = damageService;

            _behaviors = new Dictionary<EnemyType, IEnemyBehavior>
            {
                { EnemyType.Skeleton,           new SkeletonBehavior() },
                { EnemyType.Spider,             new SpiderBehavior() },
                { EnemyType.MushroomBomb,       new MushroomBombBehavior() },
                { EnemyType.MagnetGolem,        new MagnetGolemBehavior() },
                { EnemyType.ArmoredBeetle,      new ArmoredBeetleBehavior() },
                { EnemyType.MirrorSlime,        new MirrorSlimeBehavior(_damageService) },
                { EnemyType.Phantom,            new PhantomBehavior() },
                { EnemyType.Necromancer,        new NecromancerBehavior() },
                // A6: bosses
                { EnemyType.SpiderQueen,        new SpiderQueenBehavior() },
                { EnemyType.IronGolem,          new IronGolemBehavior() },
                { EnemyType.RenegadeAlchemist,  new RenegadeAlchemistBehavior() },
            };
        }

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>Calculates intent for every living enemy based on the current model state.</summary>
        public void DetermineIntents(CombatModel model)
        {
            foreach (var enemy in model.Enemies)
            {
                if (enemy.IsDead) continue;

                // A3: Stunned enemies skip their turn
                if (enemy.HasStatus(StatusEffectType.Stun))
                {
                    enemy.Intent = null;
                    continue;
                }

                enemy.Intent = _behaviors.TryGetValue(enemy.Type, out var behavior)
                    ? behavior.DetermineIntent(enemy, model)
                    : null;
            }
        }

        /// <summary>Executes each living enemy's stored intent, applying movement and damage.</summary>
        public void ExecuteIntents(CombatModel model)
        {
            // Snapshot the list — Necromancer Revive adds enemies mid-loop
            var snapshot = new List<EnemyCombatModel>(model.Enemies);
            foreach (var enemy in snapshot)
            {
                if (enemy.IsDead || enemy.Intent == null) continue;
                ExecuteEnemyIntent(enemy, model);
            }
        }

        // ── Execution ───────────────────────────────────────────────────────────

        private void ExecuteEnemyIntent(EnemyCombatModel enemy, CombatModel model)
        {
            var intent = enemy.Intent;

            switch (intent.Type)
            {
                case IntentType.Move:
                {
                    var target = intent.TargetPosition;
                    if (target == model.Player.Position) return;
                    foreach (var other in model.Enemies)
                        if (!other.IsDead && other.EntityId != enemy.EntityId && other.Position == target)
                            return;

                    enemy.Position = target;
                    Debug.Log($"[AIService] {enemy.Type}({enemy.EntityId}) moved to {enemy.Position}");
                    break;
                }

                case IntentType.Attack:
                {
                    bool hit = intent.AttackCells.Contains(model.Player.Position);
                    if (hit)
                    {
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                        // Spider applies Slow to the player on hit
                        if (enemy.Type == EnemyType.Spider)
                        {
                            var existing = model.Player.StatusEffects.Find(s => s.Type == StatusEffectType.Slow);
                            if (existing != null)
                                existing.Duration = System.Math.Max(existing.Duration, 1);
                            else
                                model.Player.StatusEffects.Add(new StatusEffect(StatusEffectType.Slow, 1));
                        }
                    }
                    Debug.Log($"[AIService] {enemy.Type}({enemy.EntityId}) attacked — player hit={hit}");
                    break;
                }

                case IntentType.Countdown:
                    Debug.Log($"[AIService] MushroomBomb({enemy.EntityId}) countdown...");
                    break;

                case IntentType.Explode:
                {
                    foreach (var other in model.Enemies)
                    {
                        if (other.IsDead || other.EntityId == enemy.EntityId) continue;
                        if (intent.AttackCells.Contains(other.Position))
                            _damageService.ApplyDamage(other, intent.Damage);
                    }
                    if (intent.AttackCells.Contains(model.Player.Position))
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                    enemy.HP = 0;

                    EventBus.Publish(new BombExplodedEvent
                    {
                        EntityId      = enemy.EntityId,
                        AffectedCells = intent.AttackCells
                    });

                    Debug.Log($"[AIService] MushroomBomb({enemy.EntityId}) EXPLODED!");
                    break;
                }

                case IntentType.Pull:
                {
                    var pulled = AIHelper.StepPlayerToward(model.Player.Position, enemy.Position, model);
                    model.Player.Position = pulled;

                    if (AIHelper.Manhattan(pulled, enemy.Position) <= 1)
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                    Debug.Log($"[AIService] MagnetGolem({enemy.EntityId}) pulled player to {pulled}");
                    break;
                }

                case IntentType.Teleport:
                {
                    var from = enemy.Position;
                    enemy.Position = intent.TargetPosition;

                    EventBus.Publish(new EnemyTeleportedEvent
                    {
                        EntityId = enemy.EntityId,
                        FromPos  = from,
                        ToPos    = enemy.Position
                    });

                    // Attack after teleport if now adjacent
                    if (AIHelper.Manhattan(enemy.Position, model.Player.Position) <= 1)
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);

                    Debug.Log($"[AIService] Phantom({enemy.EntityId}) teleported {from} → {enemy.Position}");
                    break;
                }

                // A6: boss summons minions
                case IntentType.SummonMinions:
                {
                    foreach (var pos in intent.SpawnPositions)
                    {
                        var minion = new EnemyCombatModel(model.NextEntityId(), intent.MinionType, pos, intent.MinionHP);
                        model.Enemies.Add(minion);
                        EventBus.Publish(new EnemySpawnedEvent
                        {
                            EntityId = minion.EntityId,
                            Type     = minion.Type,
                            Position = pos
                        });
                        Debug.Log($"[AIService] Boss summoned {minion.Type} at {pos}");
                    }
                    break;
                }

                // A6: boss 2×2 area slam
                case IntentType.AreaAttack:
                {
                    if (intent.AttackCells.Contains(model.Player.Position))
                        _damageService.ApplyDamageToPlayer(model.Player, intent.Damage);
                    Debug.Log($"[AIService] AreaAttack — player hit={intent.AttackCells.Contains(model.Player.Position)}");
                    break;
                }

                case IntentType.Revive:
                {
                    var deadEnemy = model.Graveyard.Find(e => e.EntityId == intent.ReviveEntityId);
                    if (deadEnemy == null) break;

                    model.Graveyard.Remove(deadEnemy);
                    deadEnemy.HP       = 1;
                    deadEnemy.Position = intent.TargetPosition;
                    model.Enemies.Add(deadEnemy);

                    EventBus.Publish(new EnemyRevivedEvent
                    {
                        EntityId = deadEnemy.EntityId,
                        Position = deadEnemy.Position
                    });

                    Debug.Log($"[AIService] Necromancer({enemy.EntityId}) revived enemy {deadEnemy.EntityId} ({deadEnemy.Type}) at {deadEnemy.Position}");
                    break;
                }
            }
        }
    }
}
