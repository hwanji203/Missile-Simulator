using MVP.Interaction;
using MVP.System.BaseMVP.Form;
using UnityEditor;
using UnityEngine;

namespace MVP.Editor
{
    /// <summary>
    /// 하이라키 창 각 행 우측에 장착된 Form 타입과 바인딩 메서드를 표시.
    /// I: = Interact 바인딩, U: = Update 바인딩, ! = 누락(빨간색), [F] = InteractionFeedback 부착.
    /// Tools/MVP/Hierarchy Labels 메뉴로 켜고 끔.
    /// </summary>
    [InitializeOnLoad]
    public static class MVPHierarchyOverlay
    {
        private const string PrefKey = "MVP.HierarchyOverlay.Enabled";
        private const string MenuPath = "Tools/MVP/Hierarchy Labels";

        private const float MinNameSpace = 120f;

        private static GUIStyle _normalStyle;
        private static GUIStyle _missingStyle;

        static MVPHierarchyOverlay()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyItemGUI;
        }

        [MenuItem(MenuPath)]
        private static void Toggle()
        {
            EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, true));
            EditorApplication.RepaintHierarchyWindow();
        }

        [MenuItem(MenuPath, true)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, true));
            return true;
        }

        private static void OnHierarchyItemGUI(int instanceID, Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (!EditorPrefs.GetBool(PrefKey, true)) return;
#pragma warning disable CS0618
            if (EditorUtility.InstanceIDToObject(instanceID) is not GameObject go) return;
#pragma warning restore CS0618

            BaseForm[] forms = go.GetComponents<BaseForm>();
            if (forms.Length == 0) return;

            EnsureStyles();

            bool missing = false;
            string text = "";
            foreach (BaseForm form in forms)
            {
                if (text.Length > 0) text += "  ";
                text += Describe(form, ref missing);
            }
            if (go.GetComponent<InteractionFeedback>() != null) text += " [F]";

            GUIStyle style = missing ? _missingStyle : _normalStyle;
            float maxWidth = rect.width - MinNameSpace;
            if (maxWidth <= 0) return;
            string display = TruncateToFit(text, style, maxWidth);
            float width = style.CalcSize(new GUIContent(display)).x;
            Rect labelRect = new(rect.xMax - width, rect.y, width, rect.height);
            GUI.Label(labelRect, display, style);
        }

        private static string Describe(BaseForm form, ref bool missing)
        {
            string s = form.GetType().Name;
            if (form is IInteractable)
            {
                bool bound = !string.IsNullOrEmpty(form.InteractMethod);
                s += bound ? $" I:{form.InteractMethod}" : " I:!";
                missing |= !bound;
            }
            if (form is IUpdatable)
            {
                bool bound = !string.IsNullOrEmpty(form.UpdateMethod);
                s += bound ? $" U:{form.UpdateMethod}" : " U:!";
                missing |= !bound;
            }
            return s;
        }

        private static string TruncateToFit(string text, GUIStyle style, float maxWidth)
        {
            if (style.CalcSize(new GUIContent(text)).x <= maxWidth) return text;
            const string ellipsis = "...";
            string s = text;
            while (s.Length > 0 && style.CalcSize(new GUIContent(s + ellipsis)).x > maxWidth)
                s = s[..^1];
            return s.Length > 0 ? s + ellipsis : ellipsis;
        }

        private static void EnsureStyles()
        {
            if (_normalStyle != null) return;
            _normalStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
                normal = { textColor = new Color(0.55f, 0.72f, 0.85f) }
            };
            _missingStyle = new GUIStyle(_normalStyle)
            {
                normal = { textColor = new Color(1f, 0.42f, 0.38f) }
            };
        }
    }
}
