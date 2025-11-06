using Assets.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeistNSeek.Core.StateMachine.States
{
    /// <summary>
    /// Initial state that runs when the game starts.
    /// Handles bootstrap initialization and transitions to gameplay.
    /// Basically preload
    /// </summary>
    public class BootstrapState : BaseState
    {
        private readonly int _mainSceneIndex;

        public BootstrapState(int mainSceneIndex = 1)
        {
            _mainSceneIndex = mainSceneIndex;
        }

        public override void Enter(object payload = default)
        {
            Debug.Log("[BootstrapState] Initializing game...");

            // Perform any bootstrap initialization here
            // (e.g., loading configs, initializing services, etc.)

            // Transition to loading the main scene
            StateMachine.Enter<LoadingState>(_mainSceneIndex);
        }

        public override void Exit()
        {
            Debug.Log("[BootstrapState] Exiting...");
        }
    }
}
