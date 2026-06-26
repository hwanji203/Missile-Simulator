using UnityEngine;

namespace ObjectPool.Runtime
{
    public class AbstractMonoPoolable : MonoBehaviour, IPoolable
    {
        [field: SerializeField] public PoolItemSO Item { get; set; }
        public GameObject GameObject => this != null ? gameObject : null;
        //Fake Null 방지
        
        public virtual void ResetItem() { }
    }
}