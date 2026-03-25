using Mergistry.Core;
using Mergistry.GameStates;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI.HUD;
using Mergistry.UI.Popups;
using Mergistry.UI.Screens;
using Mergistry.Views.Board;
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
            var boardView      = FindFirstObjectByType<BoardView>(FindObjectsInactive.Include);
            var dragController = FindFirstObjectByType<BoardDragController>(FindObjectsInactive.Include);
            var menuScreenView = FindFirstObjectByType<MenuScreenView>(FindObjectsInactive.Include);
            var actionCounter  = FindFirstObjectByType<ActionCounterView>(FindObjectsInactive.Include);
            var inventoryView  = FindFirstObjectByType<InventoryView>(FindObjectsInactive.Include);
            var replacePopup   = FindFirstObjectByType<SlotReplacePopup>(FindObjectsInactive.Include);

            Debug.Log($"[GameManager] board={boardView}, drag={dragController}, menu={menuScreenView}, " +
                      $"counter={actionCounter}, inventory={inventoryView}, popup={replacePopup}");

            if (boardView == null || dragController == null || menuScreenView == null ||
                actionCounter == null || inventoryView == null || replacePopup == null)
            {
                Debug.LogError("[GameManager] Missing required references!");
                return;
            }

            _fsm = new GameStateMachine();
            boardView.gameObject.SetActive(false);
            actionCounter.gameObject.SetActive(false);
            inventoryView.gameObject.SetActive(false);

            // Register DistillationService if not already (fallback for editor start-from-Game)
            if (!ServiceLocator.TryGet<DistillationService>(out var distillationService))
            {
                distillationService = new DistillationService();
                ServiceLocator.Register(distillationService);
            }

            var inventory = new InventoryModel();
            inventoryView.Refresh(inventory);

            var distillationState = new DistillationState(
                boardView, dragController, distillationService,
                actionCounter, inventoryView, replacePopup, inventory);

            var menuState = new MenuState(menuScreenView, _fsm, distillationState);

            _fsm.ChangeState(menuState);
            Debug.Log("[GameManager] FSM started → MenuState");
        }

        private void Update() => _fsm?.Tick();
    }
}
