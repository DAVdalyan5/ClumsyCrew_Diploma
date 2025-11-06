using Assets.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeistNSeek.Core.StateMachine.States
{
    /// <summary>
    /// State responsible for loading scenes asynchronously.
    /// Payload should be an int (scene build index) or string (scene name).
    /// </summary>
    public class LoadingState : BaseState
    {
        public override void Enter(object payload = default)
        {
            Debug.Log($"[LoadingState] Loading scene: {payload}");

            if (payload == null)
            {
                Debug.LogError("[LoadingState] No scene specified in payload!");
                return;
            }

            // Handle scene loading based on payload type
            if (payload is int sceneIndex)
            {
                SceneLoader.Load(sceneIndex, LoadSceneMode.Additive, OnSceneLoaded);
            }
            else if (payload is string sceneName)
            {
                SceneLoader.Load(sceneName, LoadSceneMode.Additive, OnSceneLoaded);
            }
            else
            {
                Debug.LogError($"[LoadingState] Invalid payload type: {payload.GetType()}. Expected int or string.");
            }
        }

        public override void Exit()
        {
            Debug.Log("[LoadingState] Scene loading complete.");
        }

        private void OnSceneLoaded()
        {
            Debug.Log("[LoadingState] Scene loaded successfully. Transitioning to GameplayState.");
            StateMachine.Enter<GameplayState>();
        }
    }
}
