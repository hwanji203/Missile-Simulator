using MVP.System;
using MVP.System.AbstractMVP.Form;
using MVP.System.BaseMVP;
using MVP.System.BaseMVP.Form;
using MVP.UIData;
using UnityEngine.UI;

namespace MVP.Forms
{
    public class ToggleForm : AbstractVisualForm, IInteractable, IInitializable
    {
        public event FormInteracted OnFormInteracted;

        private Toggle _toggle;

        public void Initialize()
        {
            _toggle = GetComponent<Toggle>();
            _toggle.onValueChanged.AddListener(SliderValueChangeHandler);
        }

        private void SliderValueChangeHandler(bool value)
        {
            OnFormInteracted?.Invoke(UIParams.UIBoolParam.Init(value));
        }

        protected override void UpdateVisual(UIParam data)
        {
            _toggle.isOn = ((UIBoolParam)data).Value;
        }
    }
}