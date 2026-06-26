using UnityEngine;

namespace Players
{
    [CreateAssetMenu(fileName = "Player Default Status", menuName = "Player/Default Status SO", order = 0)]
    public class PlayerDefaultStatusSO : ScriptableObject
    {
        [field: SerializeField] public float DefaultHp { get; set; } = 50f;
        [field: SerializeField] public float DefaultFuseTime { get; set; } = 60f;
    }
}