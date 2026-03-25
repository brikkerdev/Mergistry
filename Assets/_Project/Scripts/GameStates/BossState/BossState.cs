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
    /// <summary>
    /// A6: Boss combat state.
    /// Uses a 6×6 grid (temporarily expands GridModel.Width/Height).
    /// Boss is tracked separately from model.Enemies (2×2 footprint, phase transitions).
    /// Pillars are impassable. Cauldrons take damage and can be destroyed.
    /// </summary>
    public class BossState : IGameState
    {
        private const int BossGridSize   = 6;
        private const int NormalGridSize = 5;

        // ── Dependencies ───────────────────────────────────────────────────────
        private readonly GridView              _gridView;
        private readonly PlayerView            _playerView;
        private readonly CombatInputController _inputController;
        private readonly ICombatService        _combatService;
        private readonly IDamageService        _damageService;
        private readonly IAIService            _aiService;
        private readonly InventoryView         _inventoryView;
        private readonly FadeView              _fadeView;
        private readonly EffectView            _effectView;
        private readonly SkipTurnButtonView    _skipButton;
        private readonly InventoryModel        _inventory;
        private readonly BossHPBarView         _bossHPBar;

        // ── Flow dependencies ──────────────────────────────────────────────────
        private RunModel         _runModel;
        private HealthBarView    _healthBarView;
        private MapState         _mapState;
        private ResultState      _resultState;
        private GameStateMachine _fsm;
        private IRelicService    _relicService;
        private RelicBarView     _relicBarView;

        // ── State ──────────────────────────────────────────────────────────────
        private CombatModel _model;
        private int         _selectedSlot = -1;
        private bool        _phaseTransitionDone;

        private readonly Dictionary<int, EnemyView>     _enemyViews   = new Dictionary<int, EnemyView>();
        private readonly Dictionary<int, ZoneOverlayView> _zoneOverlays = new Dictionary<int, ZoneOverlayView>();
        private readonly List<GameObject>               _interactableViews = new List<GameObject>();
        private EnemyView _bossView;

        private enum TurnPhase { PlayerTurn, EnemyTurn }
        private TurnPhase _phase;

        public BossState(
            GridView              gridView,
            PlayerView            playerView,
            CombatInputController inputController,
            ICombatService        combatService,
            IDamageService        damageService,
            IAIService            aiService,
            InventoryView         inventoryView,
            FadeView              fadeView,
            EffectView            effectView,
            SkipTurnButtonView    skipButton,
            InventoryModel        inventory,
            BossHPBarView         bossHPBar)
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
            _bossHPBar       = bossHPBar;
        }

        public void SetFlowDependencies(
            GameStateMachine fsm,
            RunModel         runModel,
            HealthBarView    healthBarView,
            MapState         mapState,
            ResultState      resultState,
            IRelicService    relicService = null,
            RelicBarView     relicBarView = null)
        {
            _fsm           = fsm;
            _runModel      = runModel;
            _healthBarView = healthBarView;
            _mapState      = mapState;
            _resultState   = resultState;
            _relicService  = relicService;
            _relicBarView  = relicBarView;
        }

        // ── IGameState ─────────────────────────────────────────────────────────

        public void Enter()
        {
            _phaseTransitionDone = false;

            // Expand grid to 6×6
            GridModel.Width  = BossGridSize;
            GridModel.Height = BossGridSize;
            _gridView.Rebuild();

            // Init model
            _model = _combatService.InitCombat();
            _model.IsBossFight = true;
            if (_runModel != null)
            {
                _model.Player.MaxHP = _runModel.MaxHP;
                _model.Player.HP    = _runModel.PersistentHP;
            }

            int floor = _runModel?.CurrentFloor ?? 0;
            SetupBossFight(floor);

            _selectedSlot = -1;
            _phase        = TurnPhase.PlayerTurn;

            _gridView.gameObject.SetActive(true);
            _playerView.gameObject.SetActive(true);
            _playerView.PlaceAt(_gridView.GridToWorld(_model.Player.Position));

            _inventoryView.gameObject.SetActive(true);
            _inventoryView.SetCombatMode(true);
            _inventoryView.RefreshCombat(_inventory, -1);
            _skipButton.gameObject.SetActive(true);

            if (_healthBarView != null)
            {
                _healthBarView.gameObject.SetActive(true);
                _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);
            }

            if (_relicBarView != null)
            {
                _relicBarView.gameObject.SetActive(true);
                _relicBarView.Refresh(_relicService?.GetActiveRelics());
            }

            _bossHPBar?.Show(_model.BossEnemy.HP, _model.BossEnemy.MaxHP, BossName(floor));

            _inputController.Initialize(_gridView, _playerView, _model);
            _inputController.GetValidMovesFunc = m => _combatService.GetValidMoves(m);
            _inputController.OnMoveRequested   = OnMoveRequested;
            _inputController.OnGridTapped      = OnGridTapped;
            _inputController.OnPushRequested   = null; // no push in boss fights
            _inputController.SetActive(true);

            _inventoryView.OnSlotClicked += OnSlotClicked;
            _skipButton.OnClicked        += OnSkipTurn;

            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<BombExplodedEvent>(OnBombExploded);
            EventBus.Subscribe<EnemySpawnedEvent>(OnEnemySpawned);

            SpawnBossView();
            SpawnEnemyViews();
            SpawnInteractableViews();

            DetermineBossIntent();
            _aiService.DetermineIntents(_model);
            RefreshIntentHighlights();

            _fadeView?.FadeIn(0.2f, null);

            Debug.Log($"[BossState] Entered floor {floor} boss fight — " +
                      $"boss={_model.BossEnemy?.Type}, HP={_model.BossEnemy?.HP}");
        }

        public void Exit()
        {
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<BombExplodedEvent>(OnBombExploded);
            EventBus.Unsubscribe<EnemySpawnedEvent>(OnEnemySpawned);

            _inputController.SetActive(false);
            _inputController.GetValidMovesFunc = null;
            _inputController.OnMoveRequested   = null;
            _inputController.OnGridTapped      = null;

            _inventoryView.OnSlotClicked -= OnSlotClicked;
            _skipButton.OnClicked        -= OnSkipTurn;

            _inventoryView.SetCombatMode(false);
            _inventoryView.gameObject.SetActive(false);

            _gridView.ClearHighlights();
            _gridView.ClearAoeHighlights();
            _gridView.ClearIntentHighlights();

            if (_bossView != null)
            {
                Object.Destroy(_bossView.gameObject);
                _bossView = null;
            }
            DestroyAllEnemyViews();
            DestroyAllZoneOverlays();
            DestroyInteractableViews();

            _bossHPBar?.Hide();

            _gridView.gameObject.SetActive(false);
            _playerView.gameObject.SetActive(false);
            _skipButton.gameObject.SetActive(false);

            if (_healthBarView != null) _healthBarView.gameObject.SetActive(false);
            if (_relicBarView  != null) _relicBarView.gameObject.SetActive(false);

            // Restore 5×5 grid
            GridModel.Width  = NormalGridSize;
            GridModel.Height = NormalGridSize;
            _gridView.Rebuild();
        }

        public void Tick() { }

        // ── Boss setup by floor ────────────────────────────────────────────────

        private void SetupBossFight(int floor)
        {
            _model.Player.Position = new Vector2Int(1, 1);

            switch (floor)
            {
                case 0:  SetupSpiderQueen();       break;
                case 1:  SetupIronGolem();         break;
                default: SetupRenegadeAlchemist(); break;
            }
        }

        private void SetupSpiderQueen()
        {
            var boss = new EnemyCombatModel(_model.NextEntityId(), EnemyType.SpiderQueen,
                                            new Vector2Int(3, 3), hp: 20);
            _model.BossEnemy = boss;

            // 3 web zones (persistent Water)
            _model.Grid.AddZone(ZoneType.Water, new Vector2Int(2, 0), 999);
            _model.Grid.AddZone(ZoneType.Water, new Vector2Int(4, 2), 999);
            _model.Grid.AddZone(ZoneType.Water, new Vector2Int(1, 4), 999);

            foreach (var z in _model.Grid.Zones)
                SpawnOrRefreshZoneOverlay(z.Type, z.Position);
        }

        private void SetupIronGolem()
        {
            var boss = new EnemyCombatModel(_model.NextEntityId(), EnemyType.IronGolem,
                                            new Vector2Int(3, 3), hp: 25);
            _model.BossEnemy = boss;

            _model.Interactables.Add(new InteractableModel(InteractableType.Pillar, new Vector2Int(2, 3), isPassable: false));
            _model.Interactables.Add(new InteractableModel(InteractableType.Pillar, new Vector2Int(4, 1), isPassable: false));
        }

        private void SetupRenegadeAlchemist()
        {
            var boss = new EnemyCombatModel(_model.NextEntityId(), EnemyType.RenegadeAlchemist,
                                            new Vector2Int(3, 2), hp: 18);
            _model.BossEnemy = boss;

            _model.Interactables.Add(new InteractableModel(InteractableType.Cauldron, new Vector2Int(1, 3), isPassable: true, hp: 3));
            _model.Interactables.Add(new InteractableModel(InteractableType.Cauldron, new Vector2Int(5, 1), isPassable: true, hp: 3));
            _model.Interactables.Add(new InteractableModel(InteractableType.Cauldron, new Vector2Int(5, 4), isPassable: true, hp: 3));
        }

        // ── Boss AI ────────────────────────────────────────────────────────────

        private void DetermineBossIntent()
        {
            if (_model.BossEnemy == null || _model.BossEnemy.IsDead) return;

            var boss = _model.BossEnemy;
            if (boss.HasStatus(StatusEffectType.Stun)) { boss.Intent = null; return; }

            // Ask AIService to call the registered boss behavior
            // Temporarily add boss to Enemies so DetermineIntents processes it
            _model.Enemies.Add(boss);
            _aiService.DetermineIntents(_model);
            _model.Enemies.Remove(boss);
        }

        private void ExecuteBossIntent()
        {
            var boss   = _model.BossEnemy;
            var intent = boss?.Intent;
            if (intent == null) return;

            switch (intent.Type)
            {
                case IntentType.Move:
                    boss.Position = intent.TargetPosition;
                    if (_bossView != null)
                        _bossView.PlaceAt(_gridView.GridToWorld(boss.Position));
                    Debug.Log($"[BossState] Boss moved to {boss.Position}");
                    break;

                case IntentType.Attack:
                    // Web zone creation (Spider Queen Phase 1)
                    if (boss.Type == EnemyType.SpiderQueen)
                    {
                        foreach (var c in intent.AttackCells)
                        {
                            _model.Grid.AddZone(ZoneType.Water, c, 2);
                            SpawnOrRefreshZoneOverlay(ZoneType.Water, c);
                        }
                    }
                    // Hit player if in attack cells
                    if (intent.AttackCells.Contains(_model.Player.Position))
                        _damageService.ApplyDamageToPlayer(_model.Player, intent.Damage);

                    // Boss also moves
                    boss.Position = intent.TargetPosition;
                    if (_bossView != null)
                        _bossView.PlaceAt(_gridView.GridToWorld(boss.Position));
                    break;

                case IntentType.AreaAttack:
                    if (intent.AttackCells.Contains(_model.Player.Position))
                        _damageService.ApplyDamageToPlayer(_model.Player, intent.Damage);
                    if (_bossView != null) _bossView.PlayHitFlash();
                    Debug.Log($"[BossState] Boss area attack, player hit={intent.AttackCells.Contains(_model.Player.Position)}");
                    break;

                case IntentType.Pull:
                    var pulled = AIHelper.StepPlayerToward(_model.Player.Position, boss.Position, _model);
                    _model.Player.Position = pulled;
                    if (AIHelper.Manhattan(pulled, boss.Position) <= 2)
                        _damageService.ApplyDamageToPlayer(_model.Player, intent.Damage);
                    Debug.Log($"[BossState] Boss pulled player to {pulled}");
                    break;

                case IntentType.Teleport:
                    boss.Position = intent.TargetPosition;
                    if (_bossView != null)
                        _bossView.PlaceAt(_gridView.GridToWorld(boss.Position));
                    if (intent.AttackCells.Contains(_model.Player.Position))
                        _damageService.ApplyDamageToPlayer(_model.Player, intent.Damage);
                    Debug.Log($"[BossState] Boss teleported to {boss.Position}");
                    break;

                case IntentType.SummonMinions:
                    foreach (var pos in intent.SpawnPositions)
                    {
                        var minion = new EnemyCombatModel(_model.NextEntityId(), intent.MinionType, pos, intent.MinionHP);
                        _model.Enemies.Add(minion);
                        EventBus.Publish(new EnemySpawnedEvent { EntityId = minion.EntityId, Type = minion.Type, Position = pos });
                        Debug.Log($"[BossState] Summoned {minion.Type} at {pos}");
                    }
                    break;
            }
        }

        // ── Phase transition ───────────────────────────────────────────────────

        private IEnumerator TriggerPhaseTransition()
        {
            _model.CurrentBossPhase = BossPhase.Phase2;
            EventBus.Publish(new BossPhaseChangedEvent { NewPhase = BossPhase.Phase2 });
            _bossHPBar?.ShowPhase2();

            // Screen flash
            if (_effectView != null)
                _effectView.PlayEffect(_gridView.GridToWorld(new Vector2Int(2, 2)), new Color(1f, 0.3f, 0.0f));

            // Iron Golem Phase 2: break pillars
            if (_model.BossEnemy?.Type == EnemyType.IronGolem)
            {
                foreach (var it in _model.Interactables.ToList())
                {
                    if (it.Type == InteractableType.Pillar)
                    {
                        it.HP = 0; // mark destroyed
                        _effectView?.PlayEffect(_gridView.GridToWorld(it.Position), new Color(0.8f, 0.6f, 0.2f));
                    }
                }
                RefreshInteractableViews();
            }

            yield return new WaitForSeconds(0.5f);

            Debug.Log("[BossState] Phase 2 triggered!");
        }

        // ── Input handlers ─────────────────────────────────────────────────────

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
        }

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

        private void OnGridTapped(Vector2Int cell)
        {
            if (_phase != TurnPhase.PlayerTurn) return;
            if (_selectedSlot < 0) return;

            var slot = _inventory.GetSlot(_selectedSlot);
            if (slot.IsEmpty || slot.CooldownRemaining > 0) return;

            var validCells = _damageService.GetValidThrowRange(_model.Grid, _model.Player.Position);
            if (!validCells.Contains(cell)) return;

            // Check cauldron hit
            var cauldron = _model.Interactables.FirstOrDefault(
                i => i.Type == InteractableType.Cauldron && !i.IsDestroyed && i.Position == cell);
            if (cauldron != null)
            {
                int dmg = _damageService.GetDamage(slot.Type, slot.Level);
                cauldron.HP -= dmg;
                _combatService.ThrowPotion(_model, _inventory, _selectedSlot, cell);
                _effectView.PlayEffect(_gridView.GridToWorld(cell), new Color(0.5f, 0.8f, 0.3f));

                if (cauldron.IsDestroyed)
                {
                    RefreshInteractableViews();
                    CheckAllCauldronsDestroyed();
                }

                _selectedSlot = -1;
                _gridView.ClearHighlights();
                _inventoryView.RefreshCombat(_inventory, -1);
                _gridView.Run(EnemyTurnRoutine());
                return;
            }

            // Standard potion throw
            var aoeCells    = _damageService.GetAffectedCells(slot.Type, cell, _model.Grid);
            var potionColor = BrewView.GetBrewColor(slot.Type);

            _gridView.SetAoeHighlightsTemporary(aoeCells, 0.35f);
            foreach (var aoeCell in aoeCells)
                _effectView.PlayEffect(_gridView.GridToWorld(aoeCell), potionColor);

            int damage = _damageService.GetDamage(slot.Type, slot.Level);

            // Boss hit (check 2×2 footprint)
            if (_model.BossEnemy != null && !_model.BossEnemy.IsDead)
            {
                bool bossHit = GetBossFootprint(_model.BossEnemy.Position).Any(c => aoeCells.Contains(c));
                if (bossHit)
                {
                    _damageService.ApplyDamage(_model.BossEnemy, damage);
                    _bossView?.PlayHitFlash();
                    _bossHPBar?.SetHP(_model.BossEnemy.HP, _model.BossEnemy.MaxHP);
                    Debug.Log($"[BossState] Boss hit! HP={_model.BossEnemy.HP}/{_model.BossEnemy.MaxHP}");
                }
            }

            // Minion hit
            if (slot.Type == PotionType.Acid)
                foreach (var e in _model.Enemies.ToList())
                    if (!e.IsDead && aoeCells.Contains(e.Position)) _damageService.RemoveArmor(e);

            foreach (var enemy in _model.Enemies.ToList())
            {
                if (!enemy.IsDead && aoeCells.Contains(enemy.Position))
                {
                    _damageService.ApplyDamage(enemy, damage);
                    if (_enemyViews.TryGetValue(enemy.EntityId, out var ev)) ev.PlayHitFlash();
                }
            }

            _combatService.ThrowPotion(_model, _inventory, _selectedSlot, cell);
            CreateZonesFromPotion(slot.Type, aoeCells);

            _selectedSlot = -1;
            _gridView.ClearHighlights();
            _inventoryView.RefreshCombat(_inventory, -1);

            _gridView.Run(EnemyTurnRoutine());
        }

        private void OnSkipTurn()
        {
            if (_phase != TurnPhase.PlayerTurn) return;
            _combatService.HealOnSkip(_model);
            if (_healthBarView != null) _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);
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

            RemoveDeadEnemies();

            // Check boss defeat first
            if (_model.BossEnemy == null || _model.BossEnemy.IsDead)
            {
                OnBossVictory();
                yield break;
            }

            yield return new WaitForSeconds(0.25f);

            // Execute boss intent
            ExecuteBossIntent();

            // Execute minion intents (summoned spiders, etc.)
            _aiService.ExecuteIntents(_model);

            // Update positions
            foreach (var enemy in _model.Enemies)
                if (_enemyViews.TryGetValue(enemy.EntityId, out var view))
                    view.PlaceAt(_gridView.GridToWorld(enemy.Position));

            _playerView.PlaceAt(_gridView.GridToWorld(_model.Player.Position));

            yield return new WaitForSeconds(0.15f);

            // Zone effects
            _combatService.ApplyZoneEffects(_model, _damageService);
            if (_healthBarView != null) _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);

            var expiredZones = _combatService.TickZones(_model.Grid);
            foreach (var z in expiredZones)
            {
                // Persistent boss zones (turns == 999) don't really expire; already handled by TickZones
                RemoveZoneOverlay(z.Position);
            }

            _combatService.TickStatuses(_model, _damageService);
            if (_healthBarView != null) _healthBarView.Refresh(_model.Player.HP, _model.Player.MaxHP);

            RemoveDeadEnemies();
            _combatService.StartNextPlayerTurn(_model, _inventory);

            if (_model.Player.HP <= 0)
            {
                OnBossDefeat();
                yield break;
            }

            if (_model.BossEnemy == null || _model.BossEnemy.IsDead)
            {
                OnBossVictory();
                yield break;
            }

            // Phase transition check
            if (!_phaseTransitionDone && _model.BossEnemy.HP <= _model.BossEnemy.MaxHP / 2)
            {
                _phaseTransitionDone = true;
                yield return TriggerPhaseTransition();
            }

            // Cauldron zone generation (Alchemist Phase 2)
            if (_model.BossEnemy?.Type == EnemyType.RenegadeAlchemist &&
                _model.CurrentBossPhase == BossPhase.Phase2)
            {
                foreach (var it in _model.Interactables)
                {
                    if (!it.IsDestroyed && it.Type == InteractableType.Cauldron)
                    {
                        _model.Grid.AddZone(ZoneType.Poison, it.Position, 2);
                        SpawnOrRefreshZoneOverlay(ZoneType.Poison, it.Position);
                    }
                }
            }

            DetermineBossIntent();
            _aiService.DetermineIntents(_model);
            RefreshIntentHighlights();

            _inventoryView.RefreshCombat(_inventory, -1);

            _phase = TurnPhase.PlayerTurn;
            _inputController.SetActive(true);

            Debug.Log($"[BossState] Player turn — HP={_model.Player.HP}/{_model.Player.MaxHP}, boss HP={_model.BossEnemy?.HP}");
        }

        // ── Outcome ────────────────────────────────────────────────────────────

        private void OnBossVictory()
        {
            Debug.Log("[BossState] Boss defeated!");
            EventBus.Publish(new CombatEndedEvent { Victory = true });

            if (_runModel == null || _fsm == null) return;

            _runModel.PersistentHP = _model.Player.HP;
            if (_relicService?.HasRelic(Data.RelicType.Flask) == true)
                _runModel.PersistentHP = Mathf.Min(_runModel.PersistentHP + 1, _runModel.MaxHP);

            _runModel.CurrentFight++;
            _fadeView?.FadeOut(0.2f, () => _fsm.ChangeState(_mapState));
        }

        private void OnBossDefeat()
        {
            Debug.Log("[BossState] Player died to boss.");
            EventBus.Publish(new CombatEndedEvent { Victory = false });

            if (_runModel == null || _fsm == null) return;

            _runModel.LastVictory  = false;
            _runModel.PersistentHP = 0;
            _fadeView?.FadeOut(0.3f, () => _fsm.ChangeState(_resultState));
        }

        // ── Event handlers ─────────────────────────────────────────────────────

        private void OnPlayerDamaged(PlayerDamagedEvent e)
        {
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

        private void OnEnemySpawned(EnemySpawnedEvent e)
        {
            // Minion was added to model.Enemies by ExecuteBossIntent; create its view
            var enemy = _model.Enemies.FirstOrDefault(en => en.EntityId == e.EntityId);
            if (enemy != null && !_enemyViews.ContainsKey(e.EntityId))
                SpawnViewForEnemy(enemy);
        }

        // ── Helpers: boss footprint ────────────────────────────────────────────

        private static IEnumerable<Vector2Int> GetBossFootprint(Vector2Int topLeft)
            => AIHelper.BossFootprint(topLeft);

        // ── Helpers: zone overlays ─────────────────────────────────────────────

        private void SpawnOrRefreshZoneOverlay(ZoneType type, Vector2Int cell)
        {
            int key = cell.x * GridModel.Height + cell.y;
            if (_zoneOverlays.TryGetValue(key, out var existing) && existing != null)
            {
                Object.Destroy(existing.gameObject);
                _zoneOverlays.Remove(key);
            }
            var go = new GameObject($"Zone_{type}_{cell.x}_{cell.y}");
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

        // ── Helpers: interactables ─────────────────────────────────────────────

        private void SpawnInteractableViews()
        {
            foreach (var it in _model.Interactables)
                _interactableViews.Add(CreateInteractableView(it));
        }

        private GameObject CreateInteractableView(InteractableModel it)
        {
            var go     = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name    = $"{it.Type}_{it.Position.x}_{it.Position.y}";
            go.transform.SetParent(_gridView.transform.parent);
            go.transform.position = new Vector3(
                _gridView.GridToWorld(it.Position).x,
                _gridView.GridToWorld(it.Position).y, -0.05f);
            go.transform.localScale = Vector3.one * (it.Type == InteractableType.Pillar ? 0.85f : 0.70f);
            Object.Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<MeshRenderer>();
            var color = it.Type == InteractableType.Pillar
                ? new Color(0.72f, 0.68f, 0.60f)
                : new Color(0.50f, 0.30f, 0.15f);
            rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            // Label for type
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            labelGo.transform.localScale    = Vector3.one * 0.35f;
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text      = it.Type == InteractableType.Pillar ? "I" : "C";
            tm.fontSize  = 18;
            tm.alignment = TextAlignment.Center;
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.color     = Color.white;

            return go;
        }

        private void RefreshInteractableViews()
        {
            // Destroy and recreate — only non-destroyed ones
            DestroyInteractableViews();
            foreach (var it in _model.Interactables)
                if (!it.IsDestroyed)
                    _interactableViews.Add(CreateInteractableView(it));
        }

        private void DestroyInteractableViews()
        {
            foreach (var go in _interactableViews)
                if (go != null) Object.Destroy(go);
            _interactableViews.Clear();
        }

        private void CheckAllCauldronsDestroyed()
        {
            bool allGone = _model.Interactables
                .Where(i => i.Type == InteractableType.Cauldron)
                .All(i => i.IsDestroyed);

            if (allGone)
            {
                _model.CauldronsDestroyed = true;
                Debug.Log("[BossState] All cauldrons destroyed — boss teleport disabled!");
            }
        }

        // ── Helpers: zone creation from potions (mirrors CombatState logic) ───

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
        }

        // ── Helpers: intent highlights ─────────────────────────────────────────

        private void RefreshIntentHighlights()
        {
            _gridView.ClearIntentHighlights();
            var attackCells = new List<Vector2Int>();

            // Boss intent
            if (_model.BossEnemy?.Intent != null)
            {
                if (_bossView != null) _bossView.SetIntent(_model.BossEnemy.Intent);
                var intent = _model.BossEnemy.Intent;
                if (intent.Type == IntentType.Attack ||
                    intent.Type == IntentType.AreaAttack ||
                    intent.Type == IntentType.Teleport)
                    attackCells.AddRange(intent.AttackCells);
            }

            // Minion intents
            foreach (var enemy in _model.Enemies)
            {
                if (enemy.IsDead || enemy.Intent == null) continue;
                if (_enemyViews.TryGetValue(enemy.EntityId, out var view))
                    view.SetIntent(enemy.Intent);
                if (enemy.Intent.Type == IntentType.Attack ||
                    enemy.Intent.Type == IntentType.Explode)
                    attackCells.AddRange(enemy.Intent.AttackCells);
            }

            if (attackCells.Count > 0) _gridView.SetIntentHighlights(attackCells);
        }

        // ── Helpers: enemy views ───────────────────────────────────────────────

        private void SpawnBossView()
        {
            if (_model.BossEnemy == null) return;
            var boss = _model.BossEnemy;

            var go = new GameObject($"Boss_{boss.EntityId}_{boss.Type}");
            go.transform.SetParent(_gridView.transform.parent);
            go.transform.position = new Vector3(0f, 0f, -0.1f);

            _bossView = go.AddComponent<EnemyView>();
            _bossView.Initialize(boss, _gridView.GridToWorld(boss.Position));
        }

        private void SpawnEnemyViews()
        {
            foreach (var enemy in _model.Enemies)
                SpawnViewForEnemy(enemy);
        }

        private void SpawnViewForEnemy(EnemyCombatModel enemy)
        {
            var go = new GameObject($"Minion_{enemy.EntityId}_{enemy.Type}");
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
                _model.Graveyard.Add(d);
                if (_enemyViews.TryGetValue(d.EntityId, out var view))
                {
                    view.PlayDeathFade(() => Object.Destroy(view.gameObject));
                    _enemyViews.Remove(d.EntityId);
                }
            }
        }

        private void DestroyAllEnemyViews()
        {
            foreach (var kv in _enemyViews)
                if (kv.Value != null) Object.Destroy(kv.Value.gameObject);
            _enemyViews.Clear();
        }

        // ── Utilities ──────────────────────────────────────────────────────────

        private static string BossName(int floor) => floor switch
        {
            0 => "Spider Queen",
            1 => "Iron Golem",
            _ => "Renegade Alchemist"
        };

        private static int Manhattan(Vector2Int a, Vector2Int b) =>
            Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
}
