using System;
using UnityEngine;
using R3;
using R3.Triggers;
using Easy.MessageHub;
using Assets.Scripts.Infrastructure;

namespace Assets.Scripts.Infrastructure.EasyMessageHub
{
    public static class MessageHubExtensions
    {
        public static void UnsubscribeWithOnDisable(this IMessageHub hub, MonoBehaviour toDisposeWith, Guid subscriberToken)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            if (subscriberToken == null || subscriberToken == Guid.Empty)
                throw new ArgumentNullException(nameof(subscriberToken));

            if (toDisposeWith == null)
                throw new ArgumentNullException(nameof(toDisposeWith));

            toDisposeWith.OnDisableAsObservable().Subscribe(_ =>
            {
                if (hub.IsSubscribed(subscriberToken))
                {
                    hub.Unsubscribe(subscriberToken);
                    Debug.Log($"Unsubscribed {subscriberToken} from {toDisposeWith.name} on Disable.");
                }
            });
        }

        public static void UnsubscribeWithOnDestroy(this IMessageHub hub, MonoBehaviour toDisposeWith, Guid subscriberToken)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            if (subscriberToken == null || subscriberToken == Guid.Empty)
                throw new ArgumentNullException(nameof(subscriberToken));

            if (toDisposeWith == null)
                throw new ArgumentNullException(nameof(toDisposeWith));

            toDisposeWith.OnDestroyAsObservable().Subscribe(_ =>
            {
                if (hub.IsSubscribed(subscriberToken))
                {
                    hub.Unsubscribe(subscriberToken);
                    Debug.Log($"Unsubscribed {subscriberToken} from {toDisposeWith.name} on destroy.");
                }
            });
        }

        public static void SubscribeSafe<T>(this IMessageHub hub, MonoBehaviour toDisposeWith, Action<T> action, DisposeMethod unsubscribeOnDestroy = DisposeMethod.OnDestroy)
        {
            if (hub == null)
                throw new ArgumentNullException(nameof(hub));

            if (toDisposeWith == null)
                throw new ArgumentNullException(nameof(toDisposeWith));

            if (action == null)
                return;

            var subscriberToken = hub.Subscribe(action);

            if (unsubscribeOnDestroy == DisposeMethod.OnDestroy)
            {
                hub.UnsubscribeWithOnDestroy(toDisposeWith, subscriberToken);
                return;
            }

            hub.UnsubscribeWithOnDisable(toDisposeWith, subscriberToken);
        }

        //in future get this working with non-mono classes
    }
}