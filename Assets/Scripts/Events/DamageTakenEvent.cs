using UnityEngine;

namespace Assets.Scripts.Events
{
    /// <summary>
    /// Published on the local owner client when they take damage.
    /// AttackerWorldPosition is the world-space position of whoever dealt the damage.
    /// </summary>
    public class DamageTakenEvent
    {
        public Vector3 AttackerWorldPosition { get; }
        public float Damage { get; }

        public DamageTakenEvent(Vector3 attackerWorldPosition, float damage)
        {
            AttackerWorldPosition = attackerWorldPosition;
            Damage = damage;
        }
    }
}
