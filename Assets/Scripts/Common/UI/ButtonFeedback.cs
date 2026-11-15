using Project.Common.Tween;
using Project.Core.Audio;
using Project.Core.Haptics;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Project.Common.UI
{
    /// <summary>Press scale, click sound and a light haptic for any UI button. Add next to a Button.</summary>
    public sealed class ButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [SerializeField] private string _clickSfx = "ui_click";
        [SerializeField] private HapticStrength _haptic = HapticStrength.Light;

        public void OnPointerDown(PointerEventData eventData) => TweenPresets.Press(transform);

        public void OnPointerUp(PointerEventData eventData) => TweenPresets.Release(transform);

        public void OnPointerClick(PointerEventData eventData)
        {
            if (Core.Services.Services.TryGet<AudioService>(out var audio)) audio.PlaySfx(_clickSfx);
            if (Core.Services.Services.TryGet<HapticService>(out var haptics)) haptics.Play(_haptic);
        }

        private void OnDisable() => transform.localScale = Vector3.one;
    }
}
