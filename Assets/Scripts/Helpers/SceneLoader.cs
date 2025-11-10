using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Assets.Scripts.Core
{
    //TODO: add with name if needed
    public static class SceneLoader
    {
        public static void Load(int index, LoadSceneMode loadSceneMode, Action onLoaded = null) =>
            CoroutineHelper.RunCoroutine(LoadScene(index, loadSceneMode, onLoaded), "SceneLoaderCoroutine");

        public static void Load(string name, LoadSceneMode loadSceneMode, Action onLoaded = null)
        {
            var scene = SceneManager.GetSceneByName(name);
            CoroutineHelper.RunCoroutine(LoadScene(scene.buildIndex, loadSceneMode, onLoaded), "SceneLoaderCoroutine");
        }

        private static IEnumerator LoadScene(int index, LoadSceneMode loadSceneMode, Action onLoaded = null)
        {
            if (SceneManager.GetActiveScene().buildIndex == index)
            {
                onLoaded?.Invoke();
                yield break;
            }

            AsyncOperation waitNextScene = SceneManager.LoadSceneAsync(index, loadSceneMode);

            yield return new WaitUntil(() => waitNextScene.isDone);

            Scene loadedScene = SceneManager.GetSceneByBuildIndex(index);
            if (loadedScene.IsValid())
            {
                SceneManager.SetActiveScene(loadedScene);
            }
            onLoaded?.Invoke();
        }
    }
}
