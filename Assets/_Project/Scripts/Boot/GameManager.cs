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
// A6: BossState namespace already in Mergistry.GameStates


namespace Mergistry.Boot
{
    /// <summary>
    /// Lives in Game scene. Finds its dependencies at runtime and runs the FSM.
    /// A4: added MapState, EventState, MapGeneratorService; flow is now
    ///     Menu → Map → Distillation → Combat → Map (loop).
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
        private MapState          _mapState;          // A4
        private EventState        _eventState;        // A4
        private RelicChoiceState  _relicChoiceState;  // A5
        private BossState         _bossState;         // A6

        // ── Dev access ────────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public GameStateMachine  DevFSM               => _fsm;
        public RunModel          DevRunModel           => _runModel;
        public InventoryModel    DevInventory          => _inventory;
        public CombatState       DevCombatState        => _combatState;
        public DistillationState DevDistillationState  => _distillationState;
        public MenuState         DevMenuState          => _menuState;
        public ResultState       DevResultState        => _resultState;
        public MapState          DevMapState           => _mapState;
        public EventState        DevEventState         => _eventState;
        public RelicChoiceState  DevRelicChoiceState   => _relicChoiceState;
        public BossState         DevBossState          => _bossState;          // A6
#endif

        private void Awake()  => Debug.Log("[GameManager] Awake");
        private void Start()  { Debug.Log("[GameManager] Start — initializing FSM"); Init(); }
        private void Update() => _fsm?.Tick();

        private void Init()
        {
            // ── Scene views ───────────────────────────────────────────────────
            var boardView      = FindFirstObjectByType<BoardView>(FindObjectsInactive.Include);
            var dragController = FindFirstObjectByType<BoardDragController>(FindObjectsInactive.Include);
            var menuScreenView = FindFirstObjectByType<MenuScreenView>(FindObjectsInactive.Include);
            var actionCounter  = FindFirstObjectByType<ActionCounterView>(FindObjectsInactive.Include);
            var inventoryView  = FindFirstObjectByType<InventoryView>(FindObjectsInactive.Include);
            var replacePopup   = FindFirstObjectByType<SlotReplacePopup>(FindObjectsInactive.Include);
            var gridView       = FindFirstObjectByType<GridView>(FindObjectsInactive.Include);
            var playerView     = FindFirstObjectByType<PlayerView>(FindObjectsInactive.Include);
            var combatInput    = FindFirstObjectByType<CombatInputController>(FindObjectsInactive.Include);
            var fadeView       = FindFirstObjectByType<FadeView>(FindObjectsInactive.Include);
            var effectView     = FindFirstObjectByType<EffectView>(FindObjectsInactive.Include);
            var skipButton     = FindFirstObjectByType<SkipTurnButtonView>(FindObjectsInactive.Include);
            var healthBarView  = FindFirstObjectByType<HealthBarView>(FindObjectsInactive.Include);
            var resultView     = FindFirstObjectByType<ResultScreenView>(FindObjectsInactive.Include);

            Debug.Log($"[GameManager] board={boardView}, drag={dragController}, menu={menuScreenView}, " +
                      $"counter={actionCounter}, inventory={inventoryView}, popup={replacePopup}");
            Debug.Log($"[GameManager] grid={gridView}, player={playerView}, combatInput={combatInput}, " +
                      $"fade={fadeView}, effect={effectView}, skip={skipButton}");
            Debug.Log($"[GameManager] healthBar={healthBarView}, resultScreen={resultView}");

            if (boardView == null || dragController == null || menuScreenView == null ||
                actionCounter == null || inventoryView == null || replacePopup == null ||
                gridView == null || playerView == null || combatInput == null ||
                fadeView == null || effectView == null || skipButton == null)
            {
                Debug.LogError("[GameManager] Missing required scene references!");
                return;
            }

            if (healthBarView == null)
                Debug.LogWarning("[GameManager] HealthBarView not found.");
            if (resultView == null)
                Debug.LogWarning("[GameManager] ResultScreenView not found.");

            _fsm = new GameStateMachine();

            // Hide all gameplay views initially
            boardView.gameObject.SetActive(false);
            actionCounter.gameObject.SetActive(false);
            inventoryView.gameObject.SetActive(false);
            gridView.gameObject.SetActive(false);
            playerView.gameObject.SetActive(false);
            skipButton.gameObject.SetActive(false);
            if (healthBarView != null) healthBarView.gameObject.SetActive(false);

            // ── Services ──────────────────────────────────────────────────────
            // A5: relicService must be created first so it can be injected into combat services
            if (!ServiceLocator.TryGet<IRelicService>(out var relicService))
            {
                relicService = new RelicService();
                ServiceLocator.Register<IRelicService>(relicService);
            }

            if (!ServiceLocator.TryGet<ILootService>(out var lootService))
            {
                lootService = new LootService();
                ServiceLocator.Register<ILootService>(lootService);
            }

            if (!ServiceLocator.TryGet<IDistillationService>(out var distillationService))
            {
                distillationService = new DistillationService();
                ServiceLocator.Register<IDistillationService>(distillationService);
            }
            if (!ServiceLocator.TryGet<ICombatService>(out var combatService))
            {
                combatService = new CombatService(relicService); // A5: inject relicService
                ServiceLocator.Register<ICombatService>(combatService);
            }
            if (!ServiceLocator.TryGet<IDamageService>(out var damageService))
            {
                damageService = new DamageService(relicService); // A5: inject relicService
                ServiceLocator.Register<IDamageService>(damageService);
            }
            if (!ServiceLocator.TryGet<IAIService>(out var aiService))
            {
                aiService = new AIService(damageService);
                ServiceLocator.Register<IAIService>(aiService);
            }

            IMapGeneratorService mapGeneratorService = new MapGeneratorService(); // A4

            // ── Models ────────────────────────────────────────────────────────
            _inventory = new InventoryModel();
            _runModel  = new RunModel();
            relicService.SetModel(_runModel.Relics);
            inventoryView.Refresh(_inventory);

            // ── Programmatic screen objects ────────────────────────────────────
            var bookScreenGo = new GameObject("BookScreen");
            var bookScreen   = bookScreenGo.AddComponent<BookScreen>();

            var mapScreenGo = new GameObject("MapScreen");     // A4
            var mapScreen   = mapScreenGo.AddComponent<MapScreen>();

            var eventScreenGo = new GameObject("EventScreen"); // A4
            var eventScreen   = eventScreenGo.AddComponent<EventScreenView>();

            var relicChoiceScreenGo = new GameObject("RelicChoiceScreen"); // A5
            var relicChoiceScreen   = relicChoiceScreenGo.AddComponent<RelicChoiceScreenView>();

            var relicBarGo = new GameObject("RelicBar"); // A5
            relicBarGo.transform.position = new Vector3(0f, 3.6f, -0.5f);
            var relicBarView = relicBarGo.AddComponent<RelicBarView>();

            var bossHPBarGo = new GameObject("BossHPBar"); // A6
            var bossHPBar   = bossHPBarGo.AddComponent<BossHPBarView>();

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
                _runModel, bookScreen, relicService); // A5: pass relicService for Cube relic

