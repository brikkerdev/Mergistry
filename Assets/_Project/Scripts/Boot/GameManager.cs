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
        private GameStateMachine _fsm;

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
            var gridView        = FindFirstObjectByType<GridView>(FindObjectsInactive.Include);
            var playerView      = FindFirstObjectByType<PlayerView>(FindObjectsInactive.Include);
            var combatInput     = FindFirstObjectByType<CombatInputController>(FindObjectsInactive.Include);
            var fadeView        = FindFirstObjectByType<FadeView>(FindObjectsInactive.Include);

            Debug.Log($"[GameManager] board={boardView}, drag={dragController}, menu={menuScreenView}, " +
                      $"counter={actionCounter}, inventory={inventoryView}, popup={replacePopup}");
            Debug.Log($"[GameManager] grid={gridView}, player={playerView}, " +
                      $"combatInput={combatInput}, fade={fadeView}");

            if (boardView == null || dragController == null || menuScreenView == null ||
                actionCounter == null || inventoryView == null || replacePopup == null ||
                gridView == null || playerView == null || combatInput == null || fadeView == null)
            {
                Debug.LogError("[GameManager] Missing required references!");
                return;
            }

            _fsm = new GameStateMachine();

            // Hide everything initially
            boardView.gameObject.SetActive(false);
            actionCounter.gameObject.SetActive(false);
            inventoryView.gameObject.SetActive(false);
            gridView.gameObject.SetActive(false);
            playerView.gameObject.SetActive(false);

            // Services
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

            // Models
            var inventory = new InventoryModel();
            inventoryView.Refresh(inventory);

            // States
            var combatState = new CombatState(
                gridView, playerView, combatInput, combatService, inventoryView, fadeView);

            var distillationState = new DistillationState(
                boardView, dragController, distillationService,
                actionCounter, inventoryView, replacePopup, inventory,
                _fsm, combatState, fadeView);

            var menuState = new MenuState(menuScreenView, _fsm, distillationState);

            _fsm.ChangeState(menuState);
            Debug.Log("[GameManager] FSM started → MenuState");
        }

        private void Update() => _fsm?.Tick();
    }
}
