using UnityEngine;

namespace HeistNSeek.Core.Player
{
    /// <summary>
    /// Resolves player by GameObject tag. Use "Player" tag on the player GameObject.
    /// </summary>
    public class PlayerProviderByTag : IPlayerProvider
    {
        private const string PlayerTag = "Player";

        public Transform GetPlayer()
        {
            var playerGo = GameObject.FindGameObjectWithTag(PlayerTag);
            return playerGo != null ? playerGo.transform : null;
        }
    }
}
