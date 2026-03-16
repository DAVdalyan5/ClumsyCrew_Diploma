using UnityEngine;

namespace HeistNSeek.Core.Player
{
    /// <summary>
    /// Provides the player transform for enemy targeting.
    /// Allows different resolution strategies (tag, local player, etc.).
    /// </summary>
    public interface IPlayerProvider
    {
        Transform GetPlayer();
    }
}
