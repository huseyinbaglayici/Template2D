using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Events
{
    /// <summary>Marker for event payloads. Use small structs so raising an event does not allocate.</summary>
    public interface IEvent { }

    /// <summary>
    /// Typed, synchronous event bus: EventBus&lt;LevelWon&gt;.Raise(new LevelWon(...)).
    /// Handlers may subscribe or unsubscribe while the event is being raised.
    /// </summary>
    public static class EventBus<T> where T : struct, IEvent
    {
        private static readonly List<Action<T>> Handlers = new();
        private static readonly List<Action<T>> Snapshot = new();

        static EventBus() => EventBusReset.Register(() => Handlers.Clear());

        public static void Subscribe(Action<T> handler)
        {
            if (handler != null && !Handlers.Contains(handler)) Handlers.Add(handler);
        }

        public static void Unsubscribe(Action<T> handler) => Handlers.Remove(handler);

        public static void Raise(T evt)
        {
            Snapshot.Clear();
            Snapshot.AddRange(Handlers);
            foreach (var handler in Snapshot)
            {
                try
                {
                    handler(evt);
                }
                catch (Exception e)
                {
                    // One faulty listener must not stop the others.
                    Debug.LogException(e);
                }
            }
            Snapshot.Clear();
        }
    }

    /// <summary>Clears every bus at the start of Play (needed when Editor domain reload is disabled).</summary>
    internal static class EventBusReset
    {
        private static readonly List<Action> Clearers = new();

        public static void Register(Action clear) => Clearers.Add(clear);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            foreach (var clear in Clearers) clear();
        }
    }
}
