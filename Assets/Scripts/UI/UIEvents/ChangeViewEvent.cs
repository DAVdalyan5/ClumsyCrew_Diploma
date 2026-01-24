using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.UI.UIEvents
{
    public class ChangeViewEvent
    {
        public Type ViewType { get; private set; }
        public object Data { get; private set; }

        public ChangeViewEvent(Type viewType, object data = null)
        {
            ViewType = viewType;
            Data = data;
        }
    }
}
