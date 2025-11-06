using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

public static class CoroutineHelper
{
    /// <summary>
    /// Starts a coroutine by creating a temporary GameObject with a CoroutineRunner component.
    /// </summary>
    /// <param name="coroutine">The coroutine to run.</param>
    /// <param name="destroyOnComplete">Whether to destroy the GameObject after coroutine completion.</param>
    public static void RunCoroutine(IEnumerator coroutine, string coroutineRunnerObjectName = "TemporaryCoroutineRunner", bool destroyOnComplete = true)
    {
        coroutineRunnerObjectName += coroutine.ToString();
        GameObject go = new GameObject(coroutineRunnerObjectName);
        CoroutineRunner runner = go.AddComponent<CoroutineRunner>();
        runner.StartCoroutine(RunAndCleanup(runner, coroutine, destroyOnComplete));
    }

    private static IEnumerator RunAndCleanup(CoroutineRunner runner, IEnumerator coroutine, bool destroyOnComplete)
    {
        yield return runner.StartCoroutine(coroutine);

        if (destroyOnComplete)
        {
            Object.Destroy(runner.gameObject);
        }
    }
}

public class CoroutineRunner : MonoBehaviour { }
