using cowsins;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Clears Cowsins static singletons when session services are destroyed so stale Instance references do not persist.
    /// Attach to the root of CowsinsSessionServices (same object as SoundManager, or parent of PoolManager).
    /// </summary>
    public sealed class CowsinsSingletonLifetimeGuard : MonoBehaviour
    {
        private void OnDestroy()
        {
            ClearPoolManager();
            ClearSoundManager();
            ClearAddonManager();
        }

        private void ClearPoolManager()
        {
            var pm = GetComponentInChildren<PoolManager>(true);
            if (pm != null && PoolManager.Instance == pm)
                PoolManager.Instance = null;
        }

        private void ClearSoundManager()
        {
            var sm = GetComponent<SoundManager>();
            if (sm == null)
                sm = GetComponentInChildren<SoundManager>(true);
            if (sm != null && SoundManager.Instance == sm)
                SoundManager.Instance = null;
        }

        private void ClearAddonManager()
        {
            var am = GetComponent<AddonManager>();
            if (am == null)
                am = GetComponentInChildren<AddonManager>(true);
            if (am != null && AddonManager.instance == am)
                AddonManager.instance = null;
        }
    }
}
