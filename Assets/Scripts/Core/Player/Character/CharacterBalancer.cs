using UnityEngine;
using Easy.MessageHub;

namespace Assets.Scripts.Core.Player.Character
{
    //this will be used in future for sloped surfaces
    public class CharacterBalancer
    {
        private readonly IMessageHub messageHub;
        private BalanceInfo currentBalanceInfo;

        public CharacterBalancer(IMessageHub messageHub)
        {
            this.messageHub = messageHub ?? throw new System.ArgumentNullException(nameof(messageHub));

            currentBalanceInfo = new BalanceInfo
            {
                IsBalanced = true,
            };
        }
    }
}
