using Mergistry.Core;
using Mergistry.UI;
using Mergistry.UI.Screens;

namespace Mergistry.GameStates
{
    public class MenuState : IGameState
    {
        private readonly MenuScreenView   _menuScreen;
        private readonly GameStateMachine _fsm;
        private readonly IGameState       _nextState;
        private readonly FadeView         _fadeView;

        public MenuState(MenuScreenView menuScreen, GameStateMachine fsm, IGameState nextState, FadeView fadeView = null)
        {
            _menuScreen = menuScreen;
            _fsm        = fsm;
            _nextState  = nextState;
            _fadeView   = fadeView;
        }

        public void Enter()
        {
            _menuScreen.Show();
            _menuScreen.OnStartClicked += HandleStart;
            _fadeView?.FadeIn(0.2f, null);
        }

        public void Exit()
        {
            _menuScreen.OnStartClicked -= HandleStart;
            _menuScreen.Hide();
        }

        public void Tick() { }

        private void HandleStart() => _fsm.ChangeState(_nextState);
    }
}
