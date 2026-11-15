using UnityEngine;

namespace Project.Core.Audio
{
    /// <summary>
    /// One music source plus a small round-robin set of SFX sources (no AudioSource is created at runtime).
    /// Mute flags are persisted through the SettingsService by the caller.
    /// </summary>
    public sealed class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioLibrary _library;
        [SerializeField, Range(1, 16)] private int _sfxSourceCount = 8;

        private readonly System.Collections.Generic.HashSet<string> _warnedIds = new();
        private AudioSource _music;
        private AudioSource[] _sfx;
        private int _nextSfx;
        private string _currentMusicId;

        public bool MusicMuted { get; private set; }
        public bool SfxMuted { get; private set; }

        public void Initialize(AudioLibrary library, bool musicMuted, bool sfxMuted)
        {
            if (library != null) _library = library;
            _music = CreateSource("Music", loop: true);
            _sfx = new AudioSource[_sfxSourceCount];
            for (var i = 0; i < _sfx.Length; i++) _sfx[i] = CreateSource($"Sfx {i}", loop: false);
            SetMusicMuted(musicMuted);
            SetSfxMuted(sfxMuted);
        }

        public void PlaySfx(string id, float pitch = 1f)
        {
            if (SfxMuted || _sfx == null || !TryClip(id, out var entry)) return;
            var source = _sfx[_nextSfx];
            _nextSfx = (_nextSfx + 1) % _sfx.Length;
            source.pitch = pitch;
            source.PlayOneShot(entry.Clip, entry.Volume <= 0f ? 1f : entry.Volume);
        }

        public void PlayMusic(string id)
        {
            if (_music == null || id == _currentMusicId) return;
            _currentMusicId = id;
            if (!TryClip(id, out var entry))
            {
                _music.Stop();
                return;
            }
            _music.clip = entry.Clip;
            _music.volume = entry.Volume <= 0f ? 1f : entry.Volume;
            _music.Play();
        }

        public void StopMusic()
        {
            _currentMusicId = null;
            if (_music != null) _music.Stop();
        }

        public void SetMusicMuted(bool muted)
        {
            MusicMuted = muted;
            if (_music != null) _music.mute = muted;
        }

        public void SetSfxMuted(bool muted) => SfxMuted = muted;

        private bool TryClip(string id, out AudioLibrary.Entry entry)
        {
            entry = default;
            if (_library != null && _library.TryGet(id, out entry)) return entry.Clip != null;
            if (_warnedIds.Add(id)) Debug.LogWarning($"[Audio] Id '{id}' is not defined in the AudioLibrary.");
            return false;
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var go = new GameObject(sourceName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            return source;
        }
    }
}
