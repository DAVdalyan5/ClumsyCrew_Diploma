using Assets.Scripts.Infrastructure.EasyMessageHub;
using Assets.Scripts.UI.UIEvents;
using Easy.MessageHub;
using HeistNSeek.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace Assets.Scripts.UI.Base
{
    public class ViewSelector : MonoBehaviour
    {
        private UIViewManager _manager;
        private IMessageHub _messageHub;

        public void Awake()
        {
            _manager = GetComponentInParent<UIViewManager>();
        }

        [Inject]
        public void Inject(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        public void Start()
        {
            _messageHub.SubscribeSafe<ChangeViewEvent>(this, m => ChangeCurrentView(m));
        }

        private void ChangeCurrentView(ChangeViewEvent m)
        {
            _manager.Show(m.ViewType, m.Data);
        }
    }
}
