using System.Collections.Generic;
using UnityEngine;

namespace MVP.System.GenerateUI
{
    [CreateAssetMenu(fileName = "UIRegistry", menuName = "UI/MVP/UI Registry")]
    public class UIRegistrySO : ScriptableObject
    {
        [SerializeField] private List<GameObject> prefabs = new();
        public IReadOnlyList<GameObject> Prefabs => prefabs;
    }
}
