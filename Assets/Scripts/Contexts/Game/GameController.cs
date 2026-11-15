using Project.Common.Tween;
using Project.Common.UI.PopUps;
using Project.Contexts.Application;
using Project.Core.Audio;
using Project.Core.Debugging;
using Project.Core.Events;
using Project.Core.Haptics;
using Project.Core.Localization;
using Project.Core.Pooling;
using Project.Core.Services;
using Project.Core.StateMachine;
using TMPro;
using UnityEngine;

namespace Project.Contexts.Game
{
    /// <summary>
    /// Sample level: tap <see cref="_targetCount"/> pooled collectibles before the timer runs out.
    /// Shows the template systems working together: state machine, pool, event bus, audio, haptics,
    /// localization, popups and save (reward goes to ProgressService).
    /// </summary>
    public sealed class GameController : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private Collectible _collectiblePrefab;
        [SerializeField] private PoolConfig _poolConfig;
        [SerializeField, Min(1)] private int _targetCount = 10;
        [SerializeField, Min(1f)] private float _timeLimit = 20f;
        [SerializeField, Min(0.05f)] private float _spawnInterval = 0.6f;
        [SerializeField] private Vector2 _spawnArea = new(4f, 7f);
        [SerializeField] private int _reward = 25;

        [Header("UI")]
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _progressText;
        [SerializeField] private ResultPopup _resultPopup;

        private readonly StateMachine<GameState> _states = new();
        private PoolService _pools;
        private AudioService _audio;
        private HapticService _haptics;
        private LocalizationService _localization;
        private float _timeLeft;
        private float _spawnTimer;
        private int _collected;

        private void Start()
        {
            _pools = Services.Get<PoolService>();
            _audio = Services.Get<AudioService>();
            _haptics = Services.Get<HapticService>();
            _localization = Services.Get<LocalizationService>();
            _pools.Prewarm(_poolConfig);

            _states.Add(GameState.Playing, new PlayingState(this))
                   .Add(GameState.Won, new ResultState(this, won: true))
                   .Add(GameState.Lost, new ResultState(this, won: false));
            _states.ChangeState(GameState.Playing);

            DebugPanel.AddAction("Win level", () => EndLevel(won: true));
            DebugPanel.AddAction("Lose level", () => EndLevel(won: false));
        }

        private void OnEnable()
        {
            EventBus<CollectibleTapped>.Subscribe(OnTapped);
            EventBus<CollectibleExpired>.Subscribe(OnExpired);
        }

        private void OnDisable()
        {
            EventBus<CollectibleTapped>.Unsubscribe(OnTapped);
            EventBus<CollectibleExpired>.Unsubscribe(OnExpired);
            DebugPanel.RemoveAction("Win level");
            DebugPanel.RemoveAction("Lose level");
        }

        private void Update() => _states.Tick(Time.deltaTime);

        private void OnTapped(CollectibleTapped evt)
        {
            _pools.Despawn(evt.Target);
            if (_states.CurrentKey != GameState.Playing) return;

            _collected++;
            _audio.PlaySfx("collect", Random.Range(0.95f, 1.1f));
            _haptics.Play(HapticStrength.Light);
            TweenPresets.Punch(_progressText.transform);
            RefreshHud();
            if (_collected >= _targetCount) EndLevel(won: true);
        }

        private void OnExpired(CollectibleExpired evt) => _pools.Despawn(evt.Target);

        private void RefreshHud()
        {
            _timerText.text = Mathf.CeilToInt(Mathf.Max(0f, _timeLeft)).ToString();
            _progressText.text = _localization.Format("game.progress", _collected, _targetCount);
        }

        private void SpawnOne()
        {
            var position = new Vector3(
                Random.Range(-_spawnArea.x, _spawnArea.x) * 0.5f,
                Random.Range(-_spawnArea.y, _spawnArea.y) * 0.5f, 0f);
            // No scene parent: pooled objects stay under the Boot scene's pool root,
            // so unloading the Game scene can never destroy them.
            _pools.Spawn(_collectiblePrefab, position);
        }

        private void EndLevel(bool won)
        {
            if (_states.CurrentKey != GameState.Playing) return;
            _states.ChangeState(won ? GameState.Won : GameState.Lost);
        }

        private sealed class PlayingState : State
        {
            private readonly GameController _c;
            public PlayingState(GameController controller) => _c = controller;

            public override void Enter()
            {
                _c._collected = 0;
                _c._timeLeft = _c._timeLimit;
                _c._spawnTimer = 0f;
                _c.RefreshHud();
            }

            public override void Tick(float deltaTime)
            {
                _c._timeLeft -= deltaTime;
                _c._spawnTimer -= deltaTime;
                if (_c._spawnTimer <= 0f)
                {
                    _c._spawnTimer = _c._spawnInterval;
                    _c.SpawnOne();
                }
                _c.RefreshHud();
                if (_c._timeLeft <= 0f) _c.EndLevel(won: false);
            }
        }

        private sealed class ResultState : State
        {
            private readonly GameController _c;
            private readonly bool _won;

            public ResultState(GameController controller, bool won)
            {
                _c = controller;
                _won = won;
            }

            public override void Enter()
            {
                _c._pools.DespawnAll();
                if (_won) Services.Get<ProgressService>().CompleteLevel(_c._reward);
                _c._audio.PlaySfx(_won ? "win" : "lose");
                _c._haptics.Play(_won ? HapticStrength.Heavy : HapticStrength.Medium);
                Services.Get<PopupService>().Show(_c._resultPopup, new ResultArgs(_won, _c._reward));
            }
        }
    }
}
