using System.Collections.Generic;
using Project.Core.Pooling;
using Project.Core.Services;
using UnityEditor;
using UnityEngine;

namespace Project.Editor
{
    /// <summary>Live view of every pool while playing: in use / available / capacity / grow count.</summary>
    public sealed class PoolMonitorWindow : EditorWindow
    {
        private readonly List<PoolStats> _stats = new();
        private Vector2 _scroll;

        [MenuItem("Tools/Project/Pool Monitor")]
        private static void Open() => GetWindow<PoolMonitorWindow>("Pool Monitor");

        private void OnInspectorUpdate()
        {
            if (EditorApplication.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to see pools.", MessageType.Info);
                return;
            }

            if (!Services.TryGet<PoolService>(out var pools))
            {
                EditorGUILayout.HelpBox("PoolService is not registered (start from the Boot scene).", MessageType.Warning);
                return;
            }

            pools.CollectStats(_stats);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("Pool", EditorStyles.boldLabel, GUILayout.MinWidth(160));
                GUILayout.Label("In use", GUILayout.Width(60));
                GUILayout.Label("Free", GUILayout.Width(60));
                GUILayout.Label("Max", GUILayout.Width(60));
                GUILayout.Label("Grows", GUILayout.Width(60));
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var s in _stats)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(s.Name, GUILayout.MinWidth(160));
                    GUILayout.Label(s.InUse.ToString(), GUILayout.Width(60));
                    GUILayout.Label(s.Available.ToString(), GUILayout.Width(60));
                    GUILayout.Label(s.Max.ToString(), GUILayout.Width(60));
                    var old = GUI.color;
                    if (s.Grows > 0) GUI.color = Color.yellow;
                    GUILayout.Label(s.Grows.ToString(), GUILayout.Width(60));
                    GUI.color = old;
                }
            }
            EditorGUILayout.EndScrollView();

            if (_stats.Count == 0) EditorGUILayout.HelpBox("No pools created yet.", MessageType.None);
            else if (_stats.Exists(s => s.Grows > 0))
                EditorGUILayout.HelpBox("A pool grew past its MaxCount. Raise MaxCount in the PoolConfig.", MessageType.Warning);
        }
    }
}
