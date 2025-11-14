using UnityEngine;
using Easy.MessageHub;

namespace Assets.Scripts.Core.Player.Character
{
    public class CharacterBalancer
    {
        private readonly float highSpeedThreshold;
        private readonly IMessageHub messageHub;
        private BalanceInfo currentBalanceInfo;

        public CharacterBalancer(IMessageHub messageHub, float impactSpeedThreshold = 5f)
        {
            this.messageHub = messageHub ?? throw new System.ArgumentNullException(nameof(messageHub));

            currentBalanceInfo = new BalanceInfo
            {
                IsBalanced = true,
            };

            highSpeedThreshold = impactSpeedThreshold;
        }

        /// <summary>
        /// Call this from OnCollisionEnter to detect high-speed impacts.
        /// Publishes BalanceLostEvent when balance is lost.
        /// </summary>
        public void OnCollision(float collisionSpeed)
        {
            if (collisionSpeed > highSpeedThreshold && currentBalanceInfo.IsBalanced)
            {
                currentBalanceInfo.IsBalanced = false;
                messageHub.Publish(new BalanceLostEvent(collisionSpeed));
                Debug.Log($"[CharacterBalancer] Balance lost! Impact speed: {collisionSpeed}");
            }
        }

        /// <summary>
        /// Manually reset the balance state (called when player presses recovery key).
        /// Publishes BalanceRegainedEvent when balance is regained.
        /// </summary>
        public void RegainBalance()
        {
            if (!currentBalanceInfo.IsBalanced)
            {
                currentBalanceInfo.IsBalanced = true;
                messageHub.Publish(new BalanceRegainedEvent());
                Debug.Log("[CharacterBalancer] Balance regained!");
            }
        }

        public BalanceInfo GetBalanceInfo()
        {
            return currentBalanceInfo;
        }
    }
}
