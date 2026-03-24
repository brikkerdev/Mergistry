using Mergistry.Core;
using Mergistry.UI.Screens;

namespace Mergistry.GameStates
{
    public class MenuState : IGameState
    {
        private readonly MenuScreenView  _menuScreen;
        private readonly GameStateMachine _fsm;
        private readonly IGameState       _nextState;

        public MenuState(MenuScreenView menuScreen, GameStateMachine fsm, IGameState nextState)
        {
            _menuScreen = menuScreen;
            _fsm        = fsm;
            _nextState  = nextState;
        }

        public void Enter()
        {
            _menuScreen.Show();
            _menuScreen.OnStartClicked += HandleStart;
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
