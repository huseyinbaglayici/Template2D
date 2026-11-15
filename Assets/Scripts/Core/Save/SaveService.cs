using System;
using System.IO;
using UnityEngine;

namespace Project.Core.Save
{
    /// <summary>
    /// One JSON file per model under persistentDataPath. Writes go to a temp file first and the
    /// previous file is kept as .bak, so a crash during save never leaves a half-written file.
    /// Loading falls back to the backup when the main file is missing or corrupt.
    /// </summary>
    public sealed class SaveService
    {
        private readonly string _folder;

        public SaveService(string folder = null)
        {
            _folder = folder ?? Path.Combine(Application.persistentDataPath, "save");
            Directory.CreateDirectory(_folder);
        }

        public T Load<T>(string key) where T : new()
        {
            var path = PathFor(key);
            if (TryRead(path, out T model)) return model;
            if (TryRead(path + ".bak", out model))
            {
                Debug.LogWarning($"[Save] '{key}' restored from backup.");
                return model;
            }
            return new T();
        }

        public void Save<T>(string key, T model)
        {
            var path = PathFor(key);
            var temp = path + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonUtility.ToJson(model));
                if (File.Exists(path)) File.Copy(path, path + ".bak", true);
                File.Copy(temp, path, true);
                File.Delete(temp);
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] Could not save '{key}': {e.Message}");
            }
        }

        public void Delete(string key)
        {
            var path = PathFor(key);
            foreach (var file in new[] { path, path + ".bak", path + ".tmp" })
                if (File.Exists(file)) File.Delete(file);
        }

        private string PathFor(string key) => Path.Combine(_folder, key + ".json");

        private static bool TryRead<T>(string path, out T model)
        {
            model = default;
            if (!File.Exists(path)) return false;
            try
            {
                var json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json)) return false;
                model = JsonUtility.FromJson<T>(json);
                return model != null;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Corrupt file '{Path.GetFileName(path)}': {e.Message}");
                return false;
            }
        }
    }
}
