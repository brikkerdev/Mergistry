using System;
using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Handles swipe input on the combat grid.
    ///
    /// Movement:  drag starting near player → ghost preview → OnMoveRequested.
    /// Tap:       short drag not near player → OnGridTapped.
    /// Push:      drag starting near an adjacent enemy → OnPushRequested(entityId, direction).
    /// </summary>
    public class CombatInputController : MonoBehaviour
    {
        private const float ActivationRadius = 1.0f;
        private const float SnapRadius       = 1.2f;
        private const float MinDragDelta     = 0.25f;
        private const float TapThreshold     = 0.15f;

        private GridView   _gridView;
        private PlayerView _playerView;
        private CombatModel _model;

        // Delegate set by CombatState to provide valid moves without a direct service reference.
        public Func<CombatModel, List<Vector2Int>> GetValidMovesFunc;

        private bool             _active;
        private Vector3          _anyDragStart;

        // Move tracking
        private bool             _tracking;
        private bool             _wasMoveTracking;
        private Vector3          _dragStart;
        private Vector2Int?      _ghostTarget;
        private List<Vector2Int> _validMoves;

        // Push tracking
        private int     _pushTargetId = -1;  // EntityId of enemy being pushed
        private Vector3 _pushDragStart;

        public Action<Vector2Int>      OnMoveRequested;
        public Action<Vector2Int>      OnGridTapped;
        public Action<int, Vector2Int> OnPushRequested; // (entityId, direction)

        // ── Init ──────────────────────────────────────────────────────────────

        public void Initialize(GridView gridView, PlayerView playerView, CombatModel model)
        {
            _gridView   = gridView;
            _playerView = playerView;
            _model      = model;
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (!active) CancelTracking();
        }

        // ── Event wiring ──────────────────────────────────────────────────────

        private void OnEnable()
        {
            EventBus.Subscribe<DragStartEvent>(OnDragStart);
            EventBus.Subscribe<DragUpdateEvent>(OnDragUpdate);
            EventBus.Subscribe<DragEndEvent>(OnDragEnd);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DragStartEvent>(OnDragStart);
            EventBus.Unsubscribe<DragUpdateEvent>(OnDragUpdate);
            EventBus.Unsubscribe<DragEndEvent>(OnDragEnd);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void OnDragStart(DragStartEvent e)
        {
            _anyDragStart    = e.WorldPosition;
            _wasMoveTracking = false;
            _pushTargetId    = -1;

            if (!_active || _model == null) return;

            // --- Push: drag starts near an adjacent living enemy ---
            var adjacentEnemy = FindAdjacentEnemyAt(e.WorldPosition);
            if (adjacentEnemy != null)
            {
                _pushTargetId    = adjacentEnemy.EntityId;
                _pushDragStart   = e.WorldPosition;
                return; // push takes priority over move
            }

            // --- Move: drag starts near player and player hasn't moved ---
            if (_model.Player.HasMoved) return;

            var playerWorld = _gridView.GridToWorld(_model.Player.Position);
            if (Vector3.Distance(e.WorldPosition, playerWorld) > ActivationRadius) return;

            _tracking        = true;
            _wasMoveTracking = true;
            _dragStart       = e.WorldPosition;
            _validMoves      = GetValidMovesFunc?.Invoke(_model) ?? new List<Vector2Int>();
            _gridView.SetHighlights(_validMoves);
        }

        private void OnDragUpdate(DragUpdateEvent e)
        {
            if (_pushTargetId >= 0) return; // no visual feedback during push drag

            if (!_tracking) return;

            if (Vector3.Distance(e.WorldPosition, _dragStart) < MinDragDelta)
            {
                _ghostTarget = null;
                _playerView.HideGhost();
                return;
            }

            var best = FindClosestValid(e.WorldPosition);
            if (best.HasValue)
            {
                _ghostTarget = best;
                _playerView.ShowGhost(_gridView.GridToWorld(best.Value));
            }
            else
            {
                _ghostTarget = null;
                _playerView.HideGhost();
            }
        }

        private void OnDragEnd(DragEndEvent e)
        {
            bool wasTap = Vector3.Distance(e.WorldPosition, _anyDragStart) < TapThreshold;

            // --- Push release ---
            if (_pushTargetId >= 0)
            {
                var delta = e.WorldPosition - _pushDragStart;
                if (delta.magnitude >= MinDragDelta)
                {
                    var dir = ToCardinalDirection(delta);
                    OnPushRequested?.Invoke(_pushTargetId, dir);
                }
                _pushTargetId = -1;
                return;
            }

            // --- Move release ---
            if (_tracking)
            {
                _tracking = false;
                _playerView.HideGhost();
                _gridView.ClearHighlights();

                if (_ghostTarget.HasValue)
                    OnMoveRequested?.Invoke(_ghostTarget.Value);

                _ghostTarget = null;
            }

            // --- Tap ---
            if (_active && wasTap && !_wasMoveTracking)
            {
                var cell = _gridView?.WorldToGrid(e.WorldPosition);
                if (cell.HasValue)
                    OnGridTapped?.Invoke(cell.Value);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns a living enemy that is adjacent to the player AND
        /// whose world position is within ActivationRadius of dragPos.
        /// </summary>
        private EnemyCombatModel FindAdjacentEnemyAt(Vector3 dragPos)
        {
            if (_model == null) return null;
            foreach (var enemy in _model.Enemies)
            {
                if (enemy.IsDead) continue;

                int dist = Manhattan(enemy.Position, _model.Player.Position);
                if (dist > 1) continue; // not adjacent

                var enemyWorld = _gridView.GridToWorld(enemy.Position);
                if (Vector3.Distance(dragPos, enemyWorld) <= ActivationRadius)
                    return enemy;
            }
            return null;
        }

        private Vector2Int? FindClosestValid(Vector3 worldPos)
        {
            Vector2Int? best     = null;
            float       bestDist = SnapRadius;

            foreach (var move in _validMoves)
            {
                float dist = Vector3.Distance(worldPos, _gridView.GridToWorld(move));
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = move;
                }
            }

            return best;
        }

        /// <summary>Converts a 2D delta into one of the 4 cardinal directions.</summary>
        private static Vector2Int ToCardinalDirection(Vector2 delta)
        {
            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
                return delta.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                return delta.y > 0 ? Vector2Int.up : Vector2Int.down;
        }

        private void CancelTracking()
        {
            _tracking        = false;
            _wasMoveTracking = false;
            _ghostTarget     = null;
            _pushTargetId    = -1;
            _playerView?.HideGhost();
            _gridView?.ClearHighlights();
        }

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
