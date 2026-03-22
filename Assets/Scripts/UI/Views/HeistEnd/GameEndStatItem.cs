using UnityEngine;

namespace Assets.Scripts.UI.Views
{
    public class GameEndStatItem : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        [SerializeField] private TMPro.TextMeshProUGUI valueText;

        public void SetData(string name, int value)
        {
            this.nameText.text = name;
            this.valueText.text = $"${value}";
        }
    }
}