            // A6: BossState — same view deps as CombatState, adds BossHPBarView
            _bossState = new BossState(
                gridView, playerView, combatInput,
                combatService, damageService, aiService,
                inventoryView, fadeView, effectView, skipButton,
                _inventory, bossHPBar);
            _distillationState.SetBossState(_bossState); // A6: route Boss nodes here

            // EventState constructed before MapState (both reference each other via Set methods)
            _eventState = new EventState(eventScreen, _fsm, fadeView,
                _runModel, _inventory, relicService, lootService);

            _mapState = new MapState(
                mapScreen, mapGeneratorService,
                _fsm, _runModel, fadeView,
                _distillationState, _eventState);

            _eventState.SetMapState(_mapState); // wire back-reference

            _relicChoiceState = new RelicChoiceState( // A5
                relicChoiceScreen, _fsm, fadeView, relicService);
            _relicChoiceState.SetMapState(_mapState);

            _menuState = new MenuState(menuScreenView, _fsm, _mapState, fadeView); // A4: goes to map

            if (resultView != null)
            {
                _resultState = new ResultState(resultView, fadeView, _fsm, _runModel, _inventory);
                _resultState.SetNavigationTargets(_menuState, _mapState); // A4: retry → map
            }

            // Wire flow dependencies into CombatState (A4: victory → mapState; A5: relic flow)
            _combatState.SetFlowDependencies(
                _fsm, _runModel, healthBarView, _mapState, _resultState,
                relicService, relicBarView, _relicChoiceState); // A5

            // A6: wire BossState flow (victory → mapState, defeat → resultState)
            _bossState.SetFlowDependencies(
                _fsm, _runModel, healthBarView, _mapState, _resultState,
                relicService, relicBarView);

            // Wire result state into MapState (for game-win path)
            _mapState.SetResultState(_resultState);

            _fsm.ChangeState(_menuState);
            Debug.Log("[GameManager] FSM started → MenuState");
        }
    }
}
