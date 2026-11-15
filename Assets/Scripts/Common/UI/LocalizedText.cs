using Project.Core.Events;
using Project.Core.Localization;
using TMPro;
using UnityEngine;

namespace Project.Common.UI
{
    /// <summary>Sets a TMP text from a localization key and refreshes when the language changes.</summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string _key;

        private TMP_Text _text;

        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                Refresh();
            }
        }

        private void Awake() => _text = GetComponent<TMP_Text>();

        private void OnEnable()
        {
            EventBus<LanguageChanged>.Subscribe(OnLanguageChanged);
            Refresh();
        }

        private void OnDisable() => EventBus<LanguageChanged>.Unsubscribe(OnLanguageChanged);

        private void OnLanguageChanged(LanguageChanged _) => Refresh();

        private void Refresh()
        {
            if (_text == null || string.IsNullOrEmpty(_key)) return;
            if (Core.Services.Services.TryGet<LocalizationService>(out var localization))
                _text.text = localization.Get(_key);
        }
    }
}
