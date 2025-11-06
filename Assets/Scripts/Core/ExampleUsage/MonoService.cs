using UnityEngine;

namespace HeistNSeek.Core
{
    public class MonoService : MonoBehaviour
    {
        [SerializeField] private string message = "Hello from MonoService";

        public string GetMessage()
        {
            return message;
        }
    }
}


