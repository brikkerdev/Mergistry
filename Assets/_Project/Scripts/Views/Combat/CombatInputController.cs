using System;
using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models.Combat;
using Mergistry.Services;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Handles swipe input on the combat grid.
    /// A drag starting near the player shows a ghost at the nearest valid destination;
    /// releasing confirms the move.
    /// </summary>
    public class CombatInputController : MonoBehaviour
    {
        // Max world-space distance from player centre to start tracking a drag.
        private const float ActivationRadius = 1.0f;
        // Max world-space distance from a candidate cell to snap the ghost to it.
        private const float SnapRadius = 1.2f;
        // Minimum drag length before we start snapping.
        private const float MinDragDelta = 0.25f;

        private GridView      _gridView;
        private PlayerView    _playerView;
        private CombatModel   _model;
        private CombatService _service;

        private bool             _active;
        private bool             _tracking;
        private Vector3          _dragStart;
        private Vector2Int?      _ghostTarget;
        private List<Vector2Int> _validMoves;

        public Action<Vector2Int> OnMoveRequested;

        // ── Init ──────────────────────────────────────────────────────────────

        public void Initialize(GridView gridView, PlayerView playerView,
            CombatModel model, CombatService service)
        {
            _gridView   = gridView;
            _playerView = playerView;
            _model      = model;
            _service    = service;
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
            if (!_active || _model == null || _model.Player.HasMoved) return;

            var playerWorld = _gridView.GridToWorld(_model.Player.Position);
            if (Vector3.Distance(e.WorldPosition, playerWorld) > ActivationRadius) return;

            _tracking   = true;
            _dragStart  = e.WorldPosition;
            _validMoves = _service.GetValidMoves(_model);
            _gridView.SetHighlights(_validMoves);
        }

        private void OnDragUpdate(DragUpdateEvent e)
        {
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
            if (!_tracking) return;

            _tracking = false;
            _playerView.HideGhost();
            _gridView.ClearHighlights();

            if (_ghostTarget.HasValue)
                OnMoveRequested?.Invoke(_ghostTarget.Value);

            _ghostTarget = null;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

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

        private void CancelTracking()
        {
            _tracking    = false;
            _ghostTarget = null;
            _playerView?.HideGhost();
            _gridView?.ClearHighlights();
        }
    }
}
