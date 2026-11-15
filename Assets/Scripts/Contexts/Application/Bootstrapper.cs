using System.Collections.Generic;
using Project.Common.UI;
using Project.Common.UI.PopUps;
using Project.Core.Audio;
using Project.Core.Debugging;
using Project.Core.Haptics;
using Project.Core.Localization;
using Project.Core.Pooling;
using Project.Core.Save;
using Project.Core.SceneLoading;
using Project.Core.Services;
using Project.Core.Settings;
using Project.Core.Updater;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.Contexts.Application
{
    /// <summary>
    /// Composition root in the Boot scene (build index 0). Creates every service in dependency
    /// order, registers it, then opens the first content scene. The Boot scene is never unloaded,
    /// so everything here (camera, EventSystem, popup canvas, audio) lives for the whole session.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class Bootstrapper : MonoBehaviour
    {
        public const string EditorStartSceneKey = "Project.EditorStartScene";

        [Header("Scene objects")]
        [SerializeField] private UpdateDispatcher _updater;
        [SerializeField] private AudioService _audio;
        [SerializeField] private LoadingOverlay _loadingOverlay;
        [SerializeField] private Transform _popupRoot;
        [SerializeField] private Transform _poolRoot;

        [Header("Data")]
        [SerializeField] private AudioLibrary _audioLibrary;
        [SerializeField] private List<LocalizationTable> _localizationTables = new();
        [SerializeField] private int _targetFrameRate = 60;

        private void Awake()
        {
            UnityEngine.Application.targetFrameRate = _targetFrameRate;

            var save = new SaveService();
            var settings = new SettingsService(save);
            _audio.Initialize(_audioLibrary, settings.Model.MusicMuted, settings.Model.SfxMuted);

            Services.Register(save);
            Services.Register(settings);
            Services.Register(_updater);
            Services.Register(_audio);
            Services.Register(new HapticService(settings.Model.HapticsEnabled));
            Services.Register(new LocalizationService(_localizationTables, settings.Model.Language));
            Services.Register(new PoolService(_poolRoot));
            Services.Register(new PopupService(_popupRoot));
            Services.Register(new SceneLoader());
            Services.Register(new ProgressService(save));
            Services.Register(new GameFlow(
                Services.Get<SceneLoader>(), _loadingOverlay, Services.Get<PopupService>(),
                Services.Get<PoolService>(), _audio));

            RegisterDebugActions();
        }

        private async void Start()
        {
            var startScene = SceneNames.Home;
#if UNITY_EDITOR
            // Set by the "Always start from Boot" editor tool: reopen the scene you pressed Play in.
            var requested = UnityEditor.SessionState.GetString(EditorStartSceneKey, string.Empty);
            UnityEditor.SessionState.EraseString(EditorStartSceneKey);
            if (SceneNames.IsContent(requested)) startScene = requested;
#endif
            var flow = Services.Get<GameFlow>();
            if (startScene == SceneNames.Game) await flow.GoToGameAsync();
            else await flow.GoHomeAsync();
        }

        private void OnDestroy() => Services.Clear();

        private static void RegisterDebugActions()
        {
            DebugPanel.AddAction("Go Home", () => _ = Services.Get<GameFlow>().GoHomeAsync());
            DebugPanel.AddAction("Add 100 coins", () => Services.Get<ProgressService>().AddCoins(100));
            DebugPanel.AddAction("Reset progress", () => Services.Get<ProgressService>().ResetProgress());
            DebugPanel.AddAction("Toggle language", () =>
            {
                var loc = Services.Get<LocalizationService>();
                var next = loc.LanguageCode == "en" ? "tr" : "en";
                loc.SetLanguage(next);
                var settings = Services.Get<SettingsService>();
                settings.Model.Language = next;
                settings.Save();
            });
            DebugPanel.AddAction("Log loaded scenes", () =>
            {
                for (var i = 0; i < SceneManager.sceneCount; i++) Debug.Log($"[Scenes] {SceneManager.GetSceneAt(i).name}");
            });
        }
    }
}
