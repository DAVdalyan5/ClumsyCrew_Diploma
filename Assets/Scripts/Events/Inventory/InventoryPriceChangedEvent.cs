using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Events.Inventory
{
    public class InventoryPriceChangedEvent
    {
        public int NewPrice { get; private set; }

        public InventoryPriceChangedEvent(int newPrice)
        {
            NewPrice = newPrice;
        }
    }
}
