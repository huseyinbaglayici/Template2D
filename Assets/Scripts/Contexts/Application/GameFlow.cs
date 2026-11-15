using Project.Common.UI;
using Project.Common.UI.PopUps;
using Project.Core.Audio;
using Project.Core.Pooling;
using Project.Core.SceneLoading;
using UnityEngine;

namespace Project.Contexts.Application
{
    /// <summary>
    /// The only place that decides which content scene is shown. Covers the switch with the
    /// loading overlay, closes popups, returns pooled objects and swaps the music.
    /// </summary>
    public sealed class GameFlow
    {
        private readonly SceneLoader _scenes;
        private readonly LoadingOverlay _overlay;
        private readonly PopupService _popups;
        private readonly PoolService _pools;
        private readonly AudioService _audio;

        public GameFlow(SceneLoader scenes, LoadingOverlay overlay, PopupService popups, PoolService pools,
            AudioService audio)
        {
            _scenes = scenes;
            _overlay = overlay;
            _popups = popups;
            _pools = pools;
            _audio = audio;
        }

        public Awaitable GoHomeAsync() => SwitchAsync(SceneNames.Home, "music_home");

        public Awaitable GoToGameAsync() => SwitchAsync(SceneNames.Game, "music_game");

        private async Awaitable SwitchAsync(string sceneName, string musicId)
        {
            if (_scenes.IsBusy || _scenes.CurrentScene == sceneName) return;

            _popups.CloseAll();
            await _overlay.ShowAsync();
            _pools.DespawnAll();
            await _scenes.SwitchToAsync(sceneName, _overlay.SetProgress);
            _audio.PlayMusic(musicId);
            await _overlay.HideAsync();
        }
    }
}
