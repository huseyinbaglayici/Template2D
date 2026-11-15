using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Updater
{
    public interface IUpdatable
    {
        void Tick(float deltaTime);
    }

    public interface ISecondUpdatable
    {
        void SecondTick();
    }

    /// <summary>
    /// One MonoBehaviour Update for plain C# services: per-frame ticks and once-per-second ticks
    /// (timers, countdowns). Lives in the Boot scene, so it survives Home/Game scene changes.
    /// </summary>
    public sealed class UpdateDispatcher : MonoBehaviour
    {
        private readonly List<IUpdatable> _frame = new();
        private readonly List<ISecondUpdatable> _second = new();
        private readonly List<IUpdatable> _frameSnapshot = new();
        private readonly List<ISecondUpdatable> _secondSnapshot = new();
        private float _secondTimer;

        public void Add(IUpdatable item)
        {
            if (!_frame.Contains(item)) _frame.Add(item);
        }

        public void Remove(IUpdatable item) => _frame.Remove(item);

        public void Add(ISecondUpdatable item)
        {
            if (!_second.Contains(item)) _second.Add(item);
        }

        public void Remove(ISecondUpdatable item) => _second.Remove(item);

        private void Update()
        {
            var dt = Time.deltaTime;
            _frameSnapshot.Clear();
            _frameSnapshot.AddRange(_frame);
            foreach (var item in _frameSnapshot) item.Tick(dt);

            _secondTimer += Time.unscaledDeltaTime;
            if (_secondTimer < 1f) return;
            _secondTimer -= 1f;
            _secondSnapshot.Clear();
            _secondSnapshot.AddRange(_second);
            foreach (var item in _secondSnapshot) item.SecondTick();
        }
    }
}
