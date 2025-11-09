using UnityEngine;

namespace Assets.Scripts.Core.Player.Character
{
    public class CharacterBalancer
    {
        //TODO: the character cannot trip, since hes always steadyu.
        //calculate his way up relative to the ground, so if even he is standing straigh. calcuate if he should fall.
        //
        //rigidbody is only needed her for the velocity no other thing.
        //speed calculation can be done with the speed provided by first person controller.

        public struct BalanceInfo
        {
            public bool IsBalanced;
            public bool IsTippingForward;
            public bool IsTippingBackward;
            public bool HasHighSpeedImpact;
            public float ForwardTiltAngle;
            public Vector3 LastImpactVelocity;
        }

        private readonly float forwardTiltThreshold;
        private readonly float backwardTiltThreshold;
        private readonly float highSpeedThreshold;
        private readonly Transform characterTransform;

        private BalanceInfo currentBalanceInfo;

        #region Constructor

        /// <summary>
        /// Initialize the CharacterBalancer
        /// </summary>
        /// <param name="transform">Character's transform (typically the root/hips)</param>
        /// <param name="forwardTilt">Maximum forward tilt angle before considered unbalanced (degrees)</param>
        /// <param name="backwardTilt">Maximum backward tilt angle before considered unbalanced (degrees)</param>
        /// <param name="speedThreshold">Velocity magnitude threshold for high-speed impacts</param>
        public CharacterBalancer(
            Transform transform,
            float forwardTilt = 45f,
            float backwardTilt = 30f,
            float speedThreshold = 5f)
        {
            characterTransform = transform;
            forwardTiltThreshold = forwardTilt;
            backwardTiltThreshold = backwardTilt;
            highSpeedThreshold = speedThreshold;

            currentBalanceInfo = new BalanceInfo
            {
                IsBalanced = true,
                IsTippingForward = false,
                IsTippingBackward = false,
                HasHighSpeedImpact = false,
                ForwardTiltAngle = 0f,
                LastImpactVelocity = Vector3.zero
            };
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Call this in FixedUpdate to update balance information.
        /// </summary>
        public BalanceInfo CheckBalance()
        {
            if (characterTransform == null)
            {
                Debug.LogError("[CharacterBalancer] Transform  is null!");
                return currentBalanceInfo;
            }

            // Calculate forward tilt angle
            Vector3 forward = characterTransform.forward;
            forward.y = 0; // Project onto horizontal plane
            forward.Normalize();

            Vector3 actualForward = characterTransform.forward;
            float forwardTiltAngle = Vector3.Angle(forward, actualForward);

            // Determine if tilting forward or backward
            float dot = Vector3.Dot(actualForward, Vector3.down);
            bool isTiltingForward = dot > 0;

            currentBalanceInfo.ForwardTiltAngle = forwardTiltAngle;
            currentBalanceInfo.IsTippingForward = isTiltingForward && forwardTiltAngle > forwardTiltThreshold;
            currentBalanceInfo.IsTippingBackward = !isTiltingForward && forwardTiltAngle > backwardTiltThreshold;

            // Update balance state
            currentBalanceInfo.IsBalanced = !currentBalanceInfo.IsTippingForward &&
                                           !currentBalanceInfo.IsTippingBackward &&
                                           !currentBalanceInfo.HasHighSpeedImpact;

            return currentBalanceInfo;
        }

        /// <summary>
        /// Call this from OnCollisionEnter to detect high-speed impacts.
        /// </summary>
        public void OnCollision(float collisionSpeed)
        {
            if (collisionSpeed > highSpeedThreshold)
            {
                currentBalanceInfo.HasHighSpeedImpact = true;
                currentBalanceInfo.IsBalanced = false;
            }
        }

        /// <summary>
        /// Reset high-speed impact flag. Call this when character recovers balance.
        /// </summary>
        public void ResetImpactFlag()
        {
            currentBalanceInfo.HasHighSpeedImpact = false;
            currentBalanceInfo.LastImpactVelocity = Vector3.zero;
        }

        public BalanceInfo GetBalanceInfo()
        {
            return currentBalanceInfo;
        }

        #endregion
    }
}
