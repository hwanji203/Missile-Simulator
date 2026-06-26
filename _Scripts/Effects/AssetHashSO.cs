using UnityEngine;

namespace Effects
{
    [CreateAssetMenu(fileName = "AssetHash", menuName = "Effects/AssetHash", order = 0)]
    public class AssetHashSO : ScriptableObject
    {
        [field: SerializeField] public string AssetName { get; private set; }
        [field: SerializeField] public int AssetHash { get; private set; }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(AssetName))
                AssetHash = Animator.StringToHash(AssetName);
        }
    }
}
