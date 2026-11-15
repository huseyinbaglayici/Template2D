using System;
using Project.Core.Save;

namespace Project.Core.Settings
{
    [Serializable]
    public sealed class SettingsModel
    {
        public int Version = 1;
        public bool MusicMuted;
        public bool SfxMuted;
        public bool HapticsEnabled = true;
        public string Language = "";
    }

    /// <summary>Player preferences, stored through <see cref="SaveService"/> as one model.</summary>
    public sealed class SettingsService
    {
        private const string Key = "settings";
        private readonly SaveService _save;

        public SettingsModel Model { get; }

        public SettingsService(SaveService save)
        {
            _save = save;
            Model = save.Load<SettingsModel>(Key);
        }

        public void Save() => _save.Save(Key, Model);
    }
}
