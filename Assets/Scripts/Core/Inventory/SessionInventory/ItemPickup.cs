using Assets.Scripts.Core.Inventory.Models;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core.Inventory.SessionInventory
{
    /// <summary>
    /// Attach this to items in the scene that can be picked up.
    /// The item remains in the scene (disabled visually) so it can be re-enabled when dropped.
    /// SETUP: Requires TWO colliders:
    ///   1. Main collider (NOT trigger) for physics - prevents falling through ground
    ///   2. Pickup trigger (isTrigger=true) - can be on a child object for pickup detection
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(MeshRenderer))]
    public class ItemPickup : MonoBehaviour
    {
        [Header("Item Configuration")]
        [SerializeField] private ItemDataSO itemData;
        [SerializeField] private int amount = 1;

        [Header("Pickup Settings")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool autoPickup = true;
        [SerializeField] private KeyCode pickupKey = KeyCode.E;
        [Tooltip("Optional: Assign a specific trigger collider for pickup. If null, will find trigger automatically.")]
        [SerializeField] private Collider pickupTrigger;

        [Header("Visual References")]
        [SerializeField] private GameObject visualRoot; // Optional: specific root for visuals

        private SessionInventory _inventory;
        private Collider _physicsCollider;
        private Rigidbody _rigidbody;
        private bool _isPickedUp = false;
        private bool _playerInRange = false;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();

            // Find the physics collider (non-trigger) and pickup trigger (trigger)
            var colliders = GetComponentsInChildren<Collider>();

            foreach (var col in colliders)
            {
                if (col.isTrigger && pickupTrigger == null)
                {
                    pickupTrigger = col;
                }
                else if (!col.isTrigger && _physicsCollider == null)
                {
                    _physicsCollider = col;
                }
            }

            // If no pickup trigger found, create one as a child
            if (pickupTrigger == null)
            {
                Debug.LogWarning($"[ItemPickup] No trigger collider found on {gameObject.name}. Creating one automatically.");
                GameObject triggerObj = new GameObject("PickupTrigger");
                triggerObj.transform.SetParent(transform);
                triggerObj.transform.localPosition = Vector3.zero;

                // Copy the physics collider settings but make it a trigger
                if (_physicsCollider is BoxCollider box)
                {
                    var triggerBox = triggerObj.AddComponent<BoxCollider>();
                    triggerBox.size = box.size * 1.2f; // Slightly larger for easier pickup
                    triggerBox.center = box.center;
                    triggerBox.isTrigger = true;
                    pickupTrigger = triggerBox;
                }
                else if (_physicsCollider is SphereCollider sphere)
                {
                    var triggerSphere = triggerObj.AddComponent<SphereCollider>();
                    triggerSphere.radius = sphere.radius * 1.2f;
                    triggerSphere.center = sphere.center;
                    triggerSphere.isTrigger = true;
                    pickupTrigger = triggerSphere;
                }
                else
                {
                    // Fallback: create a sphere trigger
                    var triggerSphere = triggerObj.AddComponent<SphereCollider>();
                    triggerSphere.radius = 0.5f;
                    triggerSphere.isTrigger = true;
                    pickupTrigger = triggerSphere;
                }
            }

            // If no specific visual root, use self
            if (visualRoot == null)
                visualRoot = gameObject;
        }

      /*  [Inject]
        public void Init(SessionInventory inventory)
        {
            _inventory = inventory;
        }*/
      //TODO: remove or use the above Init method for DI

        private void OnTriggerEnter(Collider other)
        {
            if (_isPickedUp || itemData == null)
                return;

            if (!other.CompareTag(playerTag))
                return;

            _playerInRange = true;

            if (autoPickup)
            {
                TryPickup();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                _playerInRange = false;
            }
        }

        private void Update()
        {
            if (!autoPickup && _playerInRange && !_isPickedUp && Input.GetKeyDown(pickupKey))
            {
                TryPickup();
            }
        }

        //call this function with messegeHub interact event to pick up an item
        private void TryPickup()
        {
            // Try to get inventory from DI first, then fall back to static reference
            if (_inventory == null)
            {
                _inventory = ItemDropper.GetInventory();
            }

            if (_inventory == null)
            {
                Debug.LogWarning($"[ItemPickup] SessionInventory not available on {gameObject.name}. " +
                                "Ensure ItemDropper has been initialized.");
                return;
            }

            if (_inventory.AddItem(itemData, amount))
            {
                OnPickedUp();
            }
        }

        private void OnPickedUp()
        {
            _isPickedUp = true;

            // Disable visuals but keep GameObject active for re-enabling later
            if (visualRoot != null)
                visualRoot.SetActive(false);

            // Disable physics
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
                _rigidbody.detectCollisions = false;
            }

            // Disable colliders
            if (_physicsCollider != null)
                _physicsCollider.enabled = false;

            if (pickupTrigger != null)
                pickupTrigger.enabled = false;

            Debug.Log($"[ItemPickup] Picked up {amount}x '{itemData.itemName}'");
        }

        /// <summary>
        /// Re-enables the item in the scene (called when dropped from inventory).
        /// </summary>
        public void Respawn(Vector3 position)
        {
            transform.position = position;
            _isPickedUp = false;

            if (visualRoot != null)
                visualRoot.SetActive(true);

            // Re-enable physics
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.detectCollisions = true;
            }

            // Re-enable colliders
            if (_physicsCollider != null)
                _physicsCollider.enabled = true;

            if (pickupTrigger != null)
                pickupTrigger.enabled = true;

            Debug.Log($"[ItemPickup] Respawned {itemData.itemName} at {position}");
        }

        private void OnValidate()
        {
            // Validate dual-collider setup
            var colliders = GetComponentsInChildren<Collider>();
            bool hasPhysicsCollider = false;
            bool hasTriggerCollider = false;

            foreach (var col in colliders)
            {
                if (col.isTrigger)
                    hasTriggerCollider = true;
                else
                    hasPhysicsCollider = true;
            }

            if (!hasPhysicsCollider)
            {
                Debug.LogWarning($"[ItemPickup] {gameObject.name} needs a NON-trigger collider for physics (prevents falling through ground)!");
            }

            if (!hasTriggerCollider)
            {
                Debug.LogWarning($"[ItemPickup] {gameObject.name} needs a trigger collider for pickup detection. One will be created automatically at runtime.");
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (itemData != null)
            {
                Gizmos.color = _isPickedUp ? Color.gray : Color.yellow;
                Gizmos.DrawWireSphere(transform.position, 0.2f);
            }
        }
#endif
    }
}
