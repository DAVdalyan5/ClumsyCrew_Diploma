using UnityEngine;

namespace HeistNSeek.Core
{
    public interface IPlainService
    {
        string GetGreeting();
    }

    public class PlainService : IPlainService
    {
        public string GetGreeting()
        {
            return "Hello from PlainService";
        }
    }
}


