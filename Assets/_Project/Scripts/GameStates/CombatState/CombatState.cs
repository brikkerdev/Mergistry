using Mergistry.Core;
using Mergistry.Models.Combat;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.HUD;
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
        private readonly InventoryView         _inventoryView;
        private readonly FadeView              _fadeView;

        private CombatModel _model;

        public CombatState(
            GridView              gridView,
            PlayerView            playerView,
            CombatInputController inputController,
            CombatService         combatService,
            InventoryView         inventoryView,
            FadeView              fadeView)
        {
            _gridView        = gridView;
            _playerView      = playerView;
            _inputController = inputController;
            _combatService   = combatService;
            _inventoryView   = inventoryView;
            _fadeView        = fadeView;
        }

        public void Enter()
        {
            _model = _combatService.InitCombat();

            _gridView.gameObject.SetActive(true);
            _playerView.gameObject.SetActive(true);
            _inventoryView.gameObject.SetActive(true);

            var startWorld = _gridView.GridToWorld(_model.Player.Position);
            _playerView.PlaceAt(startWorld);

            _inputController.Initialize(_gridView, _playerView, _model, _combatService);
            _inputController.OnMoveRequested = OnMoveRequested;
            _inputController.SetActive(true);

            _fadeView.FadeIn(0.2f, null);

            Debug.Log("[CombatState] Entered — player at (1,1)");
        }

        public void Exit()
        {
            _inputController.SetActive(false);
            _inputController.OnMoveRequested = null;
            _gridView.gameObject.SetActive(false);
            _playerView.gameObject.SetActive(false);
        }

        public void Tick() { }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnMoveRequested(Vector2Int target)
        {
            if (_model.Player.HasMoved) return;

            _model.Player.Position = target;
            _model.Player.HasMoved = true;

            _playerView.MoveTo(_gridView.GridToWorld(target));
            Debug.Log($"[CombatState] Player moved to {target}");
        }
    }
}
