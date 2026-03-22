namespace Assets.Scripts.Events
{
    public class InteractHoldStartEvent
    {
        public float Duration { get; }

        public InteractHoldStartEvent(float duration)
        {
            Duration = duration;
        }
    }
}
