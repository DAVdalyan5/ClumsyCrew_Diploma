using Assets.Scripts.Events.Inventory;
using Easy.MessageHub;
using HeistNSeek.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VContainer;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using TMPro;
using UnityEngine;

namespace Assets.Scripts.UI.Views
{
    public class HeistEndView : UIView
    {
        private IMessageHub _messageHub;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        void Start()
        {
        }
    }
}
