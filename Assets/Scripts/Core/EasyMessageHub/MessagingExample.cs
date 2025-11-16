using Easy.MessageHub;
using System;
using UnityEngine;

namespace Assets.Scripts.Infrastructure.EasyMessageHub
{
    internal class MessagingExample
    {
        private class Pub
        {
            private readonly IMessageHub publisher;

            private Pub(IMessageHub publisher)
            {
                this.publisher = publisher;
            }

            public void Publish() 
            {
                this.publisher.Publish("payload");
            }
        }

        private class Sub
        {
            private readonly IMessageHub subscriber;
            private Guid token;

            private Sub(IMessageHub subscriber)
            {
                this.subscriber = subscriber;

                this.token = subscriber.Subscribe<string>(obj => Console.WriteLine(obj));
            }

            public void Unsubscribe()
            {
                this.subscriber.Unsubscribe(this.token);
            }
        }

        private class SubMono //: MonoBehaviour
        {
            private readonly IMessageHub subscriber;

            private SubMono(IMessageHub subscriber)
            {
                this.subscriber = subscriber;

                //this wouldve worked if it was a monoClass
                //subscriber.SubscribeSafe<string>(this, OnMessage);
            }

            private void OnMessage(string obj)
            {
                Console.WriteLine(obj);
            }
        }
    }


}
