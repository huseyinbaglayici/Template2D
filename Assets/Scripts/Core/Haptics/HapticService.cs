using UnityEngine;

namespace Project.Core.Haptics
{
    public enum HapticStrength
    {
        Light,
        Medium,
        Heavy
    }

    /// <summary>
    /// Short vibrations for UI and game feedback. Android 8+ uses VibrationEffect with an amplitude,
    /// older devices and iOS fall back to Handheld.Vibrate (Heavy only, to avoid long buzzes).
    /// </summary>
    public sealed class HapticService
    {
        public bool Enabled { get; set; }

#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly AndroidJavaObject _vibrator;
        private readonly AndroidJavaClass _effectClass;
        private readonly bool _supportsEffects;
#endif

        public HapticService(bool enabled)
        {
            Enabled = enabled;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                using var version = new AndroidJavaClass("android.os.Build$VERSION");
                _supportsEffects = version.GetStatic<int>("SDK_INT") >= 26;
                if (_supportsEffects) _effectClass = new AndroidJavaClass("android.os.VibrationEffect");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Haptics] Vibrator unavailable: {e.Message}");
            }
#endif
        }

        public void Play(HapticStrength strength)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_vibrator == null) return;
            var (ms, amplitude) = strength switch
            {
                HapticStrength.Light => (15L, 60),
                HapticStrength.Medium => (25L, 140),
                _ => (40L, 255)
            };
            if (_supportsEffects)
            {
                using var effect = _effectClass.CallStatic<AndroidJavaObject>("createOneShot", ms, amplitude);
                _vibrator.Call("vibrate", effect);
            }
            else
            {
                _vibrator.Call("vibrate", ms);
            }
#elif UNITY_IOS && !UNITY_EDITOR
            if (strength == HapticStrength.Heavy) Handheld.Vibrate();
#endif
        }

#if UNITY_ANDROID
        // Never called. Unity adds the VIBRATE permission to the Android manifest only when
        // it finds a Handheld.Vibrate reference in the compiled code.
        private static void KeepVibratePermission() => Handheld.Vibrate();
#endif
    }
}
