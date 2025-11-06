namespace HeistNSeek.Core.StateMachine
{
    /// <summary>
    /// Base class for game states providing common functionality.
    /// Inherit from this class to create concrete states.
    /// </summary>
    public abstract class BaseState : IState
    {
        public GameStateMachine StateMachine { get; private set; }

        public void SetStateMachine(GameStateMachine stateMachine)
        {
            StateMachine = stateMachine;
        }

        public abstract void Enter(object payload = default);
        public abstract void Exit();
    }
}
