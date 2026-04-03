using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using HeistNSeek.Core;
using HeistNSeek.Core.Player;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.UI.DamageIndicator
{
    /// <summary>
    /// Manages damage direction indicators centered on the crosshair.
    /// Each indicator rotates to point toward the source of incoming damage,
    /// continuously tracks the attacker direction as the camera rotates,
    /// and fades out automatically.
    ///
    /// Setup:
    ///  1. Attach to a full-screen Canvas child (the "DamageIndicatorRoot" panel).
    ///  2. Populate IndicatorPool with pre-made child Image GameObjects.
    ///  3. Set Sprite Rotation Offset to match your sprite's default orientation
    ///     (90 for right-facing, 0 for upward-facing, etc.).
    /// </summary>
    /// 
    ///  doc for future session,
    ///C:\Users\ghaza\.claude\projects\D--Projects-UnityProjects-HeistNSeek\memory\project_damage_indicator.md
    ///
    public class DamageDirectionIndicator : MonoBehaviour
    {
        [Header("Indicator Settings")]
        [Tooltip("Pre-spawned indicator Image GameObjects. All must be children of this object.")]
        [SerializeField] private List<Image> indicatorPool = new();

        [Tooltip("How long (seconds) before a triggered indicator fully fades to zero.")]
        [SerializeField] private float fadeDuration = 1.8f;

        [Tooltip("Starting alpha when a new hit triggers an indicator.")]
        [SerializeField] [Range(0f, 1f)] private float startAlpha = 0.85f;

        [Tooltip("Distance (pixels) from screen centre where indicators appear.")]
        [SerializeField] private float distanceFromCenter = 80f;

        [Tooltip("Rotation offset to compensate for the sprite's default orientation. " +
                 "90 = sprite points RIGHT by default, 0 = sprite points UP by default.")]
        [SerializeField] private float spriteRotationOffset = 90f;

        private IMessageHub _messageHub;
        private Transform _playerTransform;
        private Transform _cameraTransform;  // Camera driven by Cinemachine – primary look reference.
        private Transform _yawTransform;     // Player-root fallback (yaw-only).

        // Per-indicator state: attacker world position and fade coroutine.
        private Vector3[] _attackerPositions;
        private Coroutine[] _fadeCoroutines;
        private int _nextIndex;

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

            int count = indicatorPool.Count;
            _fadeCoroutines = new Coroutine[count];
            _attackerPositions = new Vector3[count];

            // Force every indicator to anchor at screen centre and hide.
            foreach (var img in indicatorPool)
            {
                var rt = img.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;

                var c = img.color;
                c.a = 0f;
                img.color = c;
                img.gameObject.SetActive(false);
            }

            _messageHub?.SubscribeSafe<LocalPlayerSpawnedEvent>(this, OnLocalPlayerSpawned);
            _messageHub?.SubscribeSafe<DamageTakenEvent>(this, OnDamageTaken);
        }

        private void LateUpdate()
        {
            // Re-resolve references every frame so stale/null refs don't silently block updates.
            if (_cameraTransform == null)
                _cameraTransform = Camera.main?.transform;
            if (_yawTransform == null)
                TryResolvePlayerReferences();

            if (_cameraTransform == null && _yawTransform == null) return;

            for (int i = 0; i < indicatorPool.Count; i++)
            {
                if (indicatorPool[i].gameObject.activeSelf)
                    UpdateIndicatorTransform(i);
            }
        }

        // ── Event handlers ───────────────────────────────────────────────────

        private void OnLocalPlayerSpawned(LocalPlayerSpawnedEvent evt)
        {
            _playerTransform = evt.PlayerTransform;
            _cameraTransform = evt.PlayerCamera?.transform;
            ResolveYawTransform(evt.PlayerTransform);
        }

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            if (indicatorPool.Count == 0) return;

            if (_cameraTransform == null)
                _cameraTransform = Camera.main?.transform;
            if (_cameraTransform == null && _yawTransform == null)
                TryResolvePlayerReferences();
            if (_cameraTransform == null && _yawTransform == null) return;

            ActivateIndicator(evt.AttackerWorldPosition);
        }

        // ── Reference resolution ─────────────────────────────────────────────

        private void TryResolvePlayerReferences()
        {
            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer == null) return;

            if (_playerTransform == null)
                _playerTransform = localPlayer.transform;

            if (_yawTransform == null)
                ResolveYawTransform(localPlayer.transform);
        }

        /// <summary>
        /// Finds the transform whose Y-rotation equals the player's current look yaw.
        /// FirstPersonMovementHandler applies yaw via transform.Rotate on its own transform,
        /// so its transform.forward is the authoritative horizontal look direction.
        /// </summary>
        private void ResolveYawTransform(Transform playerRoot)
        {
            if (_yawTransform != null) return;

            var fph = playerRoot.GetComponentInChildren<FirstPersonMovementHandler>(true);
            if (fph != null)
            {
                _yawTransform = fph.transform;
                return;
            }

            // Fallback: the player root itself (NetworkObject root also rotates for movement direction)
            _yawTransform = playerRoot;
        }

        // ── Direction math ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the signed angle (degrees) from the camera's forward to the attacker,
        /// projected onto the horizontal plane. 0 = ahead, 90 = right, -90 = left, ±180 = behind.
        /// Uses Atan2 with explicit dot products to avoid Unity's left-hand SignedAngle handedness issues.
        /// </summary>
        private float CalculateAngle(Vector3 attackerWorldPos)
        {
            // Prefer the camera transform (definitive rendered view direction).
            // Fall back to yaw-only player root if camera isn't available yet.
            Transform reference = _cameraTransform != null ? _cameraTransform : _yawTransform;
            if (reference == null) return 0f;

            Vector3 toAttacker = attackerWorldPos - reference.position;
            toAttacker.y = 0f;

            if (toAttacker.sqrMagnitude < 0.001f)
                return 0f;

            toAttacker.Normalize();

            // Flatten the reference axes to the horizontal plane.
            Vector3 refRight   = reference.right;   refRight.y   = 0f; refRight.Normalize();
            Vector3 refForward = reference.forward;  refForward.y = 0f; refForward.Normalize();

            if (refForward.sqrMagnitude < 0.001f || refRight.sqrMagnitude < 0.001f)
                return 0f;

            float x = Vector3.Dot(toAttacker, refRight);    // +1 = attacker to camera's right
            float z = Vector3.Dot(toAttacker, refForward);  // +1 = attacker ahead

            // Atan2(x, z): 0 = ahead, +90 = right, -90 = left, ±180 = behind.
            return Mathf.Atan2(x, z) * Mathf.Rad2Deg;
        }

        // ── Pool management & transform ──────────────────────────────────────

        private void ActivateIndicator(Vector3 attackerWorldPos)
        {
            int index = _nextIndex % indicatorPool.Count;
            _nextIndex++;

            _attackerPositions[index] = attackerWorldPos;

            Image img = indicatorPool[index];

            // Set initial position and rotation.
            UpdateIndicatorTransform(index);

            // Reset colour and make visible.
            var col = img.color;
            col.a = startAlpha;
            img.color = col;
            img.gameObject.SetActive(true);

            // Restart fade coroutine for this slot.
            if (_fadeCoroutines[index] != null)
                StopCoroutine(_fadeCoroutines[index]);
            _fadeCoroutines[index] = StartCoroutine(FadeOut(img, index));
        }

        private void UpdateIndicatorTransform(int index)
        {
            float angle = CalculateAngle(_attackerPositions[index]);

            RectTransform rt = indicatorPool[index].rectTransform;

            // Place on a circle around screen centre.
            float rad = angle * Mathf.Deg2Rad;
            rt.anchoredPosition = new Vector2(
                Mathf.Sin(rad) * distanceFromCenter,
                Mathf.Cos(rad) * distanceFromCenter
            );

            // Rotate the sprite to face outward, compensating for its default orientation.
            rt.localRotation = Quaternion.Euler(0f, 0f, -angle + spriteRotationOffset);
        }

        private IEnumerator FadeOut(Image img, int index)
        {
            float elapsed = 0f;
            Color col = img.color;
            float initialAlpha = col.a;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                col.a = Mathf.Lerp(initialAlpha, 0f, elapsed / fadeDuration);
                img.color = col;
                yield return null;
            }

            col.a = 0f;
            img.color = col;
            img.gameObject.SetActive(false);
            _fadeCoroutines[index] = null;
        }

        // ── MessageHub fallback ──────────────────────────────────────────────

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
