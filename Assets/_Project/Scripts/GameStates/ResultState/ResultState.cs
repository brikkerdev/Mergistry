using Mergistry.Core;
using Mergistry.Models;
using Mergistry.UI;
using Mergistry.UI.Screens;

namespace Mergistry.GameStates
{
    /// <summary>
    /// Displays the result screen (victory or defeat) after combat ends.
    /// "Retry" resets the run and goes back to DistillationState.
    /// "Menu" resets the run and goes to MenuState.
    /// </summary>
    public class ResultState : IGameState
    {
        private readonly ResultScreenView _view;
        private readonly FadeView         _fadeView;
        private readonly GameStateMachine _fsm;
        private readonly RunModel         _runModel;
        private readonly InventoryModel   _inventory;

        // Wired after all states are constructed (avoids constructor circular dependency).
        private MenuState _menuState;
        private MapState  _mapState; // A4: retry goes to map

        public ResultState(
            ResultScreenView view,
            FadeView         fadeView,
            GameStateMachine fsm,
            RunModel         runModel,
            InventoryModel   inventory)
        {
            _view      = view;
            _fadeView  = fadeView;
            _fsm       = fsm;
            _runModel  = runModel;
            _inventory = inventory;
        }

        /// <summary>Call from GameManager after all states are built.</summary>
        public void SetNavigationTargets(MenuState menuState, MapState mapState)
        {
            _menuState = menuState;
            _mapState  = mapState;
        }

        // ── IGameState ───────────────────────────────────────────────────────

        public void Enter()
        {
            _view.Show(_runModel.LastVictory, _runModel.CurrentFight - 1);
            _view.OnRetryClicked += OnRetry;
            _view.OnMenuClicked  += OnMenu;
            _fadeView.FadeIn(0.3f, null);
        }

        public void Exit()
        {
            _view.OnRetryClicked -= OnRetry;
            _view.OnMenuClicked  -= OnMenu;
            _view.Hide();
        }

        public void Tick() { }

        // ── Callbacks ────────────────────────────────────────────────────────

        private void OnRetry()
        {
            _runModel.Reset();
            _inventory.Clear();
            _fadeView.FadeOut(0.2f, () => _fsm.ChangeState(_mapState));
        }

        private void OnMenu()
        {
            _runModel.Reset();
            _inventory.Clear();
            _fadeView.FadeOut(0.2f, () => _fsm.ChangeState(_menuState));
        }
    }
}
