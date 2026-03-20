namespace Assets.Scripts.Helpers
{
    public static class GameObjectExtensions
    {
        public static bool HasComponent<T>(this UnityEngine.GameObject gameObject) where T : UnityEngine.Component
        {
            return gameObject.GetComponent<T>() != null;
        }
    }
}
