using System;
using MVP.Interaction;
using MVP.System.BaseMVP.Form;
using MVP.UIData;
using UnityEngine;

namespace MVP.Forms
{
    public class LockButtonForm : BaseForm
    {
        public string Text => _textForm.Text;

        private TextForm _textForm;
        private ButtonForm _buttonForm;
        private InteractionFeedback _feedback;
        private string _interactiveFalseText;

        public void SetTextAndButtonForm(TextForm textForm, ButtonForm buttonForm)
        {
            _textForm = textForm;
            _buttonForm = buttonForm;
            _feedback = buttonForm.GetComponent<InteractionFeedback>();

            _buttonForm.OnFormInteracted += UpdateText;
        }

        private void OnDestroy()
        {
            if (_buttonForm != null)
                _buttonForm.OnFormInteracted -= UpdateText;
        }

        private void UpdateText(UIParam _ = null) => _textForm.UpdateForm();

        public void SetInteractiveFalseText(string interactableFalseText)
            => _interactiveFalseText = interactableFalseText;

        public void SetInteractive(bool interactive)
        {
            Debug.Assert(!String.IsNullOrEmpty(_interactiveFalseText), "_interactiveFalseText is empty");

            if (interactive == false)
            {
                _textForm.Text = _interactiveFalseText;
            }

            if (_feedback != null)
                _feedback.SetInteractable(interactive);
            else
                Debug.LogWarning($"[MVP] {name}: 버튼에 InteractionFeedback이 없어 잠금 연출/클릭 차단이 적용되지 않습니다.", this);
        }
    }
}
