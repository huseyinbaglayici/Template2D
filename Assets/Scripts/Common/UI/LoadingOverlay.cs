using Project.Common.Tween;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Common.UI
{
    /// <summary>Full-screen cover shown during scene switches; blocks input while visible.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class LoadingOverlay : MonoBehaviour
    {
        [SerializeField] private Image _progressFill;

        private CanvasGroup _group;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public async Awaitable ShowAsync()
        {
            gameObject.SetActive(true);
            SetProgress(0f);
            _group.blocksRaycasts = true;
            await TweenPresets.FadeIn(_group);
        }

        public async Awaitable HideAsync()
        {
            await TweenPresets.FadeOut(_group);
            _group.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void SetProgress(float value)
        {
            if (_progressFill != null) _progressFill.fillAmount = Mathf.Clamp01(value);
        }
    }
}
