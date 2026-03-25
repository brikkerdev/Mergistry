using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Services
{
    public class CombatService
    {
        // Returns a fresh combat model with the player placed at (1,1).
        public CombatModel InitCombat()
        {
            var model = new CombatModel();
            model.Player.Position = new Vector2Int(1, 1);
            model.Player.HasMoved = false;
            return model;
        }

        // Returns all cells reachable from the player's current position:
        // horizontal/vertical up to 2 steps + L-shapes (±1,±2) and (±2,±1).
        public List<Vector2Int> GetValidMoves(CombatModel combat)
        {
            var result  = new List<Vector2Int>();
            var pos     = combat.Player.Position;
            var grid    = combat.Grid;

            var offsets = new[]
            {
                // Horizontal / vertical (1 and 2 steps)
                new Vector2Int( 1,  0), new Vector2Int( 2,  0),
                new Vector2Int(-1,  0), new Vector2Int(-2,  0),
                new Vector2Int( 0,  1), new Vector2Int( 0,  2),
                new Vector2Int( 0, -1), new Vector2Int( 0, -2),
                // L-shapes (knight-style)
                new Vector2Int( 1,  2), new Vector2Int(-1,  2),
                new Vector2Int( 1, -2), new Vector2Int(-1, -2),
                new Vector2Int( 2,  1), new Vector2Int(-2,  1),
                new Vector2Int( 2, -1), new Vector2Int(-2, -1),
            };

            foreach (var off in offsets)
            {
                var target = pos + off;
                if (grid.IsInBounds(target.x, target.y))
                    result.Add(target);
            }

            return result;
        }
    }
}
