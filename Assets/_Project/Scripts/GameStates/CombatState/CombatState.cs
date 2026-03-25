using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mergistry.Core;
using Mergistry.Events;
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
        // ── Dependencies ───────────────────────────────────────────────────────
        private readonly GridView              _gridView;
        private readonly PlayerView            _playerView;
        private readonly CombatInputController _inputController;
        private readonly CombatService         _combatService;
        private readonly DamageService         _damageService;
        private readonly AIService             _aiService;
        private readonly InventoryView         _inventoryView;
        private readonly FadeView              _fadeView;
        private readonly EffectView            _effectView;
        private readonly SkipTurnButtonView    _skipButton;
        private readonly InventoryModel        _inventory;

        // ── State ──────────────────────────────────────────────────────────────
        private CombatModel               _model;
        private int                       _selectedSlot = -1;
        private readonly Dictionary<int, EnemyView> _enemyViews = new Dictionary<int, EnemyView>();

        private enum TurnPhase { PlayerTurn, EnemyTurn }
        private TurnPhase _phase;

        public CombatState(
            GridView              gridView,
            PlayerView            playerView,
            CombatInputController inputController,
            CombatService         combatService,
            DamageService         damageService,
            AIService             aiService,
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
            _aiService       = aiService;
            _inventoryView   = inventoryView;
            _fadeView        = fadeView;
            _effectView      = effectView;
            _skipButton      = skipButton;
            _inventory       = inventory;
        }

        // ── IGameState ─────────────────────────────────────────────────────────

        public void Enter()
        {
            _model = _combatService.InitCombat();
            _combatService.SpawnEnemies(_model);
            _selectedSlot = -1;
            _phase        = TurnPhase.PlayerTurn;

            _gridView.gameObject.SetActive(true);
            _playerView.gameObject.SetActive(true);
            _inventoryView.gameObject.SetActive(true);
            _skipButton.gameObject.SetActive(true);

            _playerView.PlaceAt(_gridView.GridToWorld(_model.Player.Position));

            _inventoryView.SetCombatMode(true);
            _inventoryView.RefreshCombat(_inventory, -1);

            _inputController.Initialize(_gridView, _playerView, _model, _combatService);
            _inputController.OnMoveRequested = OnMoveRequested;
            _inputController.OnGridTapped    = OnGridTapped;
            _inputController.SetActive(true);

            _inventoryView.OnSlotClicked += OnSlotClicked;
            _skipButton.OnClicked        += OnSkipTurn;

            SpawnEnemyViews();

            _aiService.DetermineIntents(_model);
            RefreshEnemyIntentHighlights();

            _fadeView.FadeIn(0.2f, null);

            Debug.Log("[CombatState] Entered — player at (1,1), enemies spawned");
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
            _gridView.ClearIntentHighlights();

            DestroyAllEnemyViews();

            _gridView.gameObject.SetActive(false);
            _playerView.gameObject.SetActive(false);
            _skipButton.gameObject.SetActive(false);
        }

        public void Tick() { }

        // ── Movement ──────────────────────────────────────────────────────────

        private void OnMoveRequested(Vector2Int target)
        {
            if (_phase != TurnPhase.PlayerTurn) return;
            if (_model.Player.HasMoved) return;

            _model.Player.Position = target;
            _model.Player.HasMoved = true;

            _playerView.MoveTo(_gridView.GridToWorld(target));

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
            if (_phase != TurnPhase.PlayerTurn) return;

            var slot = _inventory.GetSlot(slotIndex);

            if (slot.IsEmpty || slot.CooldownRemaining > 0 || _selectedSlot == slotIndex)
            {
                _selectedSlot = -1;
                _gridView.ClearHighlights();
                _inventoryView.RefreshCombat(_inventory, -1);
                return;
            }

            _selectedSlot = slotIndex;
            var validCells = _damageService.GetValidThrowRange(_model.Grid, _model.Player.Position);
            _gridView.SetHighlights(validCells);
            _inventoryView.RefreshCombat(_inventory, _selectedSlot);

            Debug.Log($"[CombatState] Slot {slotIndex} selected ({slot.Type} lv{slot.Level})");
        }

        // ── Potion throw ──────────────────────────────────────────────────────

        private void OnGridTapped(Vector2Int cell)
        {
            if (_phase != TurnPhase.PlayerTurn) return;
            if (_selectedSlot < 0) return;

            var slot = _inventory.GetSlot(_selectedSlot);
            if (slot.IsEmpty || slot.CooldownRemaining > 0) return;

            var validCells = _damageService.GetValidThrowRange(_model.Grid, _model.Player.Position);
            if (!validCells.Contains(cell)) return;

            // AoE cells and visual effects
            var aoeCells    = _damageService.GetAffectedCells(slot.Type, cell, _model.Grid);
            var potionColor = BrewView.GetBrewColor(slot.Type);

            _gridView.SetAoeHighlightsTemporary(aoeCells, 0.35f);
            foreach (var aoeCell in aoeCells)
                _effectView.PlayEffect(_gridView.GridToWorld(aoeCell), potionColor);

            // Apply damage to enemies in AoE
            int damage = _damageService.GetDamage(slot.Type, slot.Level);
            foreach (var enemy in _model.Enemies.ToList())
            {
                if (!enemy.IsDead && aoeCells.Contains(enemy.Position))
                {
                    _damageService.ApplyDamage(enemy, damage);
                    if (_enemyViews.TryGetValue(enemy.EntityId, out var ev))
                        ev.PlayHitFlash();
                }
            }

            // Execute throw (sets cooldown + HasActed)
            _combatService.ThrowPotion(_model, _inventory, _selectedSlot, cell);

            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            Debug.Log($"[CombatState] Threw {slot.Type} lv{slot.Level} at {cell}");

            // End player turn after acting
            _gridView.Run(EnemyTurnRoutine());
        }

        // ── Skip turn ─────────────────────────────────────────────────────────

        private void OnSkipTurn()
        {
            if (_phase != TurnPhase.PlayerTurn) return;

            _combatService.HealOnSkip(_model);

            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _gridView.ClearAoeHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            _gridView.Run(EnemyTurnRoutine());
        }

        // ── Enemy turn coroutine ───────────────────────────────────────────────

        private IEnumerator EnemyTurnRoutine()
        {
            _phase = TurnPhase.EnemyTurn;
            _inputController.SetActive(false);
            _gridView.ClearIntentHighlights();

            // Remove enemies that died from potion throws before enemy turn
            RemoveDeadEnemies();

            // Check victory before enemies act
            if (_model.Enemies.Count == 0)
            {
                OnCombatVictory();
                yield break;
            }

            yield return new WaitForSeconds(0.25f);

            // Execute enemy intents
            _aiService.ExecuteIntents(_model);

            // Update enemy view positions
            foreach (var enemy in _model.Enemies)
            {
                if (_enemyViews.TryGetValue(enemy.EntityId, out var view))
                    view.PlaceAt(_gridView.GridToWorld(enemy.Position));
            }

            yield return new WaitForSeconds(0.15f);

            // Tick cooldowns and reset player flags
            _combatService.StartNextPlayerTurn(_model, _inventory);

            // Check player death
            if (_model.Player.HP <= 0)
            {
                OnCombatDefeat();
                yield break;
            }

            // Determine intents for next turn
            _aiService.DetermineIntents(_model);
            RefreshEnemyIntentHighlights();

            _inventoryView.RefreshCombat(_inventory, -1);

            _phase = TurnPhase.PlayerTurn;
            _inputController.SetActive(true);

            Debug.Log($"[CombatState] New player turn — HP={_model.Player.HP}/{_model.Player.MaxHP}");
        }

        // ── Outcome ───────────────────────────────────────────────────────────

        private void OnCombatVictory()
        {
            Debug.Log("[CombatState] Victory! All enemies defeated.");
            EventBus.Publish(new CombatEndedEvent { Victory = true });
        }

        private void OnCombatDefeat()
        {
            Debug.Log("[CombatState] Defeat! Player HP reached 0.");
            EventBus.Publish(new CombatEndedEvent { Victory = false });
        }

        // ── Enemy view management ─────────────────────────────────────────────

        private void SpawnEnemyViews()
        {
            foreach (var enemy in _model.Enemies)
            {
                var go   = new GameObject($"Enemy_{enemy.EntityId}_{enemy.Type}");
                go.transform.SetParent(_gridView.transform.parent);
                go.transform.position = new Vector3(0f, 0f, -0.1f);

                var view = go.AddComponent<EnemyView>();
                view.Initialize(enemy, _gridView.GridToWorld(enemy.Position));
                _enemyViews[enemy.EntityId] = view;
            }
        }

        private void RemoveDeadEnemies()
        {
            var dead = _model.Enemies.Where(e => e.IsDead).ToList();
            foreach (var d in dead)
            {
                _model.Enemies.Remove(d);
                if (_enemyViews.TryGetValue(d.EntityId, out var view))
                {
                    view.PlayDeathFade(() => Object.Destroy(view.gameObject));
                    _enemyViews.Remove(d.EntityId);
                }
                Debug.Log($"[CombatState] Enemy {d.EntityId} ({d.Type}) removed from battlefield");
            }
        }

        private void DestroyAllEnemyViews()
        {
            foreach (var kv in _enemyViews)
                if (kv.Value != null) Object.Destroy(kv.Value.gameObject);
            _enemyViews.Clear();
        }

        private void RefreshEnemyIntentHighlights()
        {
            _gridView.ClearIntentHighlights();

            var attackCells = new List<Vector2Int>();
            foreach (var enemy in _model.Enemies)
            {
                if (enemy.IsDead || enemy.Intent == null) continue;

                // Update intent icon on view
                if (_enemyViews.TryGetValue(enemy.EntityId, out var view))
                    view.SetIntent(enemy.Intent);

                // Collect attack cells for orange grid highlight
                if (enemy.Intent.Type == IntentType.Attack)
                    attackCells.AddRange(enemy.Intent.AttackCells);
            }

            if (attackCells.Count > 0)
                _gridView.SetIntentHighlights(attackCells);
        }
    }
}
