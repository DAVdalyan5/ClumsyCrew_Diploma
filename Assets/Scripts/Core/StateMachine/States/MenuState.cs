using UnityEngine;

namespace HeistNSeek.Core.StateMachine.States
{
    /// <summary>
    /// Not Used right now, could be used in future
    /// Example state for main menu.
    /// Optional state - not registered by default.
    /// </summary>
    public class MenuState : BaseState
    {
        public override void Enter(object payload = default)
        {
            Debug.Log("[MenuState] Entered main menu.");

            // Show menu UI, enable menu input, etc.
        }

        public override void Exit()
        {
            Debug.Log("[MenuState] Exiting main menu.");

            // Hide menu UI, disable menu input, etc.
        }

        public void StartGame(int sceneIndex)
        {
            // Transition to loading state to load the game scene
            StateMachine.Enter<LoadingState>(sceneIndex);
        }
    }
}
