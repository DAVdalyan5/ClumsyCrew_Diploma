using UnityEngine;

namespace HeistNSeek.UI
{
    /// <summary>
    /// Base class for all UI views. Attach to root GameObject of each view.
    /// </summary>
    public abstract class UIView : MonoBehaviour
    {
        /// <summary>
        /// Called when the view is shown. Override to handle incoming data.
        /// </summary>
        /// <param name="data">Optional data passed from previous view</param>
        public virtual void OnShow(object data = null)
        {
        }

        /// <summary>
        /// Called when the view is hidden.
        /// </summary>
        public virtual void OnHide()
        {
        }

        internal void Show(object data = null)
        {
            gameObject.SetActive(true);
            OnShow(data);
        }

        internal void Hide()
        {
            OnHide();
            gameObject.SetActive(false);
        }
    }
}
