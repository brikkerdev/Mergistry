using Mergistry.Core;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.Screens;

namespace Mergistry.GameStates
{
    public class MenuState : IGameState
    {
        private readonly MenuScreenView   _menuScreen;
        private readonly GameStateMachine _fsm;
        private readonly IGameState       _nextState;
        private readonly FadeView         _fadeView;

        // A8: save/load for "Continue" flow
        private readonly ISaveService  _saveService;
        private readonly RunModel      _runModel;
        private readonly InventoryModel _inventory;

        public MenuState(
            MenuScreenView   menuScreen,
            GameStateMachine fsm,
            IGameState       nextState,
            FadeView         fadeView   = null,
            ISaveService     saveService = null,
            RunModel         runModel    = null,
            InventoryModel   inventory   = null)
        {
            _menuScreen  = menuScreen;
            _fsm         = fsm;
            _nextState   = nextState;
            _fadeView    = fadeView;
            _saveService = saveService;
            _runModel    = runModel;
            _inventory   = inventory;
        }

        public void Enter()
        {
            _menuScreen.Show();
            _menuScreen.OnStartClicked    += HandleStart;
            _menuScreen.OnContinueClicked += HandleContinue;

            // A8: show Continue button if a mid-run save exists
            bool hasSave = _saveService != null && _saveService.HasRunSave;
            _menuScreen.ShowContinueButton(hasSave);

            _fadeView?.FadeIn(0.2f, null);
        }

        public void Exit()
        {
            _menuScreen.OnStartClicked    -= HandleStart;
            _menuScreen.OnContinueClicked -= HandleContinue;
            _menuScreen.ShowContinueButton(false);
            _menuScreen.Hide();
        }

        public void Tick() { }

        // ── Handlers ─────────────────────────────────────────────────────────

        private void HandleStart()
        {
            // New run: discard any existing save
            _saveService?.DeleteRunSave();
            _fsm.ChangeState(_nextState);
        }

        private void HandleContinue()
        {
            if (_saveService == null || _runModel == null || _inventory == null) return;

            var data = _saveService.LoadRun();
            if (data == null)
            {
                // Save was corrupt — just start fresh
                _menuScreen.ShowContinueButton(false);
                return;
            }

            // Restore run state (HP, relics, inventory, floor)
            RunSaveHelper.Restore(data, _runModel, _inventory);

            // Navigate to MapState (will generate fresh map for the saved floor)
            _fadeView?.FadeOut(0.2f, () => _fsm.ChangeState(_nextState));
        }
    }
}
