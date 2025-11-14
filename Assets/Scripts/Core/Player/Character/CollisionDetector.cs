using R3;
using R3.Triggers;
using System;
using UnityEngine;

namespace Assets.Scripts.Core.Player.Character
{
    [RequireComponent(typeof(Collider))]
    public class CollisionDetector : MonoBehaviour
    {
        private Collider cachedCollider;

        [Header("Detection Parameters")]
        [Tooltip("Velocity magnitude threshold for high-speed impacts")]
        [SerializeField] private float highSpeedThreshold = 5f;

        [Tooltip("Multiplier applied to collision impact calculation")]
        [SerializeField] private float impactMultiplier = 1f;

        public float HighSpeedThreshold => highSpeedThreshold;
        public float ImpactMultiplier => impactMultiplier;

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
            return cachedCollider.OnTriggerEnterAsObservable()
                                 .Subscribe(collision =>  onCollision?.Invoke(collision, this));
        }
    }
}
