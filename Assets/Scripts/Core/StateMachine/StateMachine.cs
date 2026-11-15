using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.StateMachine
{
    public interface IState
    {
        void Enter();
        void Exit();
        void Tick(float deltaTime);
    }

    /// <summary>Base class so simple states only override what they need.</summary>
    public abstract class State : IState
    {
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick(float deltaTime) { }
    }

    /// <summary>
    /// Minimal finite state machine keyed by an enum (e.g. GameState.Playing).
    /// Transitions requested from inside Enter/Exit are queued to keep the order predictable.
    /// </summary>
    public sealed class StateMachine<TKey> where TKey : struct, Enum
    {
        private readonly Dictionary<TKey, IState> _states = new();
        private IState _current;
        private bool _transitioning;
        private TKey? _pending;

        public TKey? CurrentKey { get; private set; }

        public StateMachine<TKey> Add(TKey key, IState state)
        {
            _states[key] = state;
            return this;
        }

        public void ChangeState(TKey key)
        {
            if (_transitioning)
            {
                _pending = key;
                return;
            }

            if (!_states.TryGetValue(key, out var next))
            {
                Debug.LogError($"[StateMachine] No state registered for {key}.");
                return;
            }

            _transitioning = true;
            _current?.Exit();
            _current = next;
            CurrentKey = key;
            _current.Enter();
            _transitioning = false;

            if (_pending is { } queued)
            {
                _pending = null;
                ChangeState(queued);
            }
        }

        public void Tick(float deltaTime) => _current?.Tick(deltaTime);
    }
}
