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
    }
}
