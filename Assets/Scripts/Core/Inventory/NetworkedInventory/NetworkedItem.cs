using Assets.Scripts.Core.Inventory.Models;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.Inventory.NetworkedInventory
{
    /// <summary>
    /// Networked world item that can be picked up by players.
    /// Server-authoritative: pickup validation happens on server.
    /// Clients see the same items in the same positions.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedItem : NetworkBehaviour, ICollectable
    {
        [Header("Visual Settings")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private MeshFilter meshFilter;

        [Header("Pickup Settings")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool autoPickup = true;
        [SerializeField] private KeyCode pickupKey = KeyCode.E;
        [SerializeField] private float pickupCooldown = 0.5f;

        [Header("Pre-placed Item Settings")]
        [Tooltip("For items placed in scene: assign the ItemDataSO here. The item ID will be set from this on spawn.")]
        [SerializeField] private ItemDataSO preplacedItemData;

        // Network synced item ID
        private readonly NetworkVariable<FixedString64Bytes> _itemId = new(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        // Network synced pickup state
        private readonly NetworkVariable<bool> _isPickedUp = new(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private Collider _triggerCollider;
        private Rigidbody _rigidbody;
        private bool _playerInRange = false;
        private float _lastPickupAttempt = 0f;
        private ulong _nearbyPlayerClientId;

        public string ItemId => _itemId.Value.ToString();
        public bool IsPickedUp => _isPickedUp.Value;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            SetupColliders();

            if (visualRoot == null)
            {
                visualRoot = gameObject;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Subscribe to network variable changes
            _itemId.OnValueChanged += OnItemIdChanged;
            _isPickedUp.OnValueChanged += OnPickedUpStateChanged;

            // Server: Initialize item ID from pre-placed data if not already set
            if (IsServer && preplacedItemData != null && string.IsNullOrEmpty(_itemId.Value.ToString()))
            {
                string itemId = ItemRegistry.GetItemId(preplacedItemData);
                if (!string.IsNullOrEmpty(itemId))
                {
                    _itemId.Value = itemId;
                    Debug.Log($"[NetworkedItem] Initialized pre-placed item with ID: {itemId}");
                }
            }

            // Apply current state
            if (!string.IsNullOrEmpty(_itemId.Value.ToString()))
            {
                UpdateVisuals();
            }

            // Register with manager (for pre-placed scene items)
            // This ensures items placed in scene are tracked for pickup
            if (IsServer && NetworkedItemSpawnManager.Instance != null)
            {
                NetworkedItemSpawnManager.Instance.RegisterSceneItem(this);
            }

            Debug.Log($"[NetworkedItem] Spawned. ItemId: {_itemId.Value}, IsServer: {IsServer}");
        }

        public override void OnNetworkDespawn()
        {
            _itemId.OnValueChanged -= OnItemIdChanged;
            _isPickedUp.OnValueChanged -= OnPickedUpStateChanged;
            base.OnNetworkDespawn();
        }

        private void SetupColliders()
        {
            // Find or create trigger collider
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                if (col.isTrigger)
                {
                    _triggerCollider = col;
                    break;
                }
            }

            if (_triggerCollider == null)
            {
                // Create a trigger collider
                var triggerObj = new GameObject("PickupTrigger");
                triggerObj.transform.SetParent(transform);
                triggerObj.transform.localPosition = Vector3.zero;

                var sphereTrigger = triggerObj.AddComponent<SphereCollider>();
                sphereTrigger.radius = 1f;
                sphereTrigger.isTrigger = true;
                _triggerCollider = sphereTrigger;

                // Need to forward trigger events from child
                var forwarder = triggerObj.AddComponent<TriggerForwarder>();
                forwarder.Initialize(this);
            }
        }

        /// <summary>
        /// Server-only: Initialize the item after network spawn
        /// </summary>
        public void InitializeItem(string itemId)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkedItem] InitializeItem called on client!");
                return;
            }

            _itemId.Value = itemId;
            UpdateVisuals();
        }

        private void OnItemIdChanged(FixedString64Bytes oldValue, FixedString64Bytes newValue)
        {
            UpdateVisuals();
        }

        private void OnPickedUpStateChanged(bool oldValue, bool newValue)
        {
            if (newValue)
            {
                // Item was picked up - hide visuals
                if (visualRoot != null)
                {
                    visualRoot.SetActive(false);
                }

                // Disable colliders
                if (_triggerCollider != null)
                {
                    _triggerCollider.enabled = false;
                }

                // Disable physics
                if (_rigidbody != null)
                {
                    _rigidbody.isKinematic = true;
                    _rigidbody.detectCollisions = false;
                }
            }
        }

        private void UpdateVisuals()
        {
            string itemId = _itemId.Value.ToString();
            if (string.IsNullOrEmpty(itemId)) return;

            var itemData = ItemRegistry.GetItemData(itemId);
            if (itemData == null)
            {
                Debug.LogWarning($"[NetworkedItem] Item data not found for: {itemId}");
                return;
            }

            // If item has a prefab with mesh, try to use it
            if (itemData.prefab != null)
            {
                var prefabMeshFilter = itemData.prefab.GetComponentInChildren<MeshFilter>();
                var prefabMeshRenderer = itemData.prefab.GetComponentInChildren<MeshRenderer>();

                if (prefabMeshFilter != null && meshFilter != null)
                {
                    meshFilter.sharedMesh = prefabMeshFilter.sharedMesh;
                }

                if (prefabMeshRenderer != null && meshRenderer != null)
                {
                    // Preserve any additional materials (like outline materials) that were added after the base materials
                    var currentMaterials = meshRenderer.sharedMaterials;
                    var prefabMaterials = prefabMeshRenderer.sharedMaterials;

                    // If we have more materials than the prefab (e.g., outline materials were added),
                    // preserve the extra materials
                    if (currentMaterials.Length > prefabMaterials.Length)
                    {
                        var newMaterials = new Material[currentMaterials.Length];
                        // Copy prefab materials first
                        for (int i = 0; i < prefabMaterials.Length; i++)
                        {
                            newMaterials[i] = prefabMaterials[i];
                        }
                        // Keep extra materials (like outline materials)
                        for (int i = prefabMaterials.Length; i < currentMaterials.Length; i++)
                        {
                            newMaterials[i] = currentMaterials[i];
                        }
                        meshRenderer.sharedMaterials = newMaterials;
                    }
                    else
                    {
                        meshRenderer.sharedMaterials = prefabMaterials;
                    }
                }
            }

            Debug.Log($"[NetworkedItem] Updated visuals for: {itemId}");
        }

        private void OnTriggerEnter(Collider other)
        {
            HandleTriggerEnter(other);
        }

        public void HandleTriggerEnter(Collider other)
        {
            if (_isPickedUp.Value) return;

            if (!other.CompareTag(playerTag)) return;

            // Get the player's network identity
            var playerNetworkObject = other.GetComponentInParent<NetworkObject>();
            if (playerNetworkObject == null) return;

            _playerInRange = true;
            _nearbyPlayerClientId = playerNetworkObject.OwnerClientId;

            if (autoPickup)
            {
                ExecutePickup();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            HandleTriggerExit(other);
        }

        public void HandleTriggerExit(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                _playerInRange = false;
            }
        }

        //private void Update()
        //{
        //    // Only the local player can trigger pickup with key
        //    if (!autoPickup && _playerInRange && !_isPickedUp.Value)
        //    {
        //        //change to input system
        //        if (Input.GetKeyDown(pickupKey))
        //        {
        //            ExecutePickup();
        //        }
        //    }
        //}

        public void ExecutePickup()
        {
            // Cooldown check
            if (Time.time - _lastPickupAttempt < pickupCooldown) return;
            _lastPickupAttempt = Time.time;

            if (_isPickedUp.Value) return;

            // Only the nearby player can pick up
            if (!_playerInRange) return;

            // Find the local player's client ID
            ulong localClientId = NetworkManager.Singleton.LocalClientId;

            // Only allow pickup if the local player is the one in range
            if (_nearbyPlayerClientId != localClientId) return;

            // Request pickup from server via manager
            if (NetworkedItemSpawnManager.Instance != null)
            {
                var networkObject = GetComponent<NetworkObject>();
                NetworkedItemSpawnManager.Instance.RequestPickupItem(
                    networkObject.NetworkObjectId,
                    localClientId
                );
            }
            else
            {
                Debug.LogWarning("[NetworkedItem] NetworkedItemSpawnManager not found!"); //her error
            }
        }

        public void ForcePickup(ulong playerClientId)
        {
            // Cooldown check
            if (Time.time - _lastPickupAttempt < pickupCooldown) return;
            _lastPickupAttempt = Time.time;

            if (_isPickedUp.Value) return;

            // Request pickup from server via manager
            if (NetworkedItemSpawnManager.Instance != null)
            {
                var networkObject = GetComponent<NetworkObject>();
                NetworkedItemSpawnManager.Instance.RequestPickupItem(
                    networkObject.NetworkObjectId,
                    playerClientId
                );
            }
            else
            {
                Debug.LogWarning("[NetworkedItem] NetworkedItemSpawnManager not found!");
            }
        }

        /// <summary>
        /// Server-only: Mark this item as picked up
        /// </summary>
        public void MarkAsPickedUp()
        {
            if (!IsServer) return;
            _isPickedUp.Value = true;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = _isPickedUp.Value ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Draw item name
            if (!string.IsNullOrEmpty(_itemId.Value.ToString()))
            {
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, _itemId.Value.ToString());
            }
        }
#endif
    }

    /// <summary>
    /// Helper component to forward trigger events from child colliders
    /// </summary>
    public class TriggerForwarder : MonoBehaviour
    {
        private NetworkedItem _parent;

        public void Initialize(NetworkedItem parent)
        {
            _parent = parent;
        }

        private void OnTriggerEnter(Collider other)
        {
            _parent?.HandleTriggerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            _parent?.HandleTriggerExit(other);
        }
    }
}
