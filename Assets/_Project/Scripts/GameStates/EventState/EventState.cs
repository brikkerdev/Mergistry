using Mergistry.Core;
using Mergistry.Models;
using Mergistry.UI;
using Mergistry.UI.Screens;
using UnityEngine;

namespace Mergistry.GameStates
{
    /// <summary>
    /// Stub event state. Shows a simple flavour screen and routes back to MapState.
    /// The visited/successor-unlock logic lives in MapState.Enter() so this state
    /// only needs to transition back.
    /// </summary>
    public class EventState : IGameState
    {
        private readonly EventScreenView  _screen;
        private readonly GameStateMachine _fsm;
        private readonly FadeView         _fadeView;

        // Wired after construction to avoid circular dependency
        private MapState _mapState;

        public EventState(
            EventScreenView  screen,
            GameStateMachine fsm,
            FadeView         fadeView)
        {
            _screen  = screen;
            _fsm     = fsm;
            _fadeView = fadeView;
        }

        public void SetMapState(MapState mapState) => _mapState = mapState;

        // ── IGameState ────────────────────────────────────────────────────────

        public void Enter()
        {
            _screen.Show("Событие", "Таинственный знак встречает вас...\nЧто скрывается впереди?");
            _screen.OnContinueClicked += OnContinue;
            _fadeView?.FadeIn(0.2f, null);
            Debug.Log("[EventState] Enter");
        }

        public void Exit()
        {
            _screen.OnContinueClicked -= OnContinue;
            _screen.Hide();
        }

        public void Tick() { }

        private void OnContinue()
        {
            _fadeView?.FadeOut(0.2f, () => _fsm.ChangeState(_mapState));
        }
    }
}
