using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace HeistNSeek.UI
{
    /// <summary>
    /// Flashes a full-screen vignette Image when the local player takes damage.
    ///
    /// Setup:
    ///  1. Add a Canvas (Screen Space - Overlay, highest sort order) to the scene.
    ///  2. Add a child Image that fills the entire canvas.
    ///     - Assign a soft vignette sprite (dark, transparent centre, opaque edges), OR
    ///       leave the sprite empty and set Image Color to a dark red for a quick placeholder.
    ///  3. Attach this component to that Image GameObject (or any child of the Canvas).
    ///  4. Drag the Image into the VignetteImage field.
    ///  5. Place the GameObject under the GameplayLifetimeScope hierarchy for DI injection.
    /// </summary>
    public class DamageVignetteUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image vignetteImage;

        [Header("Flash Settings")]
        [Tooltip("Alpha the vignette snaps to on damage.")]
        [SerializeField] [Range(0f, 1f)] private float peakAlpha = 0.75f;

        [Tooltip("Seconds for the vignette to fade back to zero.")]
        [SerializeField] private float fadeDuration = 0.5f;

        private IMessageHub _messageHub;
        private float _currentAlpha;
        private bool _fading;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Start()
        {
            if (vignetteImage == null)
            {
                Debug.LogWarning("[DamageVignette] VignetteImage is not assigned.", this);
                enabled = false;
                return;
            }

            SetAlpha(0f);

            _messageHub?.SubscribeSafe<DamageTakenEvent>(this, OnDamageTaken);
        }

        private void Update()
        {
            if (!_fading)
                return;

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, Time.deltaTime / fadeDuration);
            SetAlpha(_currentAlpha);

            if (_currentAlpha <= 0f)
                _fading = false;
        }

        private void OnDamageTaken(DamageTakenEvent evt)
        {
            _currentAlpha = peakAlpha;
            SetAlpha(_currentAlpha);
            _fading = true;
        }

        private void SetAlpha(float alpha)
        {
            var col = vignetteImage.color;
            col.a = alpha;
            vignetteImage.color = col;
        }
    }
}
