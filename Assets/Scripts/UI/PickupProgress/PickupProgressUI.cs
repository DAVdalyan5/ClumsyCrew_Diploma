using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using DG.Tweening;
using Easy.MessageHub;
using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.UI.PickupProgress
{
    /// <summary>
    /// Circular progress bar shown while the player holds E to pick up an item.
    /// Attach to the PickupProgressRing GameObject in the HUD Canvas.
    /// Set the Image component: Type=Filled, Fill Method=Radial360, Fill Origin=Top.
    /// Disable the GameObject by default — this script shows/hides it.
    /// </summary>
    public class PickupProgressUI : MonoBehaviour
    {
        [SerializeField] private Image fillImage;

        private IMessageHub _messageHub;
        private Tweener _fillTween;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Start()
        {
            EnsureMessageHubResolved();
            if (_messageHub != null)
            {
                _messageHub.SubscribeSafe<InteractHoldStartEvent>(this, OnHoldStart);
                _messageHub.SubscribeSafe<InteractHoldCanceledEvent>(this, _ => OnHoldCanceled());
            }
        }

        private void EnsureMessageHubResolved()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<global::BootstrapLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();
            if (scope != null)
                _messageHub = scope.Container.Resolve<IMessageHub>();
        }

        private void OnHoldStart(InteractHoldStartEvent evt)
        {
            gameObject.SetActive(true);
            fillImage.fillAmount = 0f;

            _fillTween?.Kill();
            _fillTween = fillImage
                .DOFillAmount(1f, evt.Duration)
                .SetEase(Ease.Linear)
                .OnComplete(() => gameObject.SetActive(false));
        }

        private void OnHoldCanceled()
        {
            _fillTween?.Kill();
            fillImage.fillAmount = 0f;
            gameObject.SetActive(false);
        }
    }
}
