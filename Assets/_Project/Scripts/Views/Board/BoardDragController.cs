using System;
using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models;
using Mergistry.Services;
using UnityEngine;

namespace Mergistry.Views.Board
{
    /// <summary>
    /// Handles drag-and-drop interactions on the board.
    /// M2: performs merge and infuse; decrements action counter via callback.
    /// </summary>
    public class BoardDragController : MonoBehaviour
    {
        private BoardView            _boardView;
        private BoardModel           _boardModel;
        private DistillationService  _distillationService;
        private Action               _onActionUsed;

        private IngredientView _dragging;
        private int            _fromX, _fromY;
        private bool           _active;

        public void Initialize(BoardView boardView, BoardModel boardModel,
            DistillationService distillationService, Action onActionUsed)
        {
            _boardView           = boardView;
            _boardModel          = boardModel;
            _distillationService = distillationService;
            _onActionUsed        = onActionUsed;
        }

        public void SetActive(bool active)
        {
            _active = active;
            if (!active) CancelDrag();
        }

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

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnDragStart(DragStartEvent e)
        {
            if (!_active || _boardView == null) return;
            if (!_boardView.TryGetGridPosition(e.WorldPosition, out int x, out int y)) return;

            var ingredient = _boardView.GetIngredientAt(x, y);
            if (ingredient == null) return;

            _dragging = ingredient;
            _fromX    = x;
            _fromY    = y;
            _dragging.StartDrag();
            HighlightNeighbors(x, y, true);
        }

        private void OnDragUpdate(DragUpdateEvent e)
        {
            if (!_active || _dragging == null) return;
            _dragging.UpdateDragPosition(e.WorldPosition);
        }

        private void OnDragEnd(DragEndEvent e)
        {
            if (!_active || _dragging == null) return;

            HighlightNeighbors(_fromX, _fromY, false);

            if (_boardView.TryGetGridPosition(e.WorldPosition, out int toX, out int toY) &&
                IsNeighbor(_fromX, _fromY, toX, toY))
            {
                if (_distillationService.CanMerge(_boardModel, _fromX, _fromY, toX, toY))
                {
                    var (potionType, element) = _distillationService.PerformMerge(_boardModel, _fromX, _fromY, toX, toY);
                    _boardView.RemoveIngredient(_fromX, _fromY);
                    _boardView.RemoveIngredient(toX, toY);
                    _boardView.PlaceBrew(toX, toY, potionType, element, 1);
                    _dragging = null;
                    _onActionUsed?.Invoke();
                    EventBus.Publish(new MergePerformedEvent());
                    return;
                }

                if (_distillationService.CanInfuse(_boardModel, _fromX, _fromY, toX, toY))
                {
                    int newLevel = _distillationService.PerformInfuse(_boardModel, _fromX, _fromY, toX, toY);
                    _boardView.RemoveIngredient(_fromX, _fromY);
                    _boardView.UpgradeBrew(toX, toY, newLevel);
                    _dragging = null;
                    _onActionUsed?.Invoke();
                    EventBus.Publish(new InfusePerformedEvent());
                    return;
                }
            }

            // Bounce back
            _dragging.EndDrag();
            _dragging = null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void CancelDrag()
        {
            if (_dragging == null) return;
            _dragging.EndDrag();
            HighlightNeighbors(_fromX, _fromY, false);
            _dragging = null;
        }

        private void HighlightNeighbors(int x, int y, bool active)
        {
            (int dx, int dy)[] dirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };
            foreach (var (dx, dy) in dirs)
            {
                int nx = x + dx, ny = y + dy;
                if (_boardModel.IsInBounds(nx, ny))
                    _boardView.SetHighlight(nx, ny, active);
            }
        }

        private static bool IsNeighbor(int x1, int y1, int x2, int y2) =>
            Mathf.Abs(x1 - x2) + Mathf.Abs(y1 - y2) == 1;
    }
}
