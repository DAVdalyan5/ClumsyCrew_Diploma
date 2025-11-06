namespace HeistNSeek.Core.StateMachine
{
    /// <summary>
    /// Base interface for all game states.
    /// States handle specific phases of the game (Bootstrap, Loading, Gameplay, etc.)
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Reference to the GameStateMachine that owns this state.
        /// </summary>
        GameStateMachine StateMachine { get; }

        /// <summary>
        /// Called when entering this state.
        /// </summary>
        /// <param name="payload">Optional data passed during state transition</param>
        void Enter(object payload = default);

        /// <summary>
        /// Called when exiting this state.
        /// </summary>
        void Exit();

        /// <summary>
        /// Sets the state machine reference. Called by GameStateMachine during registration.
        /// </summary>
        void SetStateMachine(GameStateMachine stateMachine);
    }
}
