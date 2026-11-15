using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Core.Debugging
{
    /// <summary>
    /// Lightweight in-game debug overlay (development builds and Editor only).
    /// Shows FPS and frame time; open the panel with F1 or a three-finger tap.
    /// Features add their own buttons: DebugPanel.AddAction("Win level", () => ...).
    /// </summary>
    public sealed class DebugPanel : MonoBehaviour
    {
        private static readonly List<(string Label, Action Action)> Actions = new();

        [SerializeField] private bool _showFps = true;

        private bool _open;
        private float _fpsTimer;
        private int _frames;
        private float _fps;
        private float _worstFrameMs;
        private float _windowWorstMs;
        private GUIStyle _label;
        private Vector2 _scroll;

        public static void AddAction(string label, Action action)
        {
            RemoveAction(label);
            Actions.Add((label, action));
        }

        public static void RemoveAction(string label) => Actions.RemoveAll(a => a.Label == label);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Actions.Clear();

        private void Awake()
        {
#if !(DEVELOPMENT_BUILD || UNITY_EDITOR)
            enabled = false;
#endif
        }

        private void Update()
        {
            _frames++;
            _fpsTimer += Time.unscaledDeltaTime;
            _windowWorstMs = Mathf.Max(_windowWorstMs, Time.unscaledDeltaTime * 1000f);
            if (_fpsTimer >= 0.5f)
            {
                _fps = _frames / _fpsTimer;
                _worstFrameMs = _windowWorstMs;
                _frames = 0;
                _fpsTimer = 0f;
                _windowWorstMs = 0f;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.f1Key.wasPressedThisFrame) _open = !_open;

            var touch = Touchscreen.current;
            if (touch != null && touch.touches.Count > 2 && touch.touches[2].press.wasPressedThisFrame) _open = !_open;
        }

        private void OnGUI()
        {
            var scale = Mathf.Max(1f, Screen.dpi / 160f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            _label ??= new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };

            if (_showFps)
            {
                _label.normal.textColor = _fps >= 55 ? Color.green : _fps >= 30 ? Color.yellow : Color.red;
                GUI.Label(new Rect(8, 8, 260, 22), $"{_fps:0} FPS  (worst {_worstFrameMs:0.0} ms)", _label);
            }

            if (!_open) return;

            var width = Screen.width / scale - 16f;
            var height = Screen.height / scale * 0.6f;
            GUILayout.BeginArea(new Rect(8, 36, width, height), GUI.skin.box);
            GUILayout.Label($"Debug  |  {Application.version}  |  {SystemInfo.deviceModel}");
            _showFps = GUILayout.Toggle(_showFps, "Show FPS");
            _scroll = GUILayout.BeginScrollView(_scroll);
            foreach (var (label, action) in Actions)
            {
                if (!GUILayout.Button(label, GUILayout.Height(40))) continue;
                try { action(); }
                catch (Exception e) { Debug.LogException(e); }
            }
            GUILayout.EndScrollView();
            if (GUILayout.Button("Close", GUILayout.Height(40))) _open = false;
            GUILayout.EndArea();
        }
    }
}
