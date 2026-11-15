using System;
using System.Collections.Generic;

namespace Project.Core.Services
{
    /// <summary>
    /// Application-wide service registry. The Bootstrapper registers every service explicitly,
    /// in order, so the dependency graph is readable in one place (no reflection, IL2CPP-safe).
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> Registry = new();

        public static void Register<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (!Registry.TryAdd(typeof(T), service))
                throw new InvalidOperationException($"Service {typeof(T).Name} is already registered.");
        }

        public static T Get<T>() where T : class
        {
            if (Registry.TryGetValue(typeof(T), out var service)) return (T)service;
            throw new InvalidOperationException(
                $"Service {typeof(T).Name} is not registered. Start the game from the Boot scene.");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (Registry.TryGetValue(typeof(T), out var value))
            {
                service = (T)value;
                return true;
            }

            service = null;
            return false;
        }

        public static void Clear()
        {
            foreach (var service in Registry.Values)
                if (service is IDisposable disposable) disposable.Dispose();
            Registry.Clear();
        }

        // Domain reload can be disabled in the Editor; make sure statics start clean every Play.
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Registry.Clear();
    }
}
