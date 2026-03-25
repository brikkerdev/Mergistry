using Mergistry.Core;
using Mergistry.Data;
using Mergistry.Models;
using Mergistry.Services;
using Mergistry.UI;
using Mergistry.UI.Screens;
using UnityEngine;

namespace Mergistry.GameStates
{
    /// <summary>
    /// Shows the floor map between battles.
    /// On Enter: marks the just-completed node visited, unlocks its successors,
    /// handles boss-node floor transitions or game victory.
    /// On node tap: sets CurrentNodeId and routes to Distillation or EventState.
    /// </summary>
    public class MapState : IGameState
    {
        private readonly MapScreen            _mapScreen;
        private readonly MapGeneratorService  _mapGenerator;
        private readonly GameStateMachine     _fsm;
        private readonly RunModel             _runModel;
        private readonly FadeView             _fadeView;
        private readonly DistillationState    _distillationState;
        private readonly EventState           _eventState;

        // Wired after construction to avoid circular dependencies
        private ResultState _resultState;

        private bool _abortEnter; // set true when Enter() triggers an immediate state change

        public MapState(
            MapScreen           mapScreen,
            MapGeneratorService mapGenerator,
            GameStateMachine    fsm,
            RunModel            runModel,
            FadeView            fadeView,
            DistillationState   distillationState,
            EventState          eventState)
        {
            _mapScreen         = mapScreen;
            _mapGenerator      = mapGenerator;
            _fsm               = fsm;
            _runModel          = runModel;
            _fadeView          = fadeView;
            _distillationState = distillationState;
            _eventState        = eventState;
        }

        public void SetResultState(ResultState resultState) => _resultState = resultState;

        // ── IGameState ────────────────────────────────────────────────────────

        public void Enter()
        {
            _abortEnter = false;

            // Process whichever node was just completed (combat victory or event)
            HandleCompletedNode();
            if (_abortEnter) return; // game-win case kicked off a state change

            // Generate a fresh map if none exists (start of run or after floor advance)
            if (_runModel.FloorMap == null)
            {
                _runModel.FloorMap      = _mapGenerator.GenerateFloor(_runModel.CurrentFloor);
                _runModel.CurrentNodeId = -1;
            }

            _mapScreen.OnNodeClicked += HandleNodeClicked;
            _mapScreen.Show(_runModel.FloorMap, _runModel.CurrentFloor);
            _fadeView?.FadeIn(0.2f, null);

            Debug.Log($"[MapState] Enter — floor={_runModel.CurrentFloor}, " +
                      $"node={_runModel.CurrentNodeId}");
        }

        public void Exit()
        {
            _mapScreen.OnNodeClicked -= HandleNodeClicked;
            _mapScreen.Hide();
        }

        public void Tick() { }

        // ── Node completion ────────────────────────────────────────────────────

        private void HandleCompletedNode()
        {
            if (_runModel.CurrentNodeId < 0) return;

            var map  = _runModel.FloorMap;
            if (map == null) return;

            var node = map.GetNode(_runModel.CurrentNodeId);
            if (node == null || node.IsVisited) return;

            // Mark visited and unlock successors
            node.IsVisited = true;
            foreach (int nextId in node.NextNodeIds)
            {
                var next = map.GetNode(nextId);
                if (next != null) next.IsAccessible = true;
            }

            // Boss node means floor transition or victory
            if (node.Type != MapNodeType.Boss) return;

            if (_runModel.CurrentFloor < 2)
            {
                // Advance to next floor
                _runModel.CurrentFloor++;
                _runModel.FloorMap      = _mapGenerator.GenerateFloor(_runModel.CurrentFloor);
                _runModel.CurrentNodeId = -1;
                Debug.Log($"[MapState] Floor advanced to {_runModel.CurrentFloor}");
            }
            else
            {
                // Floor 2 boss defeated → game won
                _abortEnter           = true;
                _runModel.LastVictory = true;
                _fadeView?.FadeOut(0.3f, () => _fsm.ChangeState(_resultState));
                Debug.Log("[MapState] All floors complete → Victory!");
            }
        }

        // ── Node tap ──────────────────────────────────────────────────────────

        private void HandleNodeClicked(int nodeId)
        {
            var map = _runModel.FloorMap;
            if (map == null) return;

            var node = map.GetNode(nodeId);
            if (node == null || !node.IsAccessible || node.IsVisited) return;

            _runModel.CurrentNodeId   = nodeId;
            _runModel.CurrentNodeType = node.Type;

            Debug.Log($"[MapState] Tapped node {nodeId} ({node.Type})");

            switch (node.Type)
            {
                case MapNodeType.Combat:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    _fadeView?.FadeOut(0.2f, () => _fsm.ChangeState(_distillationState));
                    break;

                case MapNodeType.Event:
                    _fadeView?.FadeOut(0.2f, () => _fsm.ChangeState(_eventState));
                    break;
            }
        }
    }
}
