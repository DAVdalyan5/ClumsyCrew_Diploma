using UnityEngine;

namespace HeistNSeek.Core
{
    public interface IMonoGlobalService
    {
        string GetInfo();
    }

    public class MonoGlobalService : MonoBehaviour, IMonoGlobalService
    {
        [SerializeField] private string info = "Hello from MonoGlobalService (Bootstrap scope)";

        public string GetInfo()
        {
            return info;
        }
    }
}


