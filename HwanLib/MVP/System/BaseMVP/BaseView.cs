using System;
using System.Collections.Generic;
using MVP.System.BaseMVP.Form;
using UnityEngine;

namespace MVP.System.BaseMVP
{
    public abstract class BaseView : MonoBehaviour
    {
        public Canvas RootCanvas { get; private set; }
        public virtual bool IsOpen
        {
            get => RootCanvas != null && RootCanvas.gameObject.activeSelf;
            protected set { }
        }

        // 닫힘 완료(SetActive false 직전 또는 애니메이션 끝) 시 발행.
        // BasePresenter가 구독 → OnClosed 이벤트로 포워딩.
        public event Action OnViewClosed;

        private IReadOnlyList<BaseForm> _forms;

        public virtual void InitializeView(IReadOnlyList<BaseForm> forms)
        {
            _forms = forms;
            RootCanvas = transform.GetChild(0).GetComponent<Canvas>();
            RootCanvas.vertexColorAlwaysGammaSpace = true;
            RootCanvas.gameObject.SetActive(false);
        }

        public virtual void OpenView()
        {
            RootCanvas.gameObject.SetActive(true);
            UpdateView();
        }

        public virtual void CloseView()
        {
            RootCanvas.gameObject.SetActive(false);
            OnViewClosed?.Invoke();
        }

        public virtual void OnDestroyView() { }

        public virtual void UpdateView()
        {
            foreach (BaseForm form in _forms)
                if (form is IUpdatable updatable)
                    updatable.UpdateForm();
        }

        protected void AddFormInteractionListener(Action handler, IInteractable form)
            => form.OnFormInteracted += _ => handler();

        // 파생 클래스(AbstractPopupView)가 애니메이션 완료 시 호출.
        protected void RaiseOnViewClosed() => OnViewClosed?.Invoke();
    }
}
