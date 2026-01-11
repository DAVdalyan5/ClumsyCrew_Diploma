using System;
using UnityEngine;
using UnityEngine.Events;

namespace cowsins
{
    public class TurretProjectile : MonoBehaviour
    {
        private Turret.TargetType targetType;
        private Vector3 direction;
        private float damage, speed, lifetime;

        public UnityEvent<TurretProjectile> destroyEvent;

        public void Initialize(Turret.TargetType targetType, Vector3 direction, float damage, float speed, float duration, Action<TurretProjectile> returnToPool)
        {
            this.targetType = targetType;
            this.direction = direction;
            this.damage = damage;
            this.speed = speed;
            this.lifetime = duration;

            destroyEvent.RemoveAllListeners();
            destroyEvent.AddListener(proj => returnToPool(proj));

            CancelInvoke();
            Invoke(nameof(DestroyProjectile), lifetime);
        }

        private void Update()
        {
            transform.Translate(direction * speed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsValidTarget(other)) return;

            if (targetType == Turret.TargetType.Player && other.TryGetComponent<PlayerStats>(out var player))
                player.Damage(damage, false);

            if (targetType == Turret.TargetType.Enemies && other.TryGetComponent<IDamageable>(out var dmg))
                dmg.Damage(damage, false);

            DestroyProjectile();
        }

        private bool IsValidTarget(Collider other)
        {
            if (targetType == Turret.TargetType.Player) return other.CompareTag("Player");
            if (targetType == Turret.TargetType.Enemies) return other.CompareTag("Enemy");
            return false;
        }

        private void DestroyProjectile() => destroyEvent?.Invoke(this);
    }
}
