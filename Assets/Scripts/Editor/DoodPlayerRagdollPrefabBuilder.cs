using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HeistNSeek.Editor
{
    /// <summary>
    /// Builds <see cref="OutputPrefabPath"/> from <see cref="DoodModelPath"/> with a Humanoid ragdoll
    /// and the placeholder animator controller used on the networked player.
    /// </summary>
    public static class DoodPlayerRagdollPrefabBuilder
    {
        private const string DoodModelPath = "Assets/Models/dood.fbx";
        private const string OutputPrefabPath = "Assets/Prefabs/Player/DoodPlayerRagdoll.prefab";
        private const string AnimatorControllerPath = "Assets/Animations/RioController.controller";

        [MenuItem("HeistNSeek/Build Dood Player Ragdoll Prefab")]
        public static void BuildFromMenu()
        {
            BuildInternal();
        }

        /// <summary>Unity batchmode: -executeMethod HeistNSeek.Editor.DoodPlayerRagdollPrefabBuilder.BuildForBatch</summary>
        public static void BuildForBatch()
        {
            BuildInternal();
        }

        private static void BuildInternal()
        {
            var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DoodModelPath);
            if (modelPrefab == null)
            {
                Debug.LogError($"[DoodPlayerRagdollPrefabBuilder] Missing model at {DoodModelPath}");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorControllerPath);
            var avatar = AssetDatabase.LoadAllAssetsAtPath(DoodModelPath).OfType<Avatar>().FirstOrDefault(a => a != null);
            if (avatar == null || !avatar.isHuman)
            {
                Debug.LogError("[DoodPlayerRagdollPrefabBuilder] dood.fbx needs a valid Humanoid Avatar (Rig tab in Model importer).");
                return;
            }

            var root = new GameObject("PlayerBody");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
            instance.name = "Dood";
            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            var animator = instance.GetComponentInChildren<Animator>();
            if (animator == null)
                animator = instance.AddComponent<Animator>();

            animator.runtimeAnimatorController = controller;
            animator.avatar = avatar;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            animator.updateMode = AnimatorUpdateMode.Fixed;

            foreach (var col in instance.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(col, true);

            AddHumanoidRagdoll(animator);

            var directory = Path.GetDirectoryName(OutputPrefabPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath, out bool success);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (success)
                Debug.Log($"[DoodPlayerRagdollPrefabBuilder] Saved {OutputPrefabPath}");
            else
                Debug.LogError("[DoodPlayerRagdollPrefabBuilder] Prefab save failed.");
        }

        private static void AddHumanoidRagdoll(Animator anim)
        {
            var hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null)
            {
                Debug.LogError("[DoodPlayerRagdollPrefabBuilder] Hips bone missing — Humanoid mapping incomplete.");
                return;
            }

            var ragdollBodies = new Dictionary<Transform, Rigidbody>();

            Transform neckOrSpineTop = anim.GetBoneTransform(HumanBodyBones.Neck);
            Transform chest = anim.GetBoneTransform(HumanBodyBones.Chest);
            if (chest == null)
                chest = anim.GetBoneTransform(HumanBodyBones.UpperChest);
            var spine = anim.GetBoneTransform(HumanBodyBones.Spine);

            var specs = new List<(Transform bone, Transform child, Transform parentForJoint, float mass)>();

            specs.Add((hips, FirstChildPreferLegs(hips), null, 3.2f));

            if (spine != null && spine != hips)
                specs.Add((spine, chest != null && chest != spine ? chest : neckOrSpineTop, hips, 2.2f));

            if (chest != null && chest != hips && chest != spine)
            {
                var chestChild = neckOrSpineTop != null ? neckOrSpineTop : anim.GetBoneTransform(HumanBodyBones.Head);
                specs.Add((chest, chestChild, spine != null ? spine : hips, 3.1f));
            }

            var headBone = anim.GetBoneTransform(HumanBodyBones.Head);
            if (neckOrSpineTop != null)
            {
                specs.Add((neckOrSpineTop, headBone, chest ?? spine ?? hips, 1.2f));
                if (headBone != null)
                    specs.Add((headBone, null, neckOrSpineTop, 1.1f));
            }
            else if (headBone != null)
            {
                var headParent = chest ?? spine ?? hips;
                if (headParent != headBone)
                    specs.Add((headBone, null, headParent, 1.1f));
            }

            AddLimbChain(anim, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, hips, specs, 2f, 1.5f);
            AddLimbChain(anim, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, hips, specs, 2f, 1.5f);
            AddLimbChain(anim, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, chest ?? spine ?? hips, specs, 1.25f, 0.85f);
            AddLimbChain(anim, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, chest ?? spine ?? hips, specs, 1.25f, 0.85f);

            foreach (var (bone, _, _, _) in specs)
            {
                if (bone == null || ragdollBodies.ContainsKey(bone))
                    continue;

                foreach (var rb in bone.GetComponents<Rigidbody>())
                    Object.DestroyImmediate(rb, true);
            }

            foreach (var (bone, child, _, mass) in specs)
            {
                if (bone == null || ragdollBodies.ContainsKey(bone))
                    continue;

                var rb = bone.gameObject.AddComponent<Rigidbody>();
                rb.mass = mass;
                rb.linearDamping = 0f;
                rb.angularDamping = 0.05f;
                rb.useGravity = true;
                rb.isKinematic = false;
                ragdollBodies[bone] = rb;
                AddCapsuleColliderForBone(bone, child);
            }

            foreach (var (bone, _, parentForJoint, _) in specs)
            {
                if (bone == null || parentForJoint == null || bone == hips)
                    continue;

                if (!ragdollBodies.TryGetValue(parentForJoint, out var parentRb) || parentRb == null)
                    continue;

                foreach (var j in bone.GetComponents<CharacterJoint>())
                    Object.DestroyImmediate(j, true);

                var joint = bone.gameObject.AddComponent<CharacterJoint>();
                joint.connectedBody = parentRb;
                joint.autoConfigureConnectedAnchor = true;

                var lowTwist = joint.lowTwistLimit;
                lowTwist.limit = -20f;
                joint.lowTwistLimit = lowTwist;

                var highTwist = joint.highTwistLimit;
                highTwist.limit = 20f;
                joint.highTwistLimit = highTwist;

                var swing1 = joint.swing1Limit;
                swing1.limit = 30f;
                joint.swing1Limit = swing1;

                var swing2 = joint.swing2Limit;
                swing2.limit = 0f;
                joint.swing2Limit = swing2;
            }
        }

        private static void AddLimbChain(
            Animator anim,
            HumanBodyBones upper,
            HumanBodyBones lower,
            Transform rootForJoint,
            List<(Transform bone, Transform child, Transform parentForJoint, float mass)> specs,
            float upperMass,
            float lowerMass)
        {
            var u = anim.GetBoneTransform(upper);
            var l = anim.GetBoneTransform(lower);
            if (u == null)
                return;

            specs.Add((u, l, rootForJoint, upperMass));
            if (l != null)
                specs.Add((l, null, u, lowerMass));
        }

        private static Transform FirstChildPreferLegs(Transform hips)
        {
            Transform fallback = null;
            foreach (Transform c in hips)
            {
                fallback ??= c;
                var n = c.name.ToLowerInvariant();
                if (n.Contains("leg") || n.Contains("up") || n.Contains("thigh"))
                    return c;
            }

            return fallback;
        }

        private static void AddCapsuleColliderForBone(Transform bone, Transform childEnd)
        {
            foreach (var c in bone.GetComponents<CapsuleCollider>())
                Object.DestroyImmediate(c, true);

            var capsule = bone.gameObject.AddComponent<CapsuleCollider>();

            if (childEnd != null)
            {
                var worldD = childEnd.position - bone.position;
                var localDir = bone.InverseTransformDirection(worldD);
                var height = worldD.magnitude;
                capsule.height = Mathf.Max(0.08f, height);
                capsule.radius = Mathf.Max(0.04f, height * 0.18f);
                var abs = new Vector3(Mathf.Abs(localDir.x), Mathf.Abs(localDir.y), Mathf.Abs(localDir.z));
                if (abs.y >= abs.x && abs.y >= abs.z)
                {
                    capsule.direction = 1;
                    capsule.center = bone.InverseTransformPoint((bone.position + childEnd.position) * 0.5f);
                }
                else if (abs.x >= abs.y && abs.x >= abs.z)
                {
                    capsule.direction = 0;
                    capsule.center = bone.InverseTransformPoint((bone.position + childEnd.position) * 0.5f);
                }
                else
                {
                    capsule.direction = 2;
                    capsule.center = bone.InverseTransformPoint((bone.position + childEnd.position) * 0.5f);
                }
            }
            else
            {
                capsule.height = 0.15f;
                capsule.radius = 0.07f;
                capsule.direction = 1;
                capsule.center = Vector3.zero;
            }
        }
    }
}
