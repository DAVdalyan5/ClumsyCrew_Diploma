using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using Easy.MessageHub;

namespace HeistNSeek.Core.StateMachine
{
    /// <summary>
    /// Main game state machine that manages state transitions.
    /// All states are registered via VContainer dependency injection.
    /// </summary>
    public class GameStateMachine
    {
        private readonly Dictionary<Type, IState> _states = new();
        private readonly IMessageHub _messageHub;
        private IState _activeState;

        [Inject]
        public GameStateMachine(IEnumerable<IState> states, IMessageHub messageHub)
        {
            _messageHub = messageHub;

            foreach (var state in states)
            {
                state.SetStateMachine(this);
                _states[state.GetType()] = state;
            }

            Debug.Log($"[GameStateMachine] Registered {_states.Count} states.");
        }

        /// <summary>
        /// Transitions to a new state.
        /// </summary>
        /// <typeparam name="TState">Type of state to enter</typeparam>
        /// <param name="payload">Optional data to pass to the new state</param>
        public void Enter<TState>(object payload = null) where TState : class, IState
        {
            var stateType = typeof(TState);

            // Exit current state
            if (_activeState != null)
            {
                var exitingStateName = _activeState.GetType().Name;
                _activeState.Exit();
                Debug.Log($"[GameStateMachine] Exited state: {exitingStateName}");

                // Publish state exit event
                _messageHub?.Publish(new StateExitedEvent { StateType = _activeState.GetType() });
            }

            // Enter new state
            TState newState = GetState<TState>();
            _activeState = newState;
            newState.Enter(payload);

            Debug.Log($"[GameStateMachine] Entered state: {stateType.Name}");

            // Publish state enter event
            _messageHub?.Publish(new StateEnteredEvent { StateType = stateType, Payload = payload });
        }

        /// <summary>
        /// Gets the currently active state.
        /// </summary>
        public IState ActiveState => _activeState;

        /// <summary>
        /// Gets a specific state by type.
        /// </summary>
        private TState GetState<TState>() where TState : class, IState
        {
            var stateType = typeof(TState);

            if (!_states.ContainsKey(stateType))
            {
                throw new InvalidOperationException($"State {stateType.Name} is not registered in GameStateMachine.");
            }

            return _states[stateType] as TState;
        }
    }
}
