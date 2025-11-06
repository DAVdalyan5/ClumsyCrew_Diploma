using HeistNSeek.Core.StateMachine.States;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core.StateMachine
{
    /// <summary>
    /// Example script demonstrating how to use the GameStateMachine.
    /// This shows how to inject and interact with the state machine from any script.
    /// </summary>
    public class StateMachineExample : MonoBehaviour
    {
        private GameStateMachine _stateMachine;

        [Inject]
        public void Init(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        // Example: Transition to a different state
        public void TransitionToMenu()
        {
            _stateMachine.Enter<MenuState>();
        }

        // Example: Load a specific scene
        public void LoadScene(int sceneIndex)
        {
            _stateMachine.Enter<LoadingState>(sceneIndex);
        }

        // Example: Start gameplay
        public void StartGameplay()
        {
            _stateMachine.Enter<GameplayState>();
        }

        // Example: Get current state info
        public void LogCurrentState()
        {
            var currentState = _stateMachine.ActiveState;
            if (currentState != null)
            {
                Debug.Log($"Current State: {currentState.GetType().Name}");
            }
            else
            {
                Debug.Log("No active state.");
            }
        }
    }
}
