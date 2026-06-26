using UnityEngine;

namespace SaveSystem
{
    [CreateAssetMenu(fileName = "new Save data", menuName = "Save/Save data", order = 0)]
    public class SaveData : ScriptableObject
    {
        [field: SerializeField] public int Id { get; private set; }
        [SerializeField, TextArea] private string description;
    }
}