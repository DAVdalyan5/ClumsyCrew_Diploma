using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using HeistNSeek.Core.Enemy;
using System.Collections;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.UI
{
    /// <summary>
    /// Plays the child particle systems on this GameObject when the owning entity takes damage,
    /// holds them for a configurable duration, then fades them out evenly.
    ///
    /// Setup:
    ///  - On the player: place as a world-space scene object (not under enemy hierarchy).
    ///    Subscribes automatically to DamageTakenEvent via MessageHub.
    ///  - On an enemy: place as a child of the enemy GameObject.
    ///    Driven directly by NetworkedEnemyHealth via TriggerEffect() — no event subscription.
    ///  No VContainer registration needed — IMessageHub is resolved via fallback.
    /// </summary>
    public class DamageParticleEffect : MonoBehaviour
    {
        [Tooltip("Total time (seconds) the particle effect is active before fully disappearing.")]
        [SerializeField] private float duration = 1f;

        [Tooltip("Portion of 'duration' (seconds) spent fading out. Must be less than or equal to duration.")]
        [SerializeField] private float fadeOutDuration = 0.3f;

        private IMessageHub _messageHub;
        private ParticleSystem[] _particleSystems;
        private ParticleSystem.Particle[] _particleBuffer;
        private Coroutine _activeCoroutine;
        private bool _isOnEnemy;

        // ── Dependency injection ─────────────────────────────────────────────

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            EnsureMessageHubResolved();

            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);

            // Pre-allocate a buffer large enough for any single particle system.
            int maxParticles = 0;
            foreach (var ps in _particleSystems)
                maxParticles = Mathf.Max(maxParticles, ps.main.maxParticles);
            _particleBuffer = new ParticleSystem.Particle[maxParticles];

            // Make sure all systems start stopped and clear.
            foreach (var ps in _particleSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            // Only subscribe to the player damage event when this effect is NOT on an enemy.
            // Enemy hits are driven directly via TriggerEffect() called from NetworkedEnemyHealth.
            _isOnEnemy = GetComponentInParent<NetworkedEnemyHealth>() != null;
            if (!_isOnEnemy)
                _messageHub?.SubscribeSafe<DamageTakenEvent>(this, OnDamageTaken);
        }

        private void OnDestroy()
        {
            if (_activeCoroutine != null)
                StopCoroutine(_activeCoroutine);
        }

        // ── Public trigger ───────────────────────────────────────────────────

        /// <summary>
        /// Triggers the damage particle effect, rotated so it appears on the side opposite
        /// the damage source (e.g. attacker in front → particles at back).
        /// </summary>
        public void TriggerEffect(Vector3 sourcePosition)
        {
            ApplyDirectionRotation(sourcePosition);

            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                ClearAllParticles();
            }

            _activeCoroutine = StartCoroutine(PlayAndFadeCoroutine());
        }

        // ── Event handler (player) ───────────────────────────────────────────

        private void OnDamageTaken(DamageTakenEvent evt) => TriggerEffect(evt.AttackerWorldPosition);

        // ── Direction ────────────────────────────────────────────────────────

        private void ApplyDirectionRotation(Vector3 sourcePosition)
        {
            Vector3 entityCenter = GetEntityCenter();
            Vector3 toSource = sourcePosition - entityCenter;
            toSource.y = 0f;

            if (toSource.sqrMagnitude < 0.001f) return;

            // Face AWAY from the attacker — particles appear on the hit side.
            transform.rotation = Quaternion.LookRotation(-toSource.normalized, Vector3.up);
        }

        private Vector3 GetEntityCenter()
        {
            if (_isOnEnemy)
                return transform.position;

            // Player effect is a standalone scene object — find the local player object.
            var localPlayer = Unity.Netcode.NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer != null)
                return localPlayer.transform.position;

            return Camera.main != null ? Camera.main.transform.position : transform.position;
        }

        // ── Coroutine ────────────────────────────────────────────────────────

        private IEnumerator PlayAndFadeCoroutine()
        {
            // Play all particle systems from a clean state.
            foreach (var ps in _particleSystems)
                ps.Play(true);

            float clampedFade = Mathf.Clamp(fadeOutDuration, 0f, duration);
            float holdTime = duration - clampedFade;

            // Hold phase — let particles run at full opacity.
            if (holdTime > 0f)
                yield return new WaitForSeconds(holdTime);

            // Fade phase — each frame, reduce all live particles' alpha.
            float elapsed = 0f;
            while (elapsed < clampedFade)
            {
                elapsed += Time.deltaTime;
                float fadeProgress = Mathf.Clamp01(elapsed / clampedFade);

                foreach (var ps in _particleSystems)
                {
                    int count = ps.GetParticles(_particleBuffer);
                    for (int i = 0; i < count; i++)
                    {
                        Color c = _particleBuffer[i].startColor;
                        c.a *= (1f - fadeProgress);
                        _particleBuffer[i].startColor = c;
                    }
                    ps.SetParticles(_particleBuffer, count);
                }

                yield return null;
            }

            ClearAllParticles();
            _activeCoroutine = null;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void ClearAllParticles()
        {
            foreach (var ps in _particleSystems)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void EnsureMessageHubResolved()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<global::BootstrapLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();
            if (scope != null)
                _messageHub = scope.Container.Resolve<IMessageHub>();
        }
    }
}
