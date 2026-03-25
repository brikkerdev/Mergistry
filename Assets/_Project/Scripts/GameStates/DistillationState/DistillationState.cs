using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.HUD;
using Mergistry.UI.Popups;
using Mergistry.Views.Board;
using UnityEngine;

namespace Mergistry.GameStates
{
    public class DistillationState : IGameState
    {
        private const int MaxActions = 3;

        private readonly BoardView           _boardView;
        private readonly BoardDragController _dragController;
        private readonly DistillationService _distillationService;
        private readonly ActionCounterView   _actionCounter;
        private readonly InventoryView       _inventoryView;
        private readonly SlotReplacePopup    _replacePopup;
        private readonly InventoryModel      _inventory;
        private readonly GameStateMachine    _fsm;
        private readonly CombatState         _combatState;
        private readonly FadeView            _fadeView;

        private int       _seed;
        private int       _actionsRemaining;
        private BoardModel _currentBoard;

        private Queue<DistillationService.BrewEntry> _pendingBrews;

        public DistillationState(
            BoardView           boardView,
            BoardDragController dragController,
            DistillationService distillationService,
            ActionCounterView   actionCounter,
            InventoryView       inventoryView,
            SlotReplacePopup    replacePopup,
            InventoryModel      inventory,
            GameStateMachine    fsm,
            CombatState         combatState,
            FadeView            fadeView)
        {
            _boardView           = boardView;
            _dragController      = dragController;
            _distillationService = distillationService;
            _actionCounter       = actionCounter;
            _inventoryView       = inventoryView;
            _replacePopup        = replacePopup;
            _inventory           = inventory;
            _fsm                 = fsm;
            _combatState         = combatState;
            _fadeView            = fadeView;
        }

        public void Enter()
        {
            _actionsRemaining = MaxActions;
            _currentBoard     = _distillationService.GenerateBoard(_seed++);

            _boardView.gameObject.SetActive(true);
            _boardView.Initialize(_currentBoard);

            _dragController.Initialize(_boardView, _currentBoard, _distillationService, OnActionUsed);
            _dragController.SetActive(true);

            _actionCounter.gameObject.SetActive(true);
            _actionCounter.Refresh(_actionsRemaining);

            _inventoryView.gameObject.SetActive(true);
            _inventoryView.Refresh(_inventory);
            _inventoryView.OnGoBattleClicked -= OnGoBattleClicked;
            _inventoryView.OnGoBattleClicked += OnGoBattleClicked;

            _fadeView.FadeIn(0.2f, null);
        }

        public void Exit()
        {
            _inventoryView.OnGoBattleClicked -= OnGoBattleClicked;
            _dragController.SetActive(false);
            _boardView.gameObject.SetActive(false);
            _actionCounter.gameObject.SetActive(false);
            _inventoryView.gameObject.SetActive(false);
            _replacePopup.Hide();
        }

        public void Tick() { }

        // ── Callbacks ────────────────────────────────────────────────────────

        private void OnActionUsed()
        {
            _actionsRemaining = Mathf.Max(0, _actionsRemaining - 1);
            _actionCounter.Refresh(_actionsRemaining);

            if (_actionsRemaining == 0)
                OnGoBattleClicked();
        }

        private void OnGoBattleClicked()
        {
            _dragController.SetActive(false);
            StartCollectBrews();
        }

        // ── Collect Brews ────────────────────────────────────────────────────

        private void StartCollectBrews()
        {
            var brews = _distillationService.CollectBrews(_currentBoard);
            _pendingBrews = new Queue<DistillationService.BrewEntry>(brews);
            ProcessNextBrew();
        }

        private void ProcessNextBrew()
        {
            if (_pendingBrews == null || _pendingBrews.Count == 0)
            {
                _inventoryView.Refresh(_inventory);
                Debug.Log("[DistillationState] Brews collected. Transitioning to CombatState.");
                _fadeView.FadeOut(0.2f, () => _fsm.ChangeState(_combatState));
                return;
            }

            var entry = _pendingBrews.Dequeue();

            if (_inventory.TryAdd(entry.PotionType, entry.Level))
            {
                _inventoryView.Refresh(_inventory);
                ProcessNextBrew(); // Next brew immediately
            }
            else
            {
                // Full inventory — ask player which slot to replace
                _replacePopup.Show(entry.PotionType, entry.Level, _inventory, slotIndex =>
                {
                    if (slotIndex >= 0)
                    {
                        _inventory.Replace(slotIndex, entry.PotionType, entry.Level);
                        _inventoryView.Refresh(_inventory);
                    }
                    // slotIndex == -1 → discard, do nothing with inventory
                    _replacePopup.Hide();
                    ProcessNextBrew();
                });
            }
        }
    }
}
