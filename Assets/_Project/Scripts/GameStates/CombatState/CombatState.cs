using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mergistry.Core;
using Mergistry.Data;
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

        // ── Flow dependencies ──────────────────────────────────────────────────
        private RunModel          _runModel;
        private HealthBarView     _healthBarView;
        private MapState          _mapState;          // A4: victory goes to map
        private ResultState       _resultState;
        private GameStateMachine  _fsm;

        // ── State ──────────────────────────────────────────────────────────────
        private CombatModel                         _model;
        private int                                 _selectedSlot = -1;
        private readonly Dictionary<int, EnemyView> _enemyViews = new Dictionary<int, EnemyView>();

        // A3: zone overlay views keyed by (x * GridModel.Height + y)
        private readonly Dictionary<int, ZoneOverlayView> _zoneOverlays = new Dictionary<int, ZoneOverlayView>();

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

        public void SetFlowDependencies(
            GameStateMachine  fsm,
            RunModel          runModel,
            HealthBarView     healthBarView,
            MapState          mapState,          // A4
            ResultState       resultState)
        {
            _fsm           = fsm;
            _runModel      = runModel;
            _healthBarView = healthBarView;
            _mapState      = mapState;
            _resultState   = resultState;
        }

        // ── Dev tools ─────────────────────────────────────────────────────────
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugGodMode { get; set; }

        public bool Debug_IsActive => _model != null;

        public int Debug_GetPlayerHP()    => _model?.Player.HP    ?? 0;
        public int Debug_GetPlayerMaxHP() => _model?.Player.MaxHP ?? 5;
        public int Debug_GetEnemyCount()  => _model?.Enemies.Count ?? 0;

        public void Debug_SetPlayerHP(int hp)
        {
            if (_model == null) return;
            _model.Player.HP = Mathf.Clamp(hp, 1, _model.Player.MaxHP);
            if (_healthBarView != null)
                _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);
        }

        public void Debug_KillAllEnemies()
        {
            if (_model == null) return;
            foreach (var e in _model.Enemies) e.HP = 0;
            RemoveDeadEnemies();
        }

        public void Debug_SpawnEnemy(EnemyType type, Vector2Int pos)
        {
            if (_model == null) return;
            EnemyCombatModel enemy;
            switch (type)
            {
                case EnemyType.Spider:
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 2);
                    break;
                case EnemyType.MushroomBomb:
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 3, bombTimer: 3);
                    break;
                case EnemyType.MagnetGolem:
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 6);
                    break;
                case EnemyType.ArmoredBeetle:
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 4, armorPoints: 2);
                    break;
                case EnemyType.MirrorSlime:
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 4);
                    break;
                case EnemyType.Phantom:
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 3);
                    break;
                case EnemyType.Necromancer:
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 5);
                    break;
                default: // Skeleton
                    enemy = new EnemyCombatModel(_model.NextEntityId(), type, pos, hp: 3);
                    break;
            }
            _model.Enemies.Add(enemy);
            SpawnViewForEnemy(enemy);
            _aiService.DetermineIntents(_model);
            RefreshEnemyIntentHighlights();
        }

        public void Debug_RespawnEnemies(int fightIndex)
        {
            if (_model == null) return;
            DestroyAllEnemyViews();
            _model.Enemies.Clear();
            _model.Graveyard.Clear();
            _combatService.SpawnEnemies(_model, fightIndex);
            SpawnEnemyViews();
            _aiService.DetermineIntents(_model);
            RefreshEnemyIntentHighlights();
        }

        public void Debug_ResetAllCooldowns()
        {
            for (int i = 0; i < InventoryModel.SlotCount; i++)
            {
                var slot = _inventory.GetSlot(i);
                if (slot != null) slot.CooldownRemaining = 0;
            }
            _inventoryView.RefreshCombat(_inventory, -1);
        }

        public void Debug_FillInventory(PotionType type, int level)
        {
            for (int i = 0; i < InventoryModel.SlotCount; i++)
                _inventory.Replace(i, type, level);
            _inventoryView.RefreshCombat(_inventory, -1);
        }

        public void Debug_ClearInventory()
        {
            _inventory.Clear();
            _inventoryView.RefreshCombat(_inventory, -1);
        }

        public void Debug_RestorePlayerTurn()
        {
            if (_model == null) return;
            _model.Player.HasMoved = false;
            _model.Player.HasActed = false;
            _phase = TurnPhase.PlayerTurn;
            _inputController.SetActive(true);
        }
