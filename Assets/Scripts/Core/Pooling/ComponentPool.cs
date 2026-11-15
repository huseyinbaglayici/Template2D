using System;
using UnityEngine;

namespace Project.Core.Pooling
{
    /// <summary>Optional callbacks for pooled objects.</summary>
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
    }

    /// <summary>Added to every pooled instance; remembers its owner and its slot in the in-use array.</summary>
    [DisallowMultipleComponent]
    public sealed class PoolHandle : MonoBehaviour
    {
        internal object Owner;
        internal int InUseIndex = -1;
        internal IPoolable[] Callbacks;
    }

    /// <summary>
    /// Fixed-capacity pool for one prefab. Arrays are allocated up front, Get/Return are O(1)
    /// (swap-remove from the in-use array), and the pool doubles its capacity with a warning
    /// when <see cref="MaxCount"/> is exceeded, so a wrong config shows up in the log instead of a crash.
    /// </summary>
    public sealed class ComponentPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _container;
        private T[] _available;
        private T[] _inUse;
        private PoolHandle[] _inUseHandles;
        private int _availableCount;
        private int _inUseCount;

        public string Name { get; }
        public int MaxCount => _available.Length;
        public int AvailableCount => _availableCount;
        public int InUseCount => _inUseCount;
        public int TotalCreated { get; private set; }
        public int GrowCount { get; private set; }

        public ComponentPool(T prefab, Transform container, int warmCount, int maxCount)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));
            _prefab = prefab;
            _container = container;
            Name = prefab.name;
            maxCount = Mathf.Max(1, Mathf.Max(maxCount, warmCount));
            _available = new T[maxCount];
            _inUse = new T[maxCount];
            _inUseHandles = new PoolHandle[maxCount];
            Warm(warmCount);
        }

        public void Warm(int count)
        {
            for (var i = 0; i < count && TotalCreated < MaxCount; i++)
                _available[_availableCount++] = CreateInstance();
        }

        public T Get()
        {
            if (_availableCount == 0)
            {
                if (TotalCreated >= MaxCount) Grow();
                _available[_availableCount++] = CreateInstance();
            }

            var item = _available[--_availableCount];
            _available[_availableCount] = null;

            var handle = item.GetComponent<PoolHandle>();
            handle.InUseIndex = _inUseCount;
            _inUse[_inUseCount] = item;
            _inUseHandles[_inUseCount] = handle;
            _inUseCount++;

            item.gameObject.SetActive(true);
            foreach (var callback in handle.Callbacks) callback.OnSpawn();
            return item;
        }

        /// <returns>false if the object is not currently spawned from this pool.</returns>
        public bool Return(T item)
        {
            if (item == null) return false;
            var handle = item.GetComponent<PoolHandle>();
            if (handle == null || handle.Owner != this || handle.InUseIndex < 0) return false;

            foreach (var callback in handle.Callbacks) callback.OnDespawn();

            var index = handle.InUseIndex;
            var last = --_inUseCount;
            _inUse[index] = _inUse[last];
            _inUseHandles[index] = _inUseHandles[last];
            _inUseHandles[index].InUseIndex = index;
            _inUse[last] = null;
            _inUseHandles[last] = null;
            handle.InUseIndex = -1;

            item.gameObject.SetActive(false);
            item.transform.SetParent(_container, false);
            _available[_availableCount++] = item;
            return true;
        }

        public void ReturnAll()
        {
            while (_inUseCount > 0) Return(_inUse[_inUseCount - 1]);
        }

        public void DestroyAll()
        {
            ReturnAll();
            for (var i = 0; i < _availableCount; i++)
            {
                if (_available[i] != null) UnityEngine.Object.Destroy(_available[i].gameObject);
                _available[i] = null;
            }
            _availableCount = 0;
            TotalCreated = 0;
        }

        private T CreateInstance()
        {
            var item = UnityEngine.Object.Instantiate(_prefab, _container);
            item.name = $"{Name} ({TotalCreated})";
            var handle = item.gameObject.AddComponent<PoolHandle>();
            handle.Owner = this;
            handle.Callbacks = item.GetComponentsInChildren<IPoolable>(true);
            item.gameObject.SetActive(false);
            TotalCreated++;
            return item;
        }

        private void Grow()
        {
            var newMax = Mathf.Max(MaxCount * 2, 1);
            Debug.LogWarning($"[Pool] '{Name}' exceeded MaxCount {MaxCount}, growing to {newMax}. " +
                             "Raise MaxCount in the PoolConfig if this is expected.");
            Array.Resize(ref _available, newMax);
            Array.Resize(ref _inUse, newMax);
            Array.Resize(ref _inUseHandles, newMax);
            GrowCount++;
        }
    }
}
