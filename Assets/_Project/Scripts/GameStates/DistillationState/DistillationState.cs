using System.Collections.Generic;
using Mergistry.Core;
using Mergistry.Data;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.HUD;
using Mergistry.UI.Popups;
using Mergistry.UI.Screens;
using Mergistry.Views.Board;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Mergistry.GameStates
{
    public class DistillationState : IGameState
    {
        private const int MaxActions = 3;

        private readonly BoardView            _boardView;
        private readonly BoardDragController  _dragController;
        private readonly IDistillationService _distillationService;
        private readonly ActionCounterView    _actionCounter;
        private readonly InventoryView        _inventoryView;
        private readonly SlotReplacePopup     _replacePopup;
        private readonly InventoryModel       _inventory;
        private readonly GameStateMachine     _fsm;
        private readonly CombatState          _combatState;
        private readonly FadeView             _fadeView;
        private readonly RunModel             _runModel;
        private readonly BookScreen           _bookScreen;
        private readonly IRelicService        _relicService;  // A5

        private int        _seed;
        private int        _actionsRemaining;
        private BoardModel _currentBoard;
        private GameObject _bookButtonGo;  // top-right corner book button (created once)

        private Queue<DistillationService.BrewEntry> _pendingBrews;

        public DistillationState(
            BoardView            boardView,
            BoardDragController  dragController,
            IDistillationService distillationService,
            ActionCounterView    actionCounter,
            InventoryView        inventoryView,
            SlotReplacePopup     replacePopup,
            InventoryModel       inventory,
            GameStateMachine     fsm,
            CombatState          combatState,
            FadeView             fadeView,
            RunModel             runModel,
            BookScreen           bookScreen,
            IRelicService        relicService = null)  // A5
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
            _runModel            = runModel;
            _bookScreen          = bookScreen;
            _relicService        = relicService;       // A5
        }

        public void Enter()
        {
            _actionsRemaining = MaxActions;
            // A5: Cube — +1 distillation action
            if (_relicService != null && _relicService.HasRelic(Data.RelicType.Cube))
                _actionsRemaining++;
            _currentBoard     = _distillationService.GenerateBoard(_seed++, _runModel.CurrentFloor);

            _boardView.gameObject.SetActive(true);
            _boardView.Initialize(_currentBoard);

            _dragController.Initialize(_boardView, _currentBoard, OnActionUsed);
            _dragController.CanMergeFunc     = (board, fx, fy, tx, ty) => _distillationService.CanMerge(board, fx, fy, tx, ty);
            _dragController.PerformMergeFunc = (board, fx, fy, tx, ty) => _distillationService.PerformMerge(board, fx, fy, tx, ty);
            _dragController.CanInfuseFunc    = (board, fx, fy, tx, ty) => _distillationService.CanInfuse(board, fx, fy, tx, ty);
            _dragController.PerformInfuseFunc = (board, fx, fy, tx, ty) => _distillationService.PerformInfuse(board, fx, fy, tx, ty);
            _dragController.SetActive(true);

            _actionCounter.gameObject.SetActive(true);
            _actionCounter.Refresh(_actionsRemaining);

            _inventoryView.gameObject.SetActive(true);
            _inventoryView.Refresh(_inventory);
            _inventoryView.OnGoBattleClicked -= OnGoBattleClicked;
            _inventoryView.OnGoBattleClicked += OnGoBattleClicked;

            EnsureBookButton();
            _bookButtonGo.SetActive(true);
            _bookScreen.Hide();
            _fadeView.FadeIn(0.2f, null);

            Debug.Log($"[DistillationState] Floor {_runModel.CurrentFloor} — {(new[] { 3, 4, 5 }[Mathf.Min(_runModel.CurrentFloor, 2)])} elements on board.");
        }

        public void Exit()
        {
            _inventoryView.OnGoBattleClicked -= OnGoBattleClicked;
            _dragController.SetActive(false);
            _boardView.gameObject.SetActive(false);
            _actionCounter.gameObject.SetActive(false);
            _inventoryView.gameObject.SetActive(false);
            _replacePopup.Hide();
            if (_bookButtonGo != null) _bookButtonGo.SetActive(false);
            _bookScreen.Hide();
        }

        public void Tick()
        {
#if UNITY_EDITOR
            // Debug: F1/F2/F3 force-switch floor and regenerate board
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb[Key.F1].wasPressedThisFrame) Debug_SetFloor(0);
            if (kb[Key.F2].wasPressedThisFrame) Debug_SetFloor(1);
            if (kb[Key.F3].wasPressedThisFrame) Debug_SetFloor(2);
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool Debug_IsActive => _currentBoard != null;

        public void Debug_RegenerateBoard()
        {
            if (_currentBoard == null) return;
            _currentBoard = _distillationService.GenerateBoard(_seed++, _runModel.CurrentFloor);
            _boardView.Initialize(_currentBoard);
            _dragController.Initialize(_boardView, _currentBoard, OnActionUsed);
            _actionsRemaining = MaxActions;
            _actionCounter.Refresh(_actionsRemaining);
            Debug.Log("[DEBUG] Board regenerated.");
        }

        public void Debug_SetFloor(int floor)
        {
            _runModel.CurrentFloor = floor;
            _currentBoard = _distillationService.GenerateBoard(_seed++, floor);
            _boardView.Initialize(_currentBoard);
            _dragController.Initialize(_boardView, _currentBoard, OnActionUsed);
            _actionsRemaining = MaxActions;
            _actionCounter.Refresh(_actionsRemaining);
            Debug.Log($"[DEBUG] Switched to floor {floor} — board regenerated.");
        }

        public void Debug_AddActions(int count)
        {
            if (_currentBoard == null) return;
            _actionsRemaining += count;
            _actionCounter.Refresh(_actionsRemaining);
        }

        public int Debug_GetActionsRemaining() => _actionsRemaining;
        public int Debug_GetCurrentFloor()     => _runModel.CurrentFloor;
#endif

        // ── Book button ──────────────────────────────────────────────────────

        private void EnsureBookButton()
        {
            if (_bookButtonGo != null) return;

            // Place in top-right corner using camera viewport
            var cam      = Camera.main;
            var topRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, Mathf.Abs(cam.transform.position.z)));
            var pos      = new Vector3(topRight.x - 0.52f, topRight.y - 0.38f, -0.5f);

            _bookButtonGo = new GameObject("BookButton_Distillation");

            // Background
            var bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            bg.name = "BG";
            bg.transform.SetParent(_bookButtonGo.transform);
            bg.transform.localPosition = new Vector3(0f, 0f, 0.01f);
            bg.transform.localScale    = new Vector3(0.80f, 0.52f, 1f);
            Object.Destroy(bg.GetComponent<MeshCollider>());
            var bgR = bg.GetComponent<MeshRenderer>();
            bgR.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.20f, 0.18f, 0.35f) };
            bgR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            bgR.receiveShadows    = false;

            // Face
            var face = GameObject.CreatePrimitive(PrimitiveType.Quad);
            face.name = "Face";
            face.transform.SetParent(_bookButtonGo.transform);
            face.transform.localPosition = Vector3.zero;
            face.transform.localScale    = new Vector3(0.70f, 0.44f, 1f);
            Object.Destroy(face.GetComponent<MeshCollider>());
            var faceR = face.GetComponent<MeshRenderer>();
            faceR.material = new Material(Shader.Find("Unlit/Color")) { color = new Color(0.35f, 0.28f, 0.60f) };
            faceR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            faceR.receiveShadows    = false;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_bookButtonGo.transform);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            labelGo.transform.localScale    = Vector3.one * 0.016f;
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text      = "RB";
            tm.fontSize  = 150;
            tm.fontStyle = FontStyle.Bold;
            tm.color     = new Color(0.92f, 0.88f, 0.50f);
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            // Collider + click
            var col  = _bookButtonGo.AddComponent<BoxCollider>();
            col.size = new Vector3(0.80f, 0.52f, 0.1f);

            var handler      = _bookButtonGo.AddComponent<UI.Popups.SlotClickHandler>();
            handler.OnClicked = () => _bookScreen.Toggle();

            _bookButtonGo.transform.position = pos;
        }

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
            // Block transition if inventory is empty and no brews on the board
            var brews = _distillationService.CollectBrews(_currentBoard);
            bool willHavePotions = !_inventory.IsEmpty() || brews.Count > 0;

            if (!willHavePotions)
            {
                // Grant a bonus action so the player can brew something
                _actionsRemaining = Mathf.Max(_actionsRemaining, 1);
                _actionCounter.Refresh(_actionsRemaining);
                _dragController.SetActive(true);
                Debug.Log("[DistillationState] Cannot go to battle — no potions! Bonus action granted.");
                return;
            }

            _dragController.SetActive(false);

            // Brews already collected above — feed them directly
            _pendingBrews = new Queue<DistillationService.BrewEntry>(brews);
            ProcessNextBrew();
        }

        // ── Collect Brews ────────────────────────────────────────────────────

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
