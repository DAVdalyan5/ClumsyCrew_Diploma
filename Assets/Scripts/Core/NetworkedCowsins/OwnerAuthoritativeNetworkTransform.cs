using Unity.Netcode.Components;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Owner-authoritative NetworkTransform (clients move their own player, others interpolate).
    /// Mirrors the project's OwnerAuthoritativeNetworkAnimator pattern.
    /// </summary>
    public class OwnerAuthoritativeNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}