#endif

        // ── IGameState ─────────────────────────────────────────────────────────

        public void Enter()
        {
            _model = _combatService.InitCombat();

            if (_runModel != null)
                _model.Player.HP = _runModel.PersistentHP;

            // A4: choose fight index based on node type
            int fightIndex = Mathf.Clamp(_runModel?.CurrentFight ?? 0, 0, 9);
            if (_runModel?.CurrentNodeType == MapNodeType.Elite)
                fightIndex = 10;
            else if (_runModel?.CurrentNodeType == MapNodeType.Boss)
                fightIndex = 11;

            _combatService.SpawnEnemies(_model, fightIndex);
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
            _inputController.OnPushRequested = OnPushRequested;
            _inputController.SetActive(true);

            _inventoryView.OnSlotClicked += OnSlotClicked;
            _skipButton.OnClicked        += OnSkipTurn;

            if (_healthBarView != null)
            {
                _healthBarView.gameObject.SetActive(true);
                _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);
            }

            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<BombExplodedEvent>(OnBombExploded);
            EventBus.Subscribe<ArmorRemovedEvent>(OnArmorRemoved);
            EventBus.Subscribe<EnemyRevivedEvent>(OnEnemyRevived);   // A3

            SpawnEnemyViews();

            _aiService.DetermineIntents(_model);
            RefreshEnemyIntentHighlights();

            _fadeView.FadeIn(0.2f, null);

            Debug.Log($"[CombatState] Entered — fight {_runModel?.CurrentFight}, " +
                      $"player HP={_model.Player.HP}, enemies={_model.Enemies.Count}");
        }

        public void Exit()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<BombExplodedEvent>(OnBombExploded);
            EventBus.Unsubscribe<ArmorRemovedEvent>(OnArmorRemoved);
            EventBus.Unsubscribe<EnemyRevivedEvent>(OnEnemyRevived); // A3

            _inputController.SetActive(false);
            _inputController.OnMoveRequested = null;
            _inputController.OnGridTapped    = null;
            _inputController.OnPushRequested = null;

            _inventoryView.OnSlotClicked -= OnSlotClicked;
            _skipButton.OnClicked        -= OnSkipTurn;

            _inventoryView.SetCombatMode(false);
            _inventoryView.gameObject.SetActive(false);

            _gridView.ClearHighlights();
            _gridView.ClearAoeHighlights();
            _gridView.ClearIntentHighlights();

            DestroyAllEnemyViews();
            DestroyAllZoneOverlays(); // A3

            _gridView.gameObject.SetActive(false);
            _playerView.gameObject.SetActive(false);
            _skipButton.gameObject.SetActive(false);

            if (_healthBarView != null)
                _healthBarView.gameObject.SetActive(false);
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

        // ── Enemy push ────────────────────────────────────────────────────────

        private void OnPushRequested(int entityId, Vector2Int direction)
        {
            if (_phase != TurnPhase.PlayerTurn) return;

            var enemy = _model.Enemies.FirstOrDefault(e => e.EntityId == entityId && !e.IsDead);
            if (enemy == null) return;

            if (Manhattan(enemy.Position, _model.Player.Position) > 1) return;

            var result = _combatService.PushEnemy(_model, enemy, direction);

            if (_enemyViews.TryGetValue(entityId, out var view))
                view.PlayPushAnimation(_gridView.GridToWorld(enemy.Position));

            if (result.BonusDamage > 0)
            {
                _damageService.ApplyDamage(enemy, result.BonusDamage);
                if (_enemyViews.TryGetValue(entityId, out var v))
                    v.PlayHitFlash();
            }

            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            Debug.Log($"[CombatState] Push result: moved={result.Moved}, bonus={result.BonusDamage}");

            _gridView.Run(EnemyTurnRoutine());
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

            var aoeCells    = _damageService.GetAffectedCells(slot.Type, cell, _model.Grid);
            var potionColor = BrewView.GetBrewColor(slot.Type);

            _gridView.SetAoeHighlightsTemporary(aoeCells, 0.35f);
            foreach (var aoeCell in aoeCells)
                _effectView.PlayEffect(_gridView.GridToWorld(aoeCell), potionColor);

            int damage = _damageService.GetDamage(slot.Type, slot.Level);

            // A2: Acid removes armor before dealing damage
            if (slot.Type == PotionType.Acid)
            {
                foreach (var enemy in _model.Enemies.ToList())
                    if (!enemy.IsDead && aoeCells.Contains(enemy.Position))
                        _damageService.RemoveArmor(enemy);
            }

            // A3: Flare stuns enemies in AoE
            if (slot.Type == PotionType.Flare)
            {
                foreach (var enemy in _model.Enemies)
                    if (!enemy.IsDead && aoeCells.Contains(enemy.Position))
                        ApplyStatusToEnemy(enemy, StatusEffectType.Stun, 1);
            }

            // A3: Poison/Napalm applies Poison DoT to enemies in AoE
            if (slot.Type == PotionType.Poison || slot.Type == PotionType.Napalm)
            {
                foreach (var enemy in _model.Enemies)
                    if (!enemy.IsDead && aoeCells.Contains(enemy.Position))
                        ApplyStatusToEnemy(enemy, StatusEffectType.Poison, 3);
            }

            // Damage all enemies in AoE
            foreach (var enemy in _model.Enemies.ToList())
            {
                if (!enemy.IsDead && aoeCells.Contains(enemy.Position))
                {
                    _damageService.ApplyDamage(enemy, damage);
                    if (_enemyViews.TryGetValue(enemy.EntityId, out var ev))
                        ev.PlayHitFlash();
                }
            }

            _combatService.ThrowPotion(_model, _inventory, _selectedSlot, cell);

            // A3: Create zones from certain potions
            CreateZonesFromPotion(slot.Type, aoeCells);

            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            Debug.Log($"[CombatState] Threw {slot.Type} lv{slot.Level} at {cell}");

            _gridView.Run(EnemyTurnRoutine());
        }

        // ── A3: Zone creation ─────────────────────────────────────────────────

        private void CreateZonesFromPotion(PotionType type, List<Vector2Int> aoeCells)
        {
            ZoneType? zoneType = type switch
            {
                PotionType.Flame  => ZoneType.Fire,
                PotionType.Napalm => ZoneType.Fire,
                PotionType.Stream => ZoneType.Water,
                PotionType.Poison => ZoneType.Poison,
                PotionType.Mist   => ZoneType.Poison,
                _                 => (ZoneType?)null
            };

            if (zoneType == null) return;

            int turns = zoneType == ZoneType.Water ? 3 : 2;
            if (type == PotionType.Poison || type == PotionType.Mist) turns = 3;

            foreach (var cell in aoeCells)
            {
                _model.Grid.AddZone(zoneType.Value, cell, turns);
                SpawnOrRefreshZoneOverlay(zoneType.Value, cell);
            }

            EventBus.Publish(new ZoneCreatedEvent
            {
                Type           = zoneType.Value,
                Positions      = aoeCells,
                TurnsRemaining = turns
            });

            Debug.Log($"[CombatState] Created {zoneType} zone on {aoeCells.Count} cells");
        }

        private void SpawnOrRefreshZoneOverlay(ZoneType type, Vector2Int cell)
        {
            int key = cell.x * GridModel.Height + cell.y;

            if (_zoneOverlays.TryGetValue(key, out var existing))
            {
                if (existing != null)
                    Object.Destroy(existing.gameObject);
                _zoneOverlays.Remove(key);
            }

            var go   = new GameObject($"Zone_{type}_{cell.x}_{cell.y}");
            go.transform.SetParent(_gridView.transform.parent);

            var overlay = go.AddComponent<ZoneOverlayView>();
            overlay.Initialize(type, _gridView.GridToWorld(cell));
            _zoneOverlays[key] = overlay;
        }

        private void RemoveZoneOverlay(Vector2Int cell)
        {
            int key = cell.x * GridModel.Height + cell.y;
            if (_zoneOverlays.TryGetValue(key, out var overlay))
            {
                if (overlay != null) Object.Destroy(overlay.gameObject);
                _zoneOverlays.Remove(key);
            }
        }

        private void DestroyAllZoneOverlays()
        {
            foreach (var kv in _zoneOverlays)
                if (kv.Value != null) Object.Destroy(kv.Value.gameObject);
            _zoneOverlays.Clear();
        }

        // ── Skip turn ─────────────────────────────────────────────────────────

        private void OnSkipTurn()
        {
            if (_phase != TurnPhase.PlayerTurn) return;

            _combatService.HealOnSkip(_model);

            if (_healthBarView != null)
                _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);

            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _gridView.ClearAoeHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            _gridView.Run(EnemyTurnRoutine());
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnPlayerDamaged(PlayerDamagedEvent e)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (DebugGodMode && _model != null)
            {
                _model.Player.HP = _model.Player.MaxHP;
                if (_healthBarView != null)
                    _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);
                return;
            }
