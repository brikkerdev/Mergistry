using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Data;
using Mergistry.Events;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    /// <summary>
    /// Pure logic service: AoE patterns, throw range, and damage application.
    /// A2: armor system — ArmorPoints absorb damage first.
    /// </summary>
    public class DamageService : IDamageService
    {
        private readonly IRelicService _relicService;

        public DamageService(IRelicService relicService) { _relicService = relicService; }

        // ── Throw range ─────────────────────────────────────────────────────────

        /// <summary>All grid cells within Manhattan distance ≤ range of the player (excluding player's cell).</summary>
        public List<Vector2Int> GetValidThrowRange(GridModel grid, Vector2Int playerPos, int range = 3)
        {
            var result = new List<Vector2Int>();
            for (int x = 0; x < GridModel.Width; x++)
            for (int y = 0; y < GridModel.Height; y++)
            {
                var cell = new Vector2Int(x, y);
                if (cell != playerPos && Manhattan(cell, playerPos) <= range)
                    result.Add(cell);
            }
            return result;
        }

        // ── AoE patterns ────────────────────────────────────────────────────────

        /// <summary>Returns grid cells hit by the AoE of the given potion type at target.</summary>
        public List<Vector2Int> GetAffectedCells(PotionType type, Vector2Int target, GridModel grid)
        {
            var cells = type switch
            {
                // ── MVP potions ───────────────────────────────────────────────
                PotionType.Flame     => Cross(target, 1, grid),
                PotionType.Stream    => LineOfN(target, 4, grid),
                PotionType.Poison    => Block2x2(target, grid),
                PotionType.Steam     => Block2x2(target, grid),
                PotionType.Napalm    => Cross(target, 1, grid),
                PotionType.Acid      => Block2x2(target, grid),
                // ── A1: Lux/Umbra base brews ──────────────────────────────────
                PotionType.Radiance  => Box3x3(target, grid),
                PotionType.Gloom     => new List<Vector2Int> { target },
                // ── A1: Lux recipe brews ──────────────────────────────────────
                PotionType.Lightning => LineOfN(target, 5, grid),
                PotionType.Flare     => Box3x3(target, grid),
                PotionType.Spore     => Cross(target, 1, grid),
                // ── A1: Umbra recipe brews ────────────────────────────────────
                PotionType.Curse     => new List<Vector2Int> { target },
                PotionType.Mist      => new List<Vector2Int> { target },
                PotionType.Miasma    => Box3x3(target, grid),
                PotionType.Chaos     => RandomAoE(target, grid),
                _                    => new List<Vector2Int> { target }
            };

            // A5: Lens — expand AoE by 1 cell in each direction
            if (_relicService != null && _relicService.HasRelic(RelicType.Lens))
            {
                cells = ExpandAoE(cells, grid);
            }
            return cells;
        }

        /// <summary>Base damage dealt by a potion at the given level.</summary>
        public int GetDamage(PotionType type, int level)
        {
            float scale = level == 1 ? 1.0f : level == 2 ? 1.5f : 2.0f;
            int baseDamage = type switch
            {
                // ── MVP potions ───────────────────────────────────────────────
                PotionType.Flame     => 3,
                PotionType.Stream    => 3,
                PotionType.Poison    => 2,
                PotionType.Steam     => 0,
                PotionType.Napalm    => 3,
                PotionType.Acid      => 2,
                // ── A1: Lux/Umbra base brews ──────────────────────────────────
                PotionType.Radiance  => 1,
                PotionType.Gloom     => 5,
                // ── A1: Lux recipe brews ──────────────────────────────────────
                PotionType.Lightning => 3,
                PotionType.Flare     => 3,
                PotionType.Spore     => 3,
                // ── A1: Umbra recipe brews ────────────────────────────────────
                PotionType.Curse     => 0,
                PotionType.Mist      => 0,
                PotionType.Miasma    => 2,
                PotionType.Chaos     => Random.Range(1, 5),
                _                    => 1
            };
            int damage = Mathf.RoundToInt(baseDamage * scale);

            // A5: Prism — combo damage ×1.5
            if (_relicService != null && _relicService.HasRelic(RelicType.Prism))
                damage = Mathf.RoundToInt(damage * 1.5f);

            return damage;
        }

        // ── Damage application ──────────────────────────────────────────────────

        /// <summary>
        /// Applies damage to an enemy. ArmorPoints absorb first; remaining goes to HP.
        /// Publishes EnemyDamagedEvent (and EnemyDiedEvent / ArmorRemovedEvent as needed).
        /// </summary>
        public void ApplyDamage(EnemyCombatModel enemy, int damage)
        {
            int remaining = damage;

            // Armor absorbs first
            if (enemy.ArmorPoints > 0)
            {
                int absorbed = Mathf.Min(enemy.ArmorPoints, remaining);
                enemy.ArmorPoints -= absorbed;
                remaining         -= absorbed;

                if (enemy.ArmorPoints == 0)
                    EventBus.Publish(new ArmorRemovedEvent { EntityId = enemy.EntityId });
            }

            // Remaining damage goes to HP
            enemy.HP -= remaining;
            if (enemy.HP < 0) enemy.HP = 0;

            EventBus.Publish(new EnemyDamagedEvent
            {
                EntityId    = enemy.EntityId,
                Damage      = damage,
                HPRemaining = enemy.HP
            });

            if (enemy.HP <= 0)
                EventBus.Publish(new EnemyDiedEvent { EntityId = enemy.EntityId });

            Debug.Log($"[DamageService] Enemy {enemy.EntityId} ({enemy.Type}) took {damage} dmg " +
                      $"→ armor={enemy.ArmorPoints}, HP={enemy.HP}/{enemy.MaxHP}");
        }

        /// <summary>Instantly removes all armor from an enemy (Acid effect).</summary>
        public void RemoveArmor(EnemyCombatModel enemy)
        {
            if (enemy.ArmorPoints <= 0) return;
            enemy.ArmorPoints = 0;
            EventBus.Publish(new ArmorRemovedEvent { EntityId = enemy.EntityId });
            Debug.Log($"[DamageService] Enemy {enemy.EntityId} armor stripped by Acid");
        }

        /// <summary>Applies damage to the player, publishing damage event.</summary>
        public void ApplyDamageToPlayer(PlayerCombatModel player, int damage)
        {
            player.HP -= damage;
            if (player.HP < 0) player.HP = 0;

            EventBus.Publish(new PlayerDamagedEvent
            {
                Damage      = damage,
                HPRemaining = player.HP
            });

            Debug.Log($"[DamageService] Player took {damage} dmg → HP={player.HP}/{player.MaxHP}");
        }

        // ── AoE helpers ─────────────────────────────────────────────────────────

        /// <summary>Expands AoE by adding all adjacent cells (Manhattan 1) of existing cells.</summary>
        private static List<Vector2Int> ExpandAoE(List<Vector2Int> original, GridModel grid)
        {
            var expanded = new HashSet<Vector2Int>(original);
            foreach (var cell in original)
            {
                var neighbors = new[]
                {
                    new Vector2Int(cell.x + 1, cell.y),
                    new Vector2Int(cell.x - 1, cell.y),
                    new Vector2Int(cell.x, cell.y + 1),
                    new Vector2Int(cell.x, cell.y - 1)
                };
                foreach (var n in neighbors)
                    if (grid.IsInBounds(n.x, n.y))
                        expanded.Add(n);
            }
            return new List<Vector2Int>(expanded);
        }

        private static List<Vector2Int> Cross(Vector2Int center, int radius, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            AddIfInBounds(cells, center, grid);
            for (int i = 1; i <= radius; i++)
            {
                AddIfInBounds(cells, new Vector2Int(center.x + i, center.y), grid);
                AddIfInBounds(cells, new Vector2Int(center.x - i, center.y), grid);
                AddIfInBounds(cells, new Vector2Int(center.x, center.y + i), grid);
                AddIfInBounds(cells, new Vector2Int(center.x, center.y - i), grid);
            }
            return cells;
        }

        private static List<Vector2Int> Row(int row, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int x = 0; x < GridModel.Width; x++)
                cells.Add(new Vector2Int(x, row));
            return cells;
        }

        private static List<Vector2Int> Column(int col, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int y = 0; y < GridModel.Height; y++)
                cells.Add(new Vector2Int(col, y));
            return cells;
        }

        private static List<Vector2Int> Block2x2(Vector2Int origin, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int dx = 0; dx <= 1; dx++)
            for (int dy = 0; dy <= 1; dy++)
                AddIfInBounds(cells, new Vector2Int(origin.x + dx, origin.y + dy), grid);
            return cells;
        }

        private static List<Vector2Int> Box3x3(Vector2Int center, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                AddIfInBounds(cells, new Vector2Int(center.x + dx, center.y + dy), grid);
            return cells;
        }

        /// <summary>All cells within Manhattan distance ≤ radius (diamond/rhombus shape).</summary>
        private static List<Vector2Int> Diamond(Vector2Int center, int radius, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            for (int dx = -radius; dx <= radius; dx++)
            for (int dy = -radius; dy <= radius; dy++)
                if (Mathf.Abs(dx) + Mathf.Abs(dy) <= radius)
                    AddIfInBounds(cells, new Vector2Int(center.x + dx, center.y + dy), grid);
            return cells;
        }

        /// <summary>
        /// N cells along the row centered on target, clamped to grid bounds.
        /// Used for Stream (4 cells) and Lightning (5 cells).
        /// </summary>
        private static List<Vector2Int> LineOfN(Vector2Int target, int count, GridModel grid)
        {
            var cells = new List<Vector2Int>();
            int half  = count / 2;
            int startX = target.x - half;
            for (int i = 0; i < count; i++)
            {
                var c = new Vector2Int(startX + i, target.y);
                AddIfInBounds(cells, c, grid);
            }
            return cells;
        }

        /// <summary>Entire row + entire column through target (cross of full lines).</summary>
        private static List<Vector2Int> RowAndColumn(Vector2Int target, GridModel grid)
        {
            var cells = Row(target.y, grid);
            for (int y = 0; y < GridModel.Height; y++)
            {
                var c = new Vector2Int(target.x, y);
                if (!cells.Contains(c)) cells.Add(c);
            }
            return cells;
        }

        /// <summary>Randomly picks one of the available AoE patterns at runtime (Chaos potion).</summary>
        private List<Vector2Int> RandomAoE(Vector2Int target, GridModel grid)
        {
            int roll = Random.Range(0, 6);
            return roll switch
            {
                0 => Cross(target, 1, grid),
                1 => Row(target.y, grid),
                2 => Block2x2(target, grid),
                3 => Cross(target, 2, grid),
                4 => Box3x3(target, grid),
                _ => Column(target.x, grid),
            };
        }

        private static void AddIfInBounds(List<Vector2Int> list, Vector2Int cell, GridModel grid)
        {
            if (grid.IsInBounds(cell.x, cell.y))
                list.Add(cell);
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
