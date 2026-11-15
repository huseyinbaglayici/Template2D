using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Core.Pooling
{
    /// <summary>
    /// Data-driven pool sizes: designers tune WarmCount/MaxCount here,
    /// code only asks the PoolService for a prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/Pool Config", fileName = "PoolConfig")]
    public sealed class PoolConfig : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public GameObject Prefab;
            [Min(0)] public int WarmCount;
            [Min(1)] public int MaxCount;
        }

        [SerializeField] private List<Entry> _entries = new();

        public IReadOnlyList<Entry> Entries => _entries;
    }
}
