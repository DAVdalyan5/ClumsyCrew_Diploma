using UnityEngine;

namespace Assets.Scripts.Core.Player.Character
{
    public class CharacterBalancer
    {
        private readonly float highSpeedThreshold;
        private readonly float recoveryTime;
        private BalanceInfo currentBalanceInfo;
        private float timeSinceLastImpact;

        public CharacterBalancer(float impactSpeedThreshold = 5f, float recoveryDuration = 1f)
        {
            currentBalanceInfo = new BalanceInfo
            {
                IsBalanced = true,
            };

            highSpeedThreshold = impactSpeedThreshold;
            recoveryTime = recoveryDuration;
            timeSinceLastImpact = float.MaxValue;  // Start as not recovering
        }

        /// <summary>
        /// Call this in FixedUpdate to update balance information and handle recovery.
        /// </summary>
        public BalanceInfo CheckBalance()
        {
            // Only increment and check if currently recovering
            if (timeSinceLastImpact < recoveryTime)
            {
                timeSinceLastImpact += Time.deltaTime;

                // Automatically reset balance when recovery time expires
                if (timeSinceLastImpact >= recoveryTime)
                {
                    ResetImpact();
                }
                else
                {
                    // Still recovering
                    currentBalanceInfo.IsBalanced = false;
                }
            }

            return currentBalanceInfo;
        }

        /// <summary>
        /// Call this from OnCollisionEnter to detect high-speed impacts.
        /// </summary>
        public void OnCollision(float collisionSpeed)
        {
            if (collisionSpeed > highSpeedThreshold)
            {
                // Always reset the timer on new impact, even if already recovering
                timeSinceLastImpact = 0f;
                currentBalanceInfo.IsBalanced = false;
            }
        }

        /// <summary>
        /// Manually reset the impact state and begin balance recovery.
        /// </summary>
        public void ResetImpact()
        {
            currentBalanceInfo.IsBalanced = true;
            timeSinceLastImpact = float.MaxValue;  // No longer recovering
        }

        public BalanceInfo GetBalanceInfo()
        {
            return currentBalanceInfo;
        }
    }
}
