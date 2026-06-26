using System;
using TMPro;

namespace MVP.Forms.Module
{
    public class TextModule
    {
        public string Text
        {
            get => _textMeshProUGUI.text;
            set => _textMeshProUGUI.text = value;
        }

        private readonly TextMeshProUGUI _textMeshProUGUI;
        private readonly string _originalText;

        public TextModule(TextMeshProUGUI textMeshProUGUI)
        {
            _textMeshProUGUI = textMeshProUGUI;
            _originalText = _textMeshProUGUI.text;
        }
        
        public void UpdateText(string text)
        {
            if (text != null)
                Text = String.Format(_originalText, text);
        }
    }
}