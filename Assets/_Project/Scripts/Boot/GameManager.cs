using Mergistry.Core;
using Mergistry.GameStates;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.HUD;
using Mergistry.UI.Popups;
using Mergistry.UI.Screens;
using Mergistry.Views.Board;
using Mergistry.Views.Combat;
using UnityEngine;


namespace Mergistry.Boot
{
    /// <summary>
    /// Lives in Game scene. Finds its dependencies at runtime and runs the FSM.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private GameStateMachine  _fsm;
        private RunModel          _runModel;
        private InventoryModel    _inventory;
        private CombatState       _combatState;
        private DistillationState _distillationState;
        private MenuState         _menuState;
        private ResultState       _resultState;

        // ── Dev access ────────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public GameStateMachine  DevFSM               => _fsm;
        public RunModel          DevRunModel           => _runModel;
        public InventoryModel    DevInventory          => _inventory;
        public CombatState       DevCombatState        => _combatState;
        public DistillationState DevDistillationState  => _distillationState;
        public MenuState         DevMenuState          => _menuState;
        public ResultState       DevResultState        => _resultState;
#endif

        private void Awake()
        {
            Debug.Log("[GameManager] Awake");
        }

        private void Start()
        {
            Debug.Log("[GameManager] Start — initializing FSM");
            Init();
        }

        private void Init()
        {
            // ── Distillation dependencies ─────────────────────────────────────
            var boardView      = FindFirstObjectByType<BoardView>(FindObjectsInactive.Include);
            var dragController = FindFirstObjectByType<BoardDragController>(FindObjectsInactive.Include);
            var menuScreenView = FindFirstObjectByType<MenuScreenView>(FindObjectsInactive.Include);
            var actionCounter  = FindFirstObjectByType<ActionCounterView>(FindObjectsInactive.Include);
            var inventoryView  = FindFirstObjectByType<InventoryView>(FindObjectsInactive.Include);
            var replacePopup   = FindFirstObjectByType<SlotReplacePopup>(FindObjectsInactive.Include);

            // ── Combat dependencies ───────────────────────────────────────────
            var gridView    = FindFirstObjectByType<GridView>(FindObjectsInactive.Include);
            var playerView  = FindFirstObjectByType<PlayerView>(FindObjectsInactive.Include);
            var combatInput = FindFirstObjectByType<CombatInputController>(FindObjectsInactive.Include);
            var fadeView    = FindFirstObjectByType<FadeView>(FindObjectsInactive.Include);
            var effectView  = FindFirstObjectByType<EffectView>(FindObjectsInactive.Include);
            var skipButton  = FindFirstObjectByType<SkipTurnButtonView>(FindObjectsInactive.Include);

            // ── M6 dependencies ───────────────────────────────────────────────
            var healthBarView    = FindFirstObjectByType<HealthBarView>(FindObjectsInactive.Include);
            var resultScreenView = FindFirstObjectByType<ResultScreenView>(FindObjectsInactive.Include);

            Debug.Log($"[GameManager] board={boardView}, drag={dragController}, menu={menuScreenView}, " +
                      $"counter={actionCounter}, inventory={inventoryView}, popup={replacePopup}");
            Debug.Log($"[GameManager] grid={gridView}, player={playerView}, combatInput={combatInput}, " +
                      $"fade={fadeView}, effect={effectView}, skip={skipButton}");
            Debug.Log($"[GameManager] healthBar={healthBarView}, resultScreen={resultScreenView}");

            if (boardView == null || dragController == null || menuScreenView == null ||
                actionCounter == null || inventoryView == null || replacePopup == null ||
                gridView == null || playerView == null || combatInput == null || fadeView == null ||
                effectView == null || skipButton == null)
            {
                Debug.LogError("[GameManager] Missing required references!");
                return;
            }

            if (healthBarView == null)
                Debug.LogWarning("[GameManager] HealthBarView not found — HP display will be absent.");
            if (resultScreenView == null)
                Debug.LogWarning("[GameManager] ResultScreenView not found — result screen will be absent.");

            _fsm = new GameStateMachine();

            // Hide everything initially
            boardView.gameObject.SetActive(false);
            actionCounter.gameObject.SetActive(false);
            inventoryView.gameObject.SetActive(false);
            gridView.gameObject.SetActive(false);
            playerView.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            if (healthBarView != null)
                healthBarView.gameObject.SetActive(false);

            // ── Services ──────────────────────────────────────────────────────
            if (!ServiceLocator.TryGet<DistillationService>(out var distillationService))
            {
                distillationService = new DistillationService();
                ServiceLocator.Register(distillationService);
            }

            if (!ServiceLocator.TryGet<CombatService>(out var combatService))
            {
                combatService = new CombatService();
                ServiceLocator.Register(combatService);
            }

            if (!ServiceLocator.TryGet<DamageService>(out var damageService))
            {
                damageService = new DamageService();
                ServiceLocator.Register(damageService);
            }

            if (!ServiceLocator.TryGet<AIService>(out var aiService))
            {
                aiService = new AIService(damageService);
                ServiceLocator.Register(aiService);
            }

            // ── Models ────────────────────────────────────────────────────────
            _inventory = new InventoryModel();
            _runModel  = new RunModel();
            inventoryView.Refresh(_inventory);

            // ── BookScreen (created programmatically, no scene GO needed) ─────
            var bookScreenGo = new GameObject("BookScreen");
            var bookScreen   = bookScreenGo.AddComponent<BookScreen>();

            // ── States ────────────────────────────────────────────────────────
            _combatState = new CombatState(
                gridView, playerView, combatInput,
                combatService, damageService, aiService,
                inventoryView, fadeView,
                effectView, skipButton,
                _inventory);

            _distillationState = new DistillationState(
                boardView, dragController, distillationService,
                actionCounter, inventoryView, replacePopup, _inventory,
                _fsm, _combatState, fadeView,
                _runModel, bookScreen);

            _menuState = new MenuState(menuScreenView, _fsm, _distillationState, fadeView);

            if (resultScreenView != null)
            {
                _resultState = new ResultState(resultScreenView, fadeView, _fsm, _runModel, _inventory);
                _resultState.SetNavigationTargets(_menuState, _distillationState);
            }

            // Wire M6 flow dependencies into CombatState
            _combatState.SetFlowDependencies(_fsm, _runModel, healthBarView, _distillationState, _resultState);

            _fsm.ChangeState(_menuState);
            Debug.Log("[GameManager] FSM started → MenuState");
        }

        private void Update() => _fsm?.Tick();
    }
}
