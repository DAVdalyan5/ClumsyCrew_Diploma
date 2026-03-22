using cowsins;
using R3;
using R3.Triggers;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Listens to the Player's main Rigidbody OnCollisionEnter and triggers ragdoll on high-speed impact.
    /// This is the primary path for impact detection because body-part trigger colliders are inside
    /// the main capsule and never reach obstacles (the capsule hits first).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class PlayerRigidbodyImpactDetector : MonoBehaviour
    {
        [Header("Impact Threshold")]
        [Tooltip("Minimum impact speed (relativeVelocity.magnitude) to trigger ragdoll. Raised to 9 so minor bumps don't trigger.")]
        [SerializeField] private float highSpeedThreshold = 9f;
        [Tooltip("Scale applied to force magnitude when threshold exceeded.")]
        [SerializeField] private float impactForceScale = 1f;
        [Tooltip("Minimum seconds between ragdoll triggers.")]
        [SerializeField] private float impactCooldownSeconds = 0.2f;
        [Tooltip("Colliders to capture ragdoll triggers")]
        [SerializeField] private List<Collider> collidersToCapture;

        private NetworkedCowsinsRagdollController _ragdollController;
        private NetworkedCowsinsPlayerController _playerController;
        private float _lastImpactTime;

        private void Start()
        {
            _ragdollController = GetComponentInParent<NetworkedCowsinsRagdollController>();
            _playerController = GetComponentInParent<NetworkedCowsinsPlayerController>();
            if (_playerController != null && !_playerController.IsOwner)
                enabled = false;

            collidersToCapture.ForEach(c => c.OnTriggerEnterAsObservable().Subscribe(CollisionEntered));
        }

        /// <summary>
        /// The logic: compute the direction from the player's center to the other collider's center — if that direction dot
        /// Vector3.down > 0.5f (more than ~60° pointing downward), it's underneath the player and treated as ground, so the
        /// impact is skipped.
        /// The 0.5f threshold means only things that are clearly below you are ignored.A wall to the side will have a near-zero
        /// or negative dot product with Vector3.down and will still trigger ragdoll normally.
        /// </summary>
        private static bool IsBelowPlayer(Collider other, Transform playerTransform)
        {
            //this not used, but couuld be if needed.
            Vector3 toOther = other.bounds.center - playerTransform.position;
            return Vector3.Dot(toOther.normalized, Vector3.down) > 0.5f;
        }

        private void CollisionEntered(Collider other)
        {
            if (_ragdollController == null || _playerController == null || !_playerController.IsOwner)
                return;
            if (other.transform.IsChildOf(_playerController.transform))
                return;
            if (other.CompareTag("Ground"))
                return;

            var rb = GetComponent<Rigidbody>();
            if (rb == null) return;

            var planarVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float impactSpeed = planarVelocity.magnitude;
            if (impactSpeed <= highSpeedThreshold)
                return;
            if (Time.unscaledTime - _lastImpactTime < impactCooldownSeconds)
                return;

            _lastImpactTime = Time.unscaledTime;
            Vector3 forceVector = planarVelocity.normalized * (impactSpeed - highSpeedThreshold) * impactForceScale;
            if (forceVector.sqrMagnitude < 0.01f)
                forceVector = -transform.forward * 5f;

            _ragdollController.TriggerRagdollFromImpactServerRpc(forceVector, impactSpeed);
        }
    }
}

//pushHandler detects when a player runs into a wall or obstacle � it watches the body-part trigger zones and checks
//  "was I moving fast enough when this trigger fired?"

//  rbImpactDet detects the physics collision itself � Unity's physics engine tells you the exact relative velocity at the
//   moment of impact, which is the "true" collision force.

//  ---
//  In practice:

//  -pushHandler is imprecise � trigger colliders don't give you collision force data, so it has to estimate speed by
//  sampling position every frame. It can fire even if you lightly brush a wall while moving fast, because it only checks
//  "were you moving fast" not "did the impact actually stop you hard."
//  - rbImpactDet is precise � collision.relativeVelocity is the actual physics impulse, so it only fires when something
//  genuinely hit hard. It's the "correct" way to detect impact damage.

//  ---
//  Why both exist:

//  The trigger-based approach (pushHandler / CollisionDetector) was the original system, designed around body-part
//  hitboxes. The rbImpactDet was added later as a comment in the file says � "body-part trigger colliders are inside the
//  main capsule and never reach obstacles (the capsule hits first)" � meaning the trigger body parts are buried inside
//  the character and physically can never touch a wall because the outer capsule collider blocks it first. So pushHandler
//   probably barely ever fires from environmental collisions; it's mainly useful for the IPushable / OnPushed() path
//  (when another player pushes you via raycast)