using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Pooling
{
    /// <summary>Read-only view of a pool, used by the Pool Monitor window.</summary>
    public readonly struct PoolStats
    {
        public readonly string Name;
        public readonly int InUse;
        public readonly int Available;
        public readonly int Max;
        public readonly int Grows;

        public PoolStats(string name, int inUse, int available, int max, int grows)
        {
            Name = name;
            InUse = inUse;
            Available = available;
            Max = max;
            Grows = grows;
        }
    }

    /// <summary>
    /// Owns one <see cref="ComponentPool{T}"/> per prefab. Pools are created from a <see cref="PoolConfig"/>
    /// when a context starts (warm-up) and lazily on first use for prefabs missing from the config.
    /// </summary>
    public sealed class PoolService : IDisposable
    {
        private const int DefaultMaxCount = 16;

        private readonly Transform _root;
        private readonly Dictionary<GameObject, ComponentPool<Transform>> _byPrefab = new();
        private readonly Dictionary<object, ComponentPool<Transform>> _byPool = new();

        public PoolService(Transform root)
        {
            _root = root;
        }

        public void Prewarm(PoolConfig config)
        {
            if (config == null) return;
            foreach (var entry in config.Entries)
            {
                if (entry.Prefab == null) continue;
                var pool = GetOrCreate(entry.Prefab, entry.MaxCount);
                pool.Warm(entry.WarmCount - pool.TotalCreated);
            }
        }

        public T Spawn<T>(T prefab, Vector3 position, Transform parent = null) where T : Component
        {
            var instance = GetOrCreate(prefab.gameObject, DefaultMaxCount).Get();
            if (parent != null) instance.SetParent(parent, false);
            instance.position = position;
            return instance.GetComponent<T>();
        }

        public void Despawn(Component instance)
        {
            if (instance == null) return;
            var handle = instance.GetComponent<PoolHandle>();
            if (handle != null && handle.Owner != null && _byPool.TryGetValue(handle.Owner, out var pool)
                && pool.Return(instance.transform))
                return;

            Debug.LogWarning($"[Pool] '{instance.name}' is not spawned from a pool; destroying it instead.");
            UnityEngine.Object.Destroy(instance.gameObject);
        }

        /// <summary>Returns every spawned object, e.g. when a level ends.</summary>
        public void DespawnAll()
        {
            foreach (var pool in _byPrefab.Values) pool.ReturnAll();
        }

        public void CollectStats(List<PoolStats> into)
        {
            into.Clear();
            foreach (var pool in _byPrefab.Values)
                into.Add(new PoolStats(pool.Name, pool.InUseCount, pool.AvailableCount, pool.MaxCount, pool.GrowCount));
        }

        public void Dispose()
        {
            foreach (var pool in _byPrefab.Values) pool.DestroyAll();
            _byPrefab.Clear();
            _byPool.Clear();
        }

        private ComponentPool<Transform> GetOrCreate(GameObject prefab, int maxCount)
        {
            if (_byPrefab.TryGetValue(prefab, out var pool)) return pool;

            var container = new GameObject($"Pool - {prefab.name}").transform;
            container.SetParent(_root, false);
            pool = new ComponentPool<Transform>(prefab.transform, container, 0, maxCount);
            _byPrefab.Add(prefab, pool);
            _byPool.Add(pool, pool);
            return pool;
        }
    }
}
