using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Localization
{
    /// <summary>
    /// Key → text per language. One asset per language keeps diffs small and lets translators
    /// work on separate files. <see cref="LanguageCode"/> uses ISO codes ("en", "tr").
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Localization Table", fileName = "Localization_en")]
    public sealed class LocalizationTable : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Key;
            [TextArea] public string Value;
        }

        [SerializeField] private string _languageCode = "en";
        [SerializeField] private List<Entry> _entries = new();

        public string LanguageCode => _languageCode;

        public Dictionary<string, string> BuildLookup()
        {
            var lookup = new Dictionary<string, string>(_entries.Count);
            foreach (var entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Key)) continue;
                if (!lookup.TryAdd(entry.Key, entry.Value))
                    Debug.LogWarning($"[Localization] Duplicate key '{entry.Key}' in {name}.");
            }
            return lookup;
        }
    }
}
