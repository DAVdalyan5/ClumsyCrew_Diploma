using Assets.Scripts.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace HeistNSeek.Core
{
    /// <summary>
    /// EntryPoint for Bootstrap scene initialization.
    /// Runs after all dependencies are resolved in BootstrapLifetimeScope.
    /// </summary>
    public class BootstrapEntryPoint : IStartable
    {
        private readonly int _gameplaySceneIndex;

        public BootstrapEntryPoint(int gameplaySceneIndex)
        {
            _gameplaySceneIndex = gameplaySceneIndex;
        }

        public void Start()
        {
            Debug.Log("[BootstrapEntryPoint] Bootstrap scene initialized.");

            // Load the gameplay scene
            if (_gameplaySceneIndex >= 0 && _gameplaySceneIndex < SceneManager.sceneCountInBuildSettings)
            {
                Debug.Log($"[BootstrapEntryPoint] Loading gameplay scene at index {_gameplaySceneIndex}");
                SceneLoader.Load(_gameplaySceneIndex, LoadSceneMode.Additive);
            }
            else
            {
                Debug.LogError($"[BootstrapEntryPoint] Invalid gameplay scene index ({_gameplaySceneIndex}). Check Build Settings.");
            }
        }
    }
}
