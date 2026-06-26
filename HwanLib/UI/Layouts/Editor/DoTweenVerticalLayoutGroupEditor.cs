using UnityEditor;
using UnityEditor.UI;

namespace HwanLib.UI.Layouts.Editor
{
    /// <summary>
    /// DoTweenVerticalLayoutGroup용 인스펙터.
    /// 순정 VerticalLayoutGroup 인스펙터(padding/spacing/childAlignment/
    /// reverseArrangement/controlChildSize/useChildScale/childForceExpand)를
    /// 그대로 그리고, 그 아래 duration/ease 두 필드만 추가한다.
    /// </summary>
    [CustomEditor(typeof(DoTweenVerticalLayoutGroup), true)]
    [CanEditMultipleObjects]
    public class DoTweenVerticalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        private SerializedProperty _duration;
        private SerializedProperty _ease;

        protected override void OnEnable()
        {
            base.OnEnable();
            _duration = serializedObject.FindProperty("duration");
            _ease = serializedObject.FindProperty("ease");
        }

        public override void OnInspectorGUI()
        {
            // 순정 VLG 인스펙터 전체.
            base.OnInspectorGUI();

            serializedObject.Update();
            EditorGUILayout.PropertyField(_duration);
            EditorGUILayout.PropertyField(_ease);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
