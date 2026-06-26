using UnityEngine;

namespace ObjectPool.Runtime
{
    public class PoolInitializer : MonoBehaviour
    {
        [field: SerializeField] public PoolManagerSO PoolManagerAsset { get; private set; }

        private void Awake()
        {
            PoolInitializer[] initializers = FindObjectsByType<PoolInitializer>(FindObjectsSortMode.None);
            if (initializers.Length > 1)
                return;
            
            PoolManagerAsset.InitializePool(transform);
            if (transform.parent != null)
                DontDestroyOnLoad(transform.parent.gameObject);
            else 
                DontDestroyOnLoad(gameObject);
        }
    }
}