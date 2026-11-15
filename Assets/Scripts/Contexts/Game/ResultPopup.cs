using Project.Common.UI;
using Project.Common.UI.PopUps;
using Project.Contexts.Application;
using Project.Core.Services;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Project.Contexts.Game
{
    public readonly struct ResultArgs
    {
        public readonly bool Won;
        public readonly int Reward;

        public ResultArgs(bool won, int reward)
        {
            Won = won;
            Reward = reward;
        }
    }

    /// <summary>Win/lose popup. "Continue" closes it and returns to Home.</summary>
    public sealed class ResultPopup : Popup
    {
        [SerializeField] private LocalizedText _title;
        [SerializeField] private TMP_Text _reward;
        [SerializeField] private Button _continue;

        protected override void Awake()
        {
            base.Awake();
            _continue.onClick.AddListener(() =>
            {
                Close();
                _ = Services.Get<GameFlow>().GoHomeAsync();
            });
        }

        protected override void OnShow(object args)
        {
            var result = args is ResultArgs r ? r : default;
            _title.Key = result.Won ? "result.win" : "result.lose";
            _reward.text = result.Won ? $"+{result.Reward}" : string.Empty;
        }
    }
}
