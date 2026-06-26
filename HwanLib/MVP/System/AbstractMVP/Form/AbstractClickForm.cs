using MVP.System.BaseMVP.Form;
using MVP.UIData;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MVP.System.AbstractMVP.Form
{
    /// <summary>
    /// 클릭 의미를 발행하는 Form. 포인터 좌클릭과 패드/키보드 Submit을 동일하게 취급한다.
    /// 비활성화는 같은 GameObject의 CanvasGroup.interactable을 따른다(InteractionFeedback과의 접점).
    /// </summary>
    public abstract class AbstractClickForm : BaseForm, IInteractable, IPointerClickHandler, ISubmitHandler
    {
        public event FormInteracted OnFormInteracted;

        private CanvasGroup _canvasGroup;
        private bool _canvasGroupSearched;

        protected bool IsInteractable
        {
            get
            {
                if (_canvasGroupSearched == false)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                    _canvasGroupSearched = true;
                }
                return _canvasGroup == null || _canvasGroup.interactable;
            }
        }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (IsInteractable == false || eventData.button != PointerEventData.InputButton.Left) return;

            OnFormInteracted?.Invoke(UIParams.UIClickParam);
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            if (IsInteractable == false) return;

            OnFormInteracted?.Invoke(UIParams.UIClickParam);
        }
    }
}
