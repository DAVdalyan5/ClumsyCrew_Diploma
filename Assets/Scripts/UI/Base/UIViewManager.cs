using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeistNSeek.UI
{
    /// <summary>
    /// Simple UI view manager. Handles switching between views.
    /// </summary>
    public class UIViewManager : MonoBehaviour
    {
        [SerializeField] private UIView[] views;
        [SerializeField] private int initialViewIndex = -1;

        private UIView _currentView;
        private Dictionary<Type, UIView> _viewLookup;

        private void Awake()
        {
            _viewLookup = new Dictionary<Type, UIView>();

            foreach (var view in views)
            {
                if (view == null) continue;

                var type = view.GetType();
                if (_viewLookup.ContainsKey(type))
                {
                    Debug.LogWarning($"[UIViewManager] Duplicate view type: {type.Name}");
                    continue;
                }

                _viewLookup[type] = view;
                view.gameObject.SetActive(false);
            }

            if (initialViewIndex >= 0 && initialViewIndex < views.Length && views[initialViewIndex] != null)
            {
                Show(views[initialViewIndex].GetType());
            }
        }

        /// <summary>
        /// Show a view by type. Hides the current view.
        /// </summary>
        /// <typeparam name="T">Type of view to show</typeparam>
        /// <param name="data">Optional data to pass to the view</param>
        public void Show<T>(object data = null) where T : UIView
        {
            Show(typeof(T), data);
        }

        /// <summary>
        /// Show a view by type. Hides the current view.
        /// </summary>
        /// <param name="viewType">Type of view to show</param>
        /// <param name="data">Optional data to pass to the view</param>
        public void Show(Type viewType, object data = null)
        {
            if (!_viewLookup.TryGetValue(viewType, out var view))
            {
                Debug.LogError($"[UIViewManager] View not found: {viewType.Name}");
                return;
            }

            if (_currentView != null)
            {
                _currentView.Hide();
            }

            _currentView = view;
            _currentView.Show(data);
        }

        /// <summary>
        /// Hide the current view without showing another.
        /// </summary>
        public void HideCurrent()
        {
            if (_currentView != null)
            {
                _currentView.Hide();
                _currentView = null;
            }
        }

        /// <summary>
        /// Get the currently active view.
        /// </summary>
        public UIView CurrentView => _currentView;

        /// <summary>
        /// Check if a specific view is currently shown.
        /// </summary>
        public bool IsShowing<T>() where T : UIView
        {
            return _currentView != null && _currentView.GetType() == typeof(T);
        }
    }
}
