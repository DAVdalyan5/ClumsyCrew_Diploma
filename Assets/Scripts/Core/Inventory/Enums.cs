using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Core.Inventory
{
    public enum ItemType 
    { 
        Loot, 
        Tool,
        Weapon, 
        Consumable, 
        KeyItem 
    }

    public enum Rarity 
    { 
        Common, 
        Uncommon, 
        Rare, 
        Legendary 
    }
}
