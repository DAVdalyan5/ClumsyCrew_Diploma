using UnityEngine;

namespace HeistNSeek.Core.StateMachine.States
{
    /// <summary>
    /// State representing active gameplay.
    /// This is where the main game loop runs.
    /// </summary>
    public class GameplayState : BaseState
    {
        public override void Enter(object payload = default)
        {
            Debug.Log("[GameplayState] Gameplay started.");

            // Initialize gameplay systems here
            // (e.g., spawn player, start game timer, enable input, etc.)
        }

        public override void Exit()
        {
            Debug.Log("[GameplayState] Gameplay ended.");

            // Cleanup gameplay systems here
            // (e.g., despawn entities, save state, etc.)
        }
    }
}
