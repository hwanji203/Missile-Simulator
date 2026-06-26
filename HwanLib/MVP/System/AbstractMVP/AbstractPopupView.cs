using System.Collections.Generic;
using MVP.Forms;
using MVP.System.BaseMVP;
using MVP.System.BaseMVP.Form;
using MVP.Utility;
using UnityEngine;

namespace MVP.System.AbstractMVP
{
    public abstract class AbstractPopupView : BaseView
    {
        [SerializeField] protected UITransition  transition;
        [SerializeField] private BackgroundForm backgroundForm; // 선택
        [SerializeField] private BaseForm[]    closeButtons;   // IInteractable인 폼들

        public override bool IsOpen { get; protected set; }

        private CanvasGroup _canvasGroup;
        private readonly List<(IInteractable form, FormInteracted handler)> _closeSubs = new();

        public override void InitializeView(IReadOnlyList<BaseForm> forms)
        {
            base.InitializeView(forms);

            _canvasGroup = RootCanvas.gameObject.GetOrAddComponent<CanvasGroup>();

            foreach (BaseForm closeBtn in closeButtons)
            {
                if (closeBtn is IInteractable interactable)
                {
                    FormInteracted handler = _ => CloseView();
                    interactable.OnFormInteracted += handler;
                    _closeSubs.Add((interactable, handler));
                }
            }

            IsOpen = false;
        }

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            foreach ((IInteractable form, FormInteracted handler) in _closeSubs)
                form.OnFormInteracted -= handler;
            _closeSubs.Clear();
        }

        public override void OpenView()
        {
            if (IsOpen) return;
            base.OpenView();  // SetActive(true) + UpdateView
            IsOpen = true;
            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;
            if (backgroundForm != null) backgroundForm.DoFade(true, transition.ShowDuration);
            transition.PlayShow();
        }

        public override void CloseView()
        {
            if (!IsOpen) return;
            IsOpen = false;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;
            if (backgroundForm != null) backgroundForm.DoFade(false, transition.HideDuration);
            transition.PlayHide(() =>
            {
                RootCanvas.gameObject.SetActive(false);
                RaiseOnViewClosed(); // BaseView 수렴점 → BasePresenter.OnClosed → UIManager
            });
        }
    }
}