#endif
            if (_healthBarView != null)
            {
                _healthBarView.Refresh(e.HPRemaining, _model.Player.MaxHP);
                _healthBarView.PlayDamageFlash();
            }
        }

        private void OnBombExploded(BombExplodedEvent e)
        {
            _gridView.SetAoeHighlightsTemporary(e.AffectedCells, 0.5f);
            foreach (var cell in e.AffectedCells)
                _effectView.PlayEffect(_gridView.GridToWorld(cell), new Color(1f, 0.4f, 0f));
        }

        private void OnArmorRemoved(ArmorRemovedEvent e)
        {
            var enemy = _model.Enemies.FirstOrDefault(en => en.EntityId == e.EntityId);
            if (enemy == null) return;
            if (_enemyViews.TryGetValue(e.EntityId, out var view))
                view.UpdateArmor(enemy.ArmorPoints);
        }

        // A3: Necromancer revived an enemy — spawn a new view for it
        private void OnEnemyRevived(EnemyRevivedEvent e)
        {
            var enemy = _model.Enemies.FirstOrDefault(en => en.EntityId == e.EntityId);
            if (enemy == null) return;

            // Remove stale view if somehow it still exists
            if (_enemyViews.TryGetValue(e.EntityId, out var old) && old != null)
            {
                Object.Destroy(old.gameObject);
                _enemyViews.Remove(e.EntityId);
            }

            SpawnViewForEnemy(enemy);
            Debug.Log($"[CombatState] Spawned view for revived enemy {e.EntityId} at {e.Position}");
        }

        // ── Enemy turn coroutine ───────────────────────────────────────────────

        private IEnumerator EnemyTurnRoutine()
        {
            _phase = TurnPhase.EnemyTurn;
            _inputController.SetActive(false);
            _gridView.ClearIntentHighlights();

            RemoveDeadEnemies();

            if (_model.Enemies.Count == 0)
            {
                OnCombatVictory();
                yield break;
            }

            yield return new WaitForSeconds(0.25f);

            _aiService.ExecuteIntents(_model);

            // Update all enemy positions (includes Phantom teleport, Necromancer move)
            foreach (var enemy in _model.Enemies)
            {
                if (_enemyViews.TryGetValue(enemy.EntityId, out var view))
                    view.PlaceAt(_gridView.GridToWorld(enemy.Position));
            }

            // A2: Update player position (may have been pulled by MagnetGolem)
            _playerView.PlaceAt(_gridView.GridToWorld(_model.Player.Position));

            yield return new WaitForSeconds(0.15f);

            // A3: Apply zone effects (Fire/Poison DoT, Water Slow)
            _combatService.ApplyZoneEffects(_model, _damageService);

            // Sync HP bar after zone damage
            if (_healthBarView != null)
                _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);

            // A3: Tick zone durations — remove expired zones and their overlays
            var expiredZones = _combatService.TickZones(_model.Grid);
            foreach (var z in expiredZones)
                RemoveZoneOverlay(z.Position);

            // A3: Tick status effect durations + Poison DoT
            _combatService.TickStatuses(_model, _damageService);

            // Sync HP bar after poison DoT
            if (_healthBarView != null)
                _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);

            // Remove bombs that just exploded, enemies killed by zone/poison DoT
            RemoveDeadEnemies();

            _combatService.StartNextPlayerTurn(_model, _inventory);

            if (_model.Player.HP <= 0)
            {
                OnCombatDefeat();
                yield break;
            }

            if (_model.Enemies.Count == 0)
            {
                OnCombatVictory();
                yield break;
            }

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

            if (_runModel == null || _fsm == null) return;

            _runModel.PersistentHP = _model.Player.HP;
            _runModel.CurrentFight++;

            // A4: return to map so it can mark the node visited and unlock successors
            _fadeView.FadeOut(0.2f, () => _fsm.ChangeState(_mapState));
        }

        private void OnCombatDefeat()
        {
            Debug.Log("[CombatState] Defeat! Player HP reached 0.");
            EventBus.Publish(new CombatEndedEvent { Victory = false });

            if (_runModel == null || _fsm == null) return;

            _runModel.LastVictory  = false;
            _runModel.PersistentHP = 0;

            _fadeView.FadeOut(0.3f, () => _fsm.ChangeState(_resultState));
        }

        // ── Enemy view management ─────────────────────────────────────────────

        private void SpawnEnemyViews()
        {
            foreach (var enemy in _model.Enemies)
                SpawnViewForEnemy(enemy);
        }

        private void SpawnViewForEnemy(EnemyCombatModel enemy)
        {
            var go = new GameObject($"Enemy_{enemy.EntityId}_{enemy.Type}");
            go.transform.SetParent(_gridView.transform.parent);
            go.transform.position = new Vector3(0f, 0f, -0.1f);

            var view = go.AddComponent<EnemyView>();
            view.Initialize(enemy, _gridView.GridToWorld(enemy.Position));
            _enemyViews[enemy.EntityId] = view;
        }

        private void RemoveDeadEnemies()
        {
            var dead = _model.Enemies.Where(e => e.IsDead).ToList();
            foreach (var d in dead)
            {
                _model.Enemies.Remove(d);
                _model.Graveyard.Add(d); // A3: keep for Necromancer to revive

                if (_enemyViews.TryGetValue(d.EntityId, out var view))
                {
                    view.PlayDeathFade(() => Object.Destroy(view.gameObject));
                    _enemyViews.Remove(d.EntityId);
                }
                Debug.Log($"[CombatState] Enemy {d.EntityId} ({d.Type}) removed → graveyard");
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

                if (_enemyViews.TryGetValue(enemy.EntityId, out var view))
                    view.SetIntent(enemy.Intent);

                if (enemy.Intent.Type == IntentType.Attack ||
                    enemy.Intent.Type == IntentType.Explode ||
                    enemy.Intent.Type == IntentType.Teleport) // A3: show phantom ring
                    attackCells.AddRange(enemy.Intent.AttackCells);
            }

            if (attackCells.Count > 0)
                _gridView.SetIntentHighlights(attackCells);
        }

        // ── A3: status application helper ─────────────────────────────────────

        private static void ApplyStatusToEnemy(EnemyCombatModel enemy, StatusEffectType type, int duration)
        {
            var existing = enemy.StatusEffects.Find(s => s.Type == type);
            if (existing != null)
                existing.Duration = Mathf.Max(existing.Duration, duration);
            else
                enemy.StatusEffects.Add(new StatusEffect(type, duration));
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
