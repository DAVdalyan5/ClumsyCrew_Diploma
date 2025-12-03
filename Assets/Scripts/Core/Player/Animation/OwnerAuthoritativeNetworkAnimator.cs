using Unity.Netcode.Components;

namespace Assets.Scripts.Core.Player.Animation
{
    public class OwnerAuthoritativeNetworkAnimator : NetworkAnimator
    {
        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
