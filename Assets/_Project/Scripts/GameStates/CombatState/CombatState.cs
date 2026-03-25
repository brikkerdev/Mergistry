using Mergistry.Core;
using Mergistry.Models;
using Mergistry.Models.Combat;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.HUD;
using Mergistry.Views.Board;
using Mergistry.Views.Combat;
using UnityEngine;

namespace Mergistry.GameStates
{
    public class CombatState : IGameState
    {
        private readonly GridView              _gridView;
        private readonly PlayerView            _playerView;
        private readonly CombatInputController _inputController;
        private readonly CombatService         _combatService;
        private readonly DamageService         _damageService;
        private readonly InventoryView         _inventoryView;
        private readonly FadeView              _fadeView;
        private readonly EffectView            _effectView;
        private readonly SkipTurnButtonView    _skipButton;
        private readonly InventoryModel        _inventory;

        private CombatModel _model;
        private int         _selectedSlot = -1;

        public CombatState(
            GridView              gridView,
            PlayerView            playerView,
            CombatInputController inputController,
            CombatService         combatService,
            DamageService         damageService,
            InventoryView         inventoryView,
            FadeView              fadeView,
            EffectView            effectView,
            SkipTurnButtonView    skipButton,
            InventoryModel        inventory)
        {
            _gridView        = gridView;
            _playerView      = playerView;
            _inputController = inputController;
            _combatService   = combatService;
            _damageService   = damageService;
            _inventoryView   = inventoryView;
            _fadeView        = fadeView;
            _effectView      = effectView;
            _skipButton      = skipButton;
            _inventory       = inventory;
        }

        public void Enter()
        {
            _model        = _combatService.InitCombat();
            _selectedSlot = -1;

            _gridView.gameObject.SetActive(true);
            _playerView.gameObject.SetActive(true);
            _inventoryView.gameObject.SetActive(true);
            _skipButton.gameObject.SetActive(true);

            var startWorld = _gridView.GridToWorld(_model.Player.Position);
            _playerView.PlaceAt(startWorld);

            // Switch inventory to combat mode
            _inventoryView.SetCombatMode(true);
            _inventoryView.RefreshCombat(_inventory, -1);

            // Wire input
            _inputController.Initialize(_gridView, _playerView, _model, _combatService);
            _inputController.OnMoveRequested = OnMoveRequested;
            _inputController.OnGridTapped    = OnGridTapped;
            _inputController.SetActive(true);

            // Wire slot clicks and skip button
            _inventoryView.OnSlotClicked += OnSlotClicked;
            _skipButton.OnClicked        += OnSkipTurn;

            _fadeView.FadeIn(0.2f, null);

            Debug.Log("[CombatState] Entered — player at (1,1)");
        }

        public void Exit()
        {
            _inputController.SetActive(false);
            _inputController.OnMoveRequested = null;
            _inputController.OnGridTapped    = null;

            _inventoryView.OnSlotClicked -= OnSlotClicked;
            _skipButton.OnClicked        -= OnSkipTurn;

            _inventoryView.SetCombatMode(false);

            _gridView.ClearHighlights();
            _gridView.ClearAoeHighlights();

            _gridView.gameObject.SetActive(false);
            _playerView.gameObject.SetActive(false);
            _skipButton.gameObject.SetActive(false);
        }

        public void Tick() { }

        // ── Movement ──────────────────────────────────────────────────────────

        private void OnMoveRequested(Vector2Int target)
        {
            if (_model.Player.HasMoved) return;

            _model.Player.Position = target;
            _model.Player.HasMoved = true;

            _playerView.MoveTo(_gridView.GridToWorld(target));

            // Clear any throw highlights when player moves (position changed)
            if (_selectedSlot >= 0)
            {
                _gridView.ClearHighlights();
                _selectedSlot = -1;
                _inventoryView.RefreshCombat(_inventory, -1);
            }

            Debug.Log($"[CombatState] Player moved to {target}");
        }

        // ── Slot selection ────────────────────────────────────────────────────

        private void OnSlotClicked(int slotIndex)
        {
            var slot = _inventory.GetSlot(slotIndex);

            // Deselect if slot is unusable or already selected
            if (slot.IsEmpty || slot.CooldownRemaining > 0 || _selectedSlot == slotIndex)
            {
                _selectedSlot = -1;
                _gridView.ClearHighlights();
                _inventoryView.RefreshCombat(_inventory, -1);
                Debug.Log("[CombatState] Slot deselected");
                return;
            }

            // Select slot and show valid throw range in blue
            _selectedSlot = slotIndex;
            var validCells = _damageService.GetValidThrowRange(_model.Grid, _model.Player.Position);
            _gridView.SetHighlights(validCells);
            _inventoryView.RefreshCombat(_inventory, _selectedSlot);

            Debug.Log($"[CombatState] Slot {slotIndex} selected ({slot.Type} lv{slot.Level}), {validCells.Count} valid cells");
        }

        // ── Potion throw ──────────────────────────────────────────────────────

        private void OnGridTapped(Vector2Int cell)
        {
            if (_selectedSlot < 0) return;

            var slot = _inventory.GetSlot(_selectedSlot);
            if (slot.IsEmpty || slot.CooldownRemaining > 0) return;

            // Validate throw range
            var validCells = _damageService.GetValidThrowRange(_model.Grid, _model.Player.Position);
            if (!validCells.Contains(cell)) return;

            // Get AoE and play effects on all affected cells
            var aoeCells   = _damageService.GetAffectedCells(slot.Type, cell, _model.Grid);
            var potionColor = BrewView.GetBrewColor(slot.Type);

            _gridView.SetAoeHighlightsTemporary(aoeCells, 0.35f);
            foreach (var aoeCell in aoeCells)
                _effectView.PlayEffect(_gridView.GridToWorld(aoeCell), potionColor);

            // Execute throw (sets cooldown, publishes event)
            _combatService.ThrowPotion(_model, _inventory, _selectedSlot, cell);

            // Clear selection
            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            Debug.Log($"[CombatState] Threw {slot.Type} lv{slot.Level} at {cell}, AoE={aoeCells.Count} cells");
        }

        // ── Skip turn ─────────────────────────────────────────────────────────

        private void OnSkipTurn()
        {
            _combatService.SkipTurn(_model, _inventory);

            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _gridView.ClearAoeHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            Debug.Log($"[CombatState] Turn skipped — HP={_model.Player.HP}/{_model.Player.MaxHP}");
        }
    }
}
