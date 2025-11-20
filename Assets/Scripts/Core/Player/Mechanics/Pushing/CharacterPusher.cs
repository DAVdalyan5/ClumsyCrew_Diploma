using UnityEngine;
using VContainer;
using Easy.MessageHub;
using System.Collections;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Assets.Scripts.Events.Actions;

namespace HeistNSeek.Core
{
    /// <summary>
    /// Controls a collider that follows camera movement and can push forward when triggered.
    /// Attach to PusherCollider GameObject.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CharacterPusher : MonoBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] private Transform cameraTransform;

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
            if (cameraTransform == null)
            {
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    cameraTransform = mainCamera.transform;
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
            if (cameraTransform == null) return;

            // Match the camera's rotation exactly
            transform.rotation = cameraTransform.rotation;
        }

        /// <summary>
        /// Performs a push action: moves the collider forward at set speed,
        /// then returns it to initial position and resumes camera tracking.
        /// </summary>
        private void PerformPush()
        {
            if (!isPushing)
            {
                StartCoroutine(PushCoroutine());
            }
        }

        private IEnumerator PushCoroutine()
        {
            isPushing = true;

            // Capture the push direction at the start (camera's forward direction in world space)
            Vector3 pushDirection = cameraTransform != null ? cameraTransform.forward : transform.forward;
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
            isPushing = true;

            Vector3 pushDirection = cameraTransform != null ? cameraTransform.forward : transform.forward; 
            Vector3 startPosition = transform.localPosition;

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
