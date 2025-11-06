using HeistNSeek.Core.StateMachine;
using HeistNSeek.Core.StateMachine.States;
using UnityEngine;
using VContainer.Unity;

namespace HeistNSeek.Core
{
    /// <summary>
    /// EntryPoint for Bootstrap scene initialization.
    /// Runs after all dependencies are resolved in BootstrapLifetimeScope.
    /// Starts the game state machine.
    /// </summary>
    public class BootstrapEntryPoint : IStartable
    {
        private readonly GameStateMachine _stateMachine;
        private readonly int _gameplaySceneIndex;

        public BootstrapEntryPoint(GameStateMachine stateMachine, int gameplaySceneIndex)
        {
            _stateMachine = stateMachine;
            _gameplaySceneIndex = gameplaySceneIndex;
        }

        public void Start()
        {
            Debug.Log("[BootstrapEntryPoint] Bootstrap scene initialized. Starting state machine...");

            // Start the game by entering the BootstrapState
            _stateMachine.Enter<BootstrapState>();
        }
    }
}
