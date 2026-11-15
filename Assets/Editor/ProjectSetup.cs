using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>
    /// Applies the 2D mobile defaults (Gamma, portrait, IL2CPP, ARM64,
    /// sorting layers) and the git-friendly editor settings. Safe to run more than once.
    /// </summary>
    public static class ProjectSetup
    {
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";

        [MenuItem("Tools/Project Setup/Apply 2D Mobile Defaults")]
        public static void Apply()
        {
            // Rendering and screen
            PlayerSettings.colorSpace = ColorSpace.Gamma;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Android build
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // Git-friendly project serialization
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";

            // Sorting layers: Background, Default, Gameplay, FX, HUD, UI, Foreground
            EnsureSortingLayer("Background", 0);
            EnsureSortingLayer("Gameplay", int.MaxValue);
            EnsureSortingLayer("FX", int.MaxValue);
            EnsureSortingLayer("HUD", int.MaxValue);
            EnsureSortingLayer("UI", int.MaxValue);
            EnsureSortingLayer("Foreground", int.MaxValue);

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] 2D mobile defaults applied: Gamma, Portrait, IL2CPP, ARM64, Force Text, Visible Meta Files, sorting layers.");
        }

        private static void EnsureSortingLayer(string layerName, int index)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(TagManagerPath);
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError($"[ProjectSetup] {TagManagerPath} not found.");
                return;
            }

            var tagManager = new SerializedObject(assets[0]);
            var layers = tagManager.FindProperty("m_SortingLayers");

            for (var i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                    return;
            }

            var at = Math.Min(index, layers.arraySize);
            layers.InsertArrayElementAtIndex(at);
            var element = layers.GetArrayElementAtIndex(at);
            element.FindPropertyRelative("name").stringValue = layerName;
            element.FindPropertyRelative("uniqueID").longValue = NewUniqueId(layers);
            element.FindPropertyRelative("locked").boolValue = false;
            tagManager.ApplyModifiedProperties();
        }

        private static long NewUniqueId(SerializedProperty layers)
        {
            while (true)
            {
                var candidate = (long)(uint)Guid.NewGuid().GetHashCode();
                if (candidate == 0) continue;

                var taken = false;
                for (var i = 0; i < layers.arraySize; i++)
                {
                    if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").longValue == candidate)
                    {
                        taken = true;
                        break;
                    }
                }

                if (!taken) return candidate;
            }
        }
    }
}
