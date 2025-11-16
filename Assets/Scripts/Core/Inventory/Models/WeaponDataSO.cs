using UnityEngine;

namespace Assets.Scripts.Core.Inventory.Models
{
    [CreateAssetMenu(fileName = "New Weapon", menuName = "Robbery/Weapon")]
    public class WeaponDataSO : ItemDataSO
    {
        [Header("Weapon Stats")]
        public float damage;
        public float fireRate;

        //public int ammoCapacity;
        //public AudioClip fireSound;
        //etc...
    }
}
