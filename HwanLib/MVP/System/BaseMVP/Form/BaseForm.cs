using UnityEngine;

namespace MVP.System.BaseMVP.Form
{
    public abstract class BaseForm : MonoBehaviour
    {
        [SerializeField] private string interactMethod;
        [SerializeField] private string updateMethod;

        /// <summary>IInteractable 일 때만 의미. Model의 void M(UIParam) 메서드명.</summary>
        public string InteractMethod => interactMethod;

        /// <summary>IUpdatable 일 때만 의미. Model의 UIParam M() 메서드명.</summary>
        public string UpdateMethod => updateMethod;

#if UNITY_EDITOR
        public void EditorSetInteractMethod(string value) => interactMethod = value;
        public void EditorSetUpdateMethod(string value) => updateMethod = value;
#endif
    }
}
