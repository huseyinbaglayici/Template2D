using PrimeTween;
using UnityEngine;

namespace Project.Common.Tween
{
    /// <summary>
    /// Shared UI motion so every popup and button animates the same way. All presets use
    /// unscaled time, so they keep working while the game is paused (Time.timeScale = 0).
    /// </summary>
    public static class TweenPresets
    {
        public const float PopDuration = 0.25f;
        public const float FadeDuration = 0.2f;

        public static PrimeTween.Tween PopIn(Transform target) =>
            PrimeTween.Tween.Scale(target, Vector3.one * 0.8f, Vector3.one, PopDuration, Ease.OutBack, useUnscaledTime: true);

        public static PrimeTween.Tween PopOut(Transform target) =>
            PrimeTween.Tween.Scale(target, Vector3.one, Vector3.one * 0.8f, PopDuration * 0.7f, Ease.InBack, useUnscaledTime: true);

        public static PrimeTween.Tween FadeIn(CanvasGroup group) =>
            PrimeTween.Tween.Alpha(group, 0f, 1f, FadeDuration, Ease.OutQuad, useUnscaledTime: true);

        public static PrimeTween.Tween FadeOut(CanvasGroup group) =>
            PrimeTween.Tween.Alpha(group, 1f, 0f, FadeDuration, Ease.InQuad, useUnscaledTime: true);

        public static PrimeTween.Tween Press(Transform target) =>
            PrimeTween.Tween.Scale(target, Vector3.one * 0.92f, 0.08f, Ease.OutQuad, useUnscaledTime: true);

        public static PrimeTween.Tween Release(Transform target) =>
            PrimeTween.Tween.Scale(target, Vector3.one, 0.15f, Ease.OutBack, useUnscaledTime: true);

        public static PrimeTween.Tween Punch(Transform target, float strength = 0.15f) =>
            PrimeTween.Tween.PunchScale(target, Vector3.one * strength, 0.3f, useUnscaledTime: true);
    }
}
