using Mergistry.Core;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.Screens;

namespace Mergistry.GameStates
{
    /// <summary>
    /// Displays the result screen (victory or defeat) after combat ends.
    /// A8: deletes run.json and updates meta.json on Enter.
    /// </summary>
    public class ResultState : IGameState
    {
        private readonly ResultScreenView _view;
        private readonly FadeView         _fadeView;
        private readonly GameStateMachine _fsm;
        private readonly RunModel         _runModel;
        private readonly InventoryModel   _inventory;

        private MenuState _menuState;
        private MapState  _mapState;

        // A8: injected via SetSaveService after construction
        private ISaveService          _saveService;
        private MetaProgressionModel  _meta;

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

        public void SetNavigationTargets(MenuState menuState, MapState mapState)
        {
            _menuState = menuState;
            _mapState  = mapState;
        }

        /// <summary>A8: inject save service so result state can update meta.</summary>
        public void SetSaveService(ISaveService saveService, MetaProgressionModel meta)
        {
            _saveService = saveService;
            _meta        = meta;
        }

        // ── IGameState ───────────────────────────────────────────────────────

        public void Enter()
        {
            // A8: run is over — delete mid-run save and update meta-progression
            if (_saveService != null)
            {
                _saveService.DeleteRunSave();

                if (_meta != null)
                {
                    _meta.TotalRuns++;
                    if (_runModel.LastVictory) _meta.TotalVictories++;
                    if (_runModel.CurrentFight - 1 > _meta.BestFightReached)
                        _meta.BestFightReached = _runModel.CurrentFight - 1;
                    _saveService.SaveMeta(_meta);
                }
            }

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
