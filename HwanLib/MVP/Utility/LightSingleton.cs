using UnityEngine;

namespace MVP.Utility
{
    [DefaultExecutionOrder(-100)]
    public class LightSingleton<T> : MonoBehaviour where T : LightSingleton<T>
    {
        protected void Awake()
        {
            T[] managers = FindObjectsByType<T>(FindObjectsSortMode.None);

            if (managers.Length > 1)
                Destroy(gameObject);
            else
            {
                Initialize();
                if (transform.parent != null)
                    DontDestroyOnLoad(transform.parent.gameObject);
                else
                    DontDestroyOnLoad(gameObject);
            }
            
        }

        protected virtual void Initialize()
        {
            
        }
    }
}