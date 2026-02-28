using Assets.Scripts.Runtime.Helpers;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts.Core.Character
{
    public static class RagdollUtilities
    {
        public static void ToggleRagdoll(GameObject character, bool state)
        {
            var rigidbodies = character.GetComponentsInChildren<Rigidbody>();
            var animator = character.GetComponentInChildren<Animator>();

            if (rigidbodies.IsNullOrEmpty()) return;

            foreach (var rb in rigidbodies)
            {
                rb.isKinematic = !state;
                rb.GetComponentInParent<Collider>().enabled = state;
            }

            if (animator != null)
            {
                animator.enabled = !state;
            }
        }

        public static bool IsRagdollEnabled(GameObject character)
        {
            var rigidbodies = character.GetComponentsInChildren<Rigidbody>();
            var colliders = character.GetComponentsInChildren<Collider>();
            var animator = character.GetComponentInChildren<Animator>();

            if (rigidbodies.IsNullOrEmpty() || colliders.IsNullOrEmpty())
            {
                return false;
            }

            return rigidbodies.All(rb => !rb.isKinematic) && !animator.enabled;
        }

        /// <summary>
        /// Applies force to the ragdoll's root Rigidbody (typically pelvis).
        /// </summary>
        /// <param name="ragdollRoot">Root GameObject of the ragdoll hierarchy.</param>
        /// <param name="force">Force vector to apply.</param>
        /// <param name="mode">Force mode (default: Impulse).</param>
        /// <param name="preferredBone">Optional specific Rigidbody to use; if null, uses first non-kinematic in hierarchy.</param>
        public static void ApplyForceToRagdoll(GameObject ragdollRoot, Vector3 force, ForceMode mode = ForceMode.Impulse, Rigidbody preferredBone = null)
        {
            if (ragdollRoot == null) return;

            var target = preferredBone;
            if (target == null)
            {
                var rigidbodies = ragdollRoot.GetComponentsInChildren<Rigidbody>();
                if (rigidbodies.IsNullOrEmpty()) return;
                target = rigidbodies[0];
            }

            target.AddForce(force, mode);
        }
    }
}
