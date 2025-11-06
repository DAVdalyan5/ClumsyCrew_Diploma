namespace HeistNSeek.Core
{
    public interface IGlobalTestService
    {
        string GetMessage();
    }

    public class GlobalTestService : IGlobalTestService
    {
        public string GetMessage()
        {
            return "Hello from GlobalTestService (Bootstrap scope)";
        }
    }
}


