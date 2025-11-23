using Assets.Scripts.Events.Actions;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using System.Collections;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core
{
    public class CharacterPusher : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] public Transform CameraTransform;

        [Header("Push Settings")]
        [SerializeField] private float pushSpeed = 10f;
        [SerializeField] private float pushDistance = 3f;
        [SerializeField] private float returnSpeed = 5f;

        private IMessageHub _messageHub;

        private new Collider collider;
        private Vector3 initialLocalPosition;
        private bool isPushing = false;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            // Get the collider component from this GameObject
            collider = GetComponent<Collider>();

            if (collider == null)
            {
                Debug.LogError($"[CharacterPusher] No Collider component found on {gameObject.name}!");
            }

            // Store initial local position for returning after push
            initialLocalPosition = transform.localPosition;

            // Auto-find camera if not assigned
            if (CameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    CameraTransform = mainCamera.transform;
                    Debug.Log($"[CharacterPusher] Auto-assigned Main Camera to {gameObject.name}");
                }
                else
                {
                    Debug.LogWarning($"[CharacterPusher] No camera assigned and Main Camera not found for {gameObject.name}!");
                }
            }
        }

        private void Start()
        {
            this._messageHub.SubscribeSafe<PushEvent>(this, _ => PerformPush());
        }

        private void Update()
        {
            // Always track camera rotation to stay synced
            FollowCameraRotation();
        }

        /// <summary>
        /// Syncs the collider's rotation with the camera's rotation to avoid desyncing.
        /// </summary>
        private void FollowCameraRotation()
        {
            if (CameraTransform == null) return;

            // Match the camera's rotation exactly
            transform.rotation = CameraTransform.rotation;
        }

        /// <summary>
        /// Performs a push action: moves the collider forward at set speed,
        /// then returns it to initial position and resumes camera tracking.
        /// </summary>
        private void PerformPush()
        {
            StartCoroutine(PerformRaycastPush());
        }

        private IEnumerator PushCoroutine()
        {
            isPushing = true;

            // Capture the push direction at the start (camera's forward direction in world space)
            Vector3 pushDirection = CameraTransform != null ? CameraTransform.forward : transform.forward;
            Vector3 startPosition = transform.localPosition;

            // Push forward using the captured direction
            float distance = 0f;
            while (distance < pushDistance)
            {
                float step = pushSpeed * Time.deltaTime;
                // Move in world space using the captured direction
                transform.position += pushDirection * step;
                distance += step;
                yield return null;
            }

            // Return to initial position (local space)
            while (Vector3.Distance(transform.localPosition, initialLocalPosition) > 0.01f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    initialLocalPosition,
                    returnSpeed * Time.deltaTime
                );
                yield return null;
            }

            // Ensure exact position
            transform.localPosition = initialLocalPosition;

            isPushing = false;
        }

        public IEnumerator PerformRaycastPush()
        {
            //add cooldown
            Vector3 pushDirection = CameraTransform != null ? CameraTransform.forward : transform.forward;

            if (CameraTransform != null && Physics.Raycast(CameraTransform.position, pushDirection, out RaycastHit hit, pushDistance))
            {
                // Don't push yourself - check if hit object is in the same player hierarchy
                if (hit.collider.transform.root == transform.root)
                {
                    Debug.DrawRay(CameraTransform.position, pushDirection * pushDistance, Color.yellow, 2000);
                    yield return null;
                    yield break;
                }

                var pushable = hit.collider.GetComponent<Assets.Scripts.Core.Player.Mechanics.Pushing.IPushable>();
                pushable?.OnPushed(pushSpeed);
            }
            Debug.DrawRay(CameraTransform.position, pushDirection * pushDistance, Color.green, 2000);

            yield return null;
        }

        /// <summary>
        /// Resets the collider to its initial position immediately.
        /// </summary>
        public void ResetPosition()
        {
            if (isPushing)
            {
                StopAllCoroutines();
                isPushing = false;
            }

            transform.localPosition = initialLocalPosition;
        }
    }
}
