using MVP.System.BaseMVP.Form;
using UnityEditor;

namespace MVP.Editor
{
    [CustomEditor(typeof(BaseForm), true)]
    public class BaseFormEditor : UnityEditor.Editor
    { 
        public override void OnInspectorGUI() => DrawDefaultInspector();
    }
}
