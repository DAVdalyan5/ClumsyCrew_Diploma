using Assets.Scripts.Core.Player.Mechanics.Pushing;
using R3;
using R3.Triggers;
using System;
using UnityEngine;

namespace Assets.Scripts.Core.Player.Character
{
    [RequireComponent(typeof(Collider))]
    public class CollisionDetector : MonoBehaviour, IPushable
    {
        private Collider cachedCollider;

        [Header("Detection Parameters")]
        [Tooltip("Velocity magnitude threshold for high-speed impacts")]
        [SerializeField] private float highSpeedThreshold = 5f;

        [Tooltip("Multiplier applied to collision impact calculation")]
        [SerializeField] private float pushImpactModifier = 1f;

        public float HighSpeedThreshold => highSpeedThreshold;
        public float ImpactMultiplier => pushImpactModifier;

        private Action<Collider, CollisionDetector> onCollisionAction;

        private void Awake()
        {
            cachedCollider = GetComponent<Collider>();
            if (!cachedCollider.isTrigger)
            {
                Debug.LogWarning($"[CollisionDetector] Collider on {gameObject.name} is not set as trigger. Collision detection may not work as expected.");
            }
        }

        public IDisposable Subscribe(Action<Collider, CollisionDetector> onCollision)
        {
            //maybe have a more rebust way of handling ths but whatever.
            this.onCollisionAction = onCollision;
            return cachedCollider.OnTriggerEnterAsObservable()
                                 .Subscribe(collision =>  onCollision?.Invoke(collision, this));
        }

        public void OnPushed(float force)
        {
            // Trigger the collision detection logic when pushed via raycast
            // Invoke the same callback that would be called on a trigger collision
            onCollisionAction?.Invoke(cachedCollider, this);
        }
    }
}
