using System.Collections.Generic;
using UnityEngine;

namespace ObjectPool.Runtime
{
    [CreateAssetMenu(fileName = "PoolManager", menuName = "Lib/Pool/PoolManager", order = 0)]
    public class PoolManagerSO : ScriptableObject
    {
        public List<PoolItemSO> itemList = new();
        private Dictionary<PoolItemSO, Pool> _pools;
        private Transform _rootTrm;

        public void InitializePool(Transform rootTrm)
        {
            _rootTrm = rootTrm;
            _pools = new Dictionary<PoolItemSO, Pool>();
            
            foreach (PoolItemSO item in itemList)
            {
                IPoolable poolable = item.prefab.GetComponent<IPoolable>();
                Debug.Assert(poolable != null, $"PoolItem은 반드시 IPoolable을 가져야합니다. {item.prefab.gameObject}");

                Pool pool = new Pool(poolable,_rootTrm,  item.initCount);
                _pools.Add(item, pool);
            }
        }

        public T Pop<T>(PoolItemSO item) where T : IPoolable
        {
            Debug.Assert(_rootTrm != null, "풀 매니저는 초기화 후 사용해야합니다");

            if (_pools.TryGetValue(item, out Pool pool))
            {
                return (T)pool.Pop();
            }

            return default;
        }

        public void Push(IPoolable item)
        {
            if (_rootTrm == null)
                return;
            Debug.Assert(item != null, $"{item}");
            Debug.Assert(_pools != null, $"{_pools}");
            if (_pools.TryGetValue(item.Item, out Pool pool))
            {
                pool.Push(item);
            }
        }
    }
}