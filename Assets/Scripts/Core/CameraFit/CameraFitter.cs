using UnityEngine;

namespace Project.Core.CameraFit
{
    /// <summary>
    /// Portrait 2D camera: keeps the design width fully visible on every aspect ratio
    /// (tall phones get more vertical space, tablets keep the design height as a minimum).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFitter : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _designWidth = 10.8f;   // world units (1080 px at 100 PPU)
        [SerializeField, Min(0.1f)] private float _designHeight = 19.2f;  // world units (1920 px at 100 PPU)

        private Camera _camera;
        private int _lastWidth;
        private int _lastHeight;

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            Apply();
        }

        private void Update()
        {
            if (Screen.width != _lastWidth || Screen.height != _lastHeight) Apply();
        }

        private void Apply()
        {
            _lastWidth = Screen.width;
            _lastHeight = Screen.height;
            if (_lastHeight == 0) return;

            var aspect = (float)_lastWidth / _lastHeight;
            var sizeForWidth = _designWidth / aspect * 0.5f;
            var sizeForHeight = _designHeight * 0.5f;
            _camera.orthographicSize = Mathf.Max(sizeForWidth, sizeForHeight);
        }
    }
}
