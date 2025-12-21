using System;

namespace HeistNSeek.Core.StateMachine
{
    /// <summary>
    /// Event published when a state is exited.
    /// </summary>
    public class StateExitedEvent
    {
        public Type StateType { get; set; }
    }
}
