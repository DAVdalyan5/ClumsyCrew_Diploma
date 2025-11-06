using System;

namespace HeistNSeek.Core.StateMachine
{
    /// <summary>
    /// Event published when a state is entered.
    /// </summary>
    public class StateEnteredEvent
    {
        public Type StateType { get; set; }
        public object Payload { get; set; }
    }
}
