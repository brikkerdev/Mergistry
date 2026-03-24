namespace Mergistry.Core
{
    public interface IGameState
    {
        void Enter();
        void Exit();
        void Tick();
    }
}
