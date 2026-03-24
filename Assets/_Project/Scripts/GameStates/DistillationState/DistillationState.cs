using Mergistry.Core;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI.HUD;
using Mergistry.Views.Board;

namespace Mergistry.GameStates
{
    public class DistillationState : IGameState
    {
        private readonly BoardView           _boardView;
        private readonly BoardDragController _dragController;
        private readonly DistillationService _distillationService;
        private readonly ActionCounterView   _actionCounter;

        private int _seed;

        public DistillationState(BoardView boardView,
            BoardDragController dragController,
            DistillationService distillationService,
            ActionCounterView actionCounter)
        {
            _boardView           = boardView;
            _dragController      = dragController;
            _distillationService = distillationService;
            _actionCounter       = actionCounter;
        }

        public void Enter()
        {
            var board = _distillationService.GenerateBoard(_seed++);
            _boardView.gameObject.SetActive(true);
            _boardView.Initialize(board);
            _dragController.Initialize(_boardView, board);
            _dragController.SetActive(true);
            _actionCounter.gameObject.SetActive(true);
        }

        public void Exit()
        {
            _dragController.SetActive(false);
            _boardView.gameObject.SetActive(false);
            _actionCounter.gameObject.SetActive(false);
        }

        public void Tick() { }
    }
}
