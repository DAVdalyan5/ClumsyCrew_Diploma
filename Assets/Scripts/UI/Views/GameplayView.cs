using Assets.Scripts.Events.Inventory;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using HeistNSeek.UI;
using TMPro;
using UnityEngine;
using VContainer;

public class GameplayView : UIView
{
    [SerializeField] TMP_Text textBox;
    private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub = messageHub;
        Debug.Log("---AAAA");
    }

    void Start()
    {
        Debug.Log("---BBB");
        _messageHub.SubscribeSafe<InventoryPriceChangedEvent>(this, message =>
        {
            textBox.text = message.NewPrice.ToString();
            Debug.Log($"Inventory Price Updated: {message.NewPrice}");
        });
    }
}
