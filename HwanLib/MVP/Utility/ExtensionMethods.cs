using UnityEngine;

namespace MVP.Utility
{
    public static class ExtensionMethods
    {
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            if (gameObject.TryGetComponent(out T component) == false)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}