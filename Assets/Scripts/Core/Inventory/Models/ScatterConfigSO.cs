using UnityEngine;

namespace Assets.Scripts.Core.Inventory.Models
{
    [CreateAssetMenu(fileName = "ScatterConfigSO", menuName = "Configs")]
    public class ScatterConfigSO : ScriptableObject
    {
        public float ItemScatterRadius;
        public float ItemScatterForce;
    }
}
