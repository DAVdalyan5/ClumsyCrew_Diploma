namespace Assets.Scripts.Core.Player.Character
{
    /// <summary>
    /// Event published when character loses balance.
    /// </summary>
    public class BalanceLostEvent
    {
        public float ImpactSpeed { get; }

        public BalanceLostEvent(float impactSpeed)
        {
            ImpactSpeed = impactSpeed;
        }
    }
}
