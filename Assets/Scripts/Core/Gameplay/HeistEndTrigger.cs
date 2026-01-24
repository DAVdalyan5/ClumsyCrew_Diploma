using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Assets.Scripts.UI.UIEvents;
using Assets.Scripts.UI.Views;
using Easy.MessageHub;
using Unity.VisualScripting;
using UnityEditor.UIElements;
using UnityEngine;
using VContainer;

public class HeistEndTrigger : MonoBehaviour
{
    // throw on server about heist end
    //when all players use this shi

    [SerializeField] string playerTag = "Player";

    private IMessageHub _messageHub;
    bool isInRadius = false;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub = messageHub;
    }

    void Start()
    {
        _messageHub.SubscribeSafe<InteractEvent>(this, m => HeistEndTriggered(m));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        this.isInRadius = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        this.isInRadius = false;
    }

    private void HeistEndTriggered(InteractEvent message)
    {
        if (!isInRadius)
        {
            return;
        }

        Debug.Log("Heist End Triggered");
        this._messageHub.Publish(new ChangeViewEvent(typeof(HeistEndView), null));
        //send to server to verify all this and end heist.
    }
}
