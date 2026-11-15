using System.Collections.Generic;
using UnityEngine;

namespace Project.Common.UI.PopUps
{
    /// <summary>
    /// Opens popup prefabs under one overlay canvas (in the Boot scene, so popups survive scene switches).
    /// Each prefab is instantiated once and reused. One popup is shown at a time; extra requests wait in a queue.
    /// </summary>
    public sealed class PopupService
    {
        private readonly Transform _root;
        private readonly Dictionary<Popup, Popup> _instances = new();
        private readonly Queue<(Popup Prefab, object Args)> _queue = new();
        private Popup _current;

        public PopupService(Transform root)
        {
            _root = root;
        }

        /// <summary>Shows the popup now, or after the current one closes.</summary>
        public void Show(Popup prefab, object args = null)
        {
            if (prefab == null) return;
            if (_current != null)
            {
                _queue.Enqueue((prefab, args));
                return;
            }
            Open(prefab, args);
        }

        public void CloseAll()
        {
            _queue.Clear();
            if (_current != null) _current.CloseImmediately();
        }

        private async void Open(Popup prefab, object args)
        {
            if (!_instances.TryGetValue(prefab, out var popup) || popup == null)
            {
                popup = Object.Instantiate(prefab, _root, false);
                popup.name = prefab.name;
                popup.Closed += OnClosed;
                _instances[prefab] = popup;
            }

            _current = popup;
            await popup.ShowAsync(args);
        }

        private void OnClosed(Popup popup)
        {
            if (popup != _current) return;
            _current = null;
            if (_queue.Count > 0)
            {
                var (prefab, args) = _queue.Dequeue();
                Open(prefab, args);
            }
        }
    }
}
