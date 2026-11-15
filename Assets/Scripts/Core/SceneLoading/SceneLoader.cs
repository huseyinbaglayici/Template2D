using System;
using Project.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Core.SceneLoading
{
    public readonly struct SceneLoaded : IEvent
    {
        public readonly string SceneName;
        public SceneLoaded(string sceneName) => SceneName = sceneName;
    }

    public readonly struct SceneUnloading : IEvent
    {
        public readonly string SceneName;
        public SceneUnloading(string sceneName) => SceneName = sceneName;
    }

    /// <summary>
    /// Loads content scenes additively on top of the persistent Boot scene.
    /// Only one content scene is active at a time; the previous one is unloaded first.
    /// </summary>
    public sealed class SceneLoader
    {
        private bool _busy;

        public string CurrentScene { get; private set; }
        public bool IsBusy => _busy;

        /// <param name="progress">0..1, for a loading bar.</param>
        public async Awaitable SwitchToAsync(string sceneName, Action<float> progress = null)
        {
            if (_busy)
            {
                Debug.LogWarning($"[SceneLoader] Ignored switch to '{sceneName}': a switch is in progress.");
                return;
            }

            _busy = true;
            try
            {
                if (!string.IsNullOrEmpty(CurrentScene))
                {
                    EventBus<SceneUnloading>.Raise(new SceneUnloading(CurrentScene));
                    await Run(SceneManager.UnloadSceneAsync(CurrentScene), p => progress?.Invoke(p * 0.3f));
                    CurrentScene = null;
                }

                var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (load == null)
                    throw new InvalidOperationException($"Scene '{sceneName}' is not in Build Profiles scene list.");
                await Run(load, p => progress?.Invoke(0.3f + p * 0.7f));

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
                CurrentScene = sceneName;
                progress?.Invoke(1f);
                EventBus<SceneLoaded>.Raise(new SceneLoaded(sceneName));
            }
            finally
            {
                _busy = false;
            }
        }

        private static async Awaitable Run(AsyncOperation op, Action<float> progress)
        {
            if (op == null) return;
            while (!op.isDone)
            {
                progress?.Invoke(Mathf.Clamp01(op.progress / 0.9f));
                await Awaitable.NextFrameAsync();
            }
        }
    }
}
