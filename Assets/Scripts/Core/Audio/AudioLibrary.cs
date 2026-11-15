using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Audio
{
    /// <summary>
    /// Maps string ids ("ui_click", "music_home") to clips so code never references clips directly.
    /// An entry may exist without a clip yet: it documents a sound the game expects and plays nothing.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Audio Library", fileName = "AudioLibrary")]
    public sealed class AudioLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Id;
            public AudioClip Clip;
            [Range(0f, 1f)] public float Volume;
        }

        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<string, Entry> _lookup;

        public bool TryGet(string id, out Entry entry)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, Entry>(_entries.Count);
                foreach (var e in _entries)
                    if (!string.IsNullOrEmpty(e.Id)) _lookup[e.Id] = e;
            }
            return _lookup.TryGetValue(id, out entry);
        }

        private void OnValidate() => _lookup = null;
    }
}
