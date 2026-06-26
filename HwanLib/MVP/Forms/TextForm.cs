using MVP.Forms.Module;
using MVP.Forms.Module.DrawerModule;
using MVP.System.AbstractMVP.Form;
using MVP.System.BaseMVP;
using MVP.UIData;
using TMPro;
using UnityEngine;

namespace MVP.Forms
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextForm : AbstractVisualForm, IInitializable
    {
        public string Text
        {
            get => TextModule.Text;
            set => TextModule.Text = value;
        }

        public DrawerModule DrawerModule { get; private set; }
        public TextModule TextModule { get; private set; }
        
        public void Initialize()
        {
            TextModule = new TextModule(GetComponent<TextMeshProUGUI>());
        }
        
        public void InitializeDrawer(DrawDirection drawDirection)
            => DrawerModule = new DrawerModule(GetComponent<RectTransform>(), drawDirection);
        
        protected override void UpdateVisual(UIParam data)
        {
            string text = ((UIStringParam)data)?.Value;
            TextModule.UpdateText(text);
        }
    }
}