using Mergistry.Core;
using Mergistry.Events;
using Mergistry.Models;
using UnityEngine;

namespace Mergistry.Views.Board
{
    /// <summary>
    /// Handles drag-and-drop interactions on the board.
    /// M1: always bounces back (no merge logic yet).
    /// </summary>
    public class BoardDragController : MonoBehaviour
    {
        private BoardView  _boardView;
        private BoardModel _boardModel;

        private IngredientView _dragging;
        private int _fromX, _fromY;
        private bool _active;

        public void Initialize(BoardView boardView, BoardModel boardModel)
        {
            _boardView  = boardView;
            _boardModel = boardModel;
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
            _fromX = x;
            _fromY = y;
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
            // M1: always bounce back
            _dragging.EndDrag();
            HighlightNeighbors(_fromX, _fromY, false);
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
    }
}
