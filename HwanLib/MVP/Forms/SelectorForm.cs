using System.Collections.Generic;
using MVP.System;
using MVP.System.AbstractMVP.Form;
using MVP.System.BaseMVP;
using MVP.System.BaseMVP.Form;
using MVP.UIData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    /// <summary>
    /// 좌우 선택 위젯. prev/next 버튼으로 옵션 인덱스를 바꾸고 라벨을 갱신, int 인덱스를 발행한다.
    /// Heat HorizontalSelector + ModeSelector를 MVP-native로 통합. 아이콘/현지화/항목별 이벤트는 제외.
    /// </summary>
    public class SelectorForm : AbstractVisualForm, IInteractable, IInitializable
    {
        [SerializeField] private List<string> options = new List<string>();
        [SerializeField] private TMP_Text label;
        [SerializeField] private Button prevButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private bool loop = true;

        public event FormInteracted OnFormInteracted;

        private int _index;

        public void Initialize()
        {
            if (prevButton != null) prevButton.onClick.AddListener(Prev);
            if (nextButton != null) nextButton.onClick.AddListener(Next);
            RefreshLabel();
        }

        public void Next() => Move(+1);
        public void Prev() => Move(-1);

        private void Move(int delta)
        {
            if (options.Count == 0) return;

            int next = _index + delta;

            if (loop)
            {
                next = (next % options.Count + options.Count) % options.Count;
            }
            else
            {
                next = Mathf.Clamp(next, 0, options.Count - 1);
                if (next == _index) return; // 끝에서 변화 없으면 발행 안 함
            }

            _index = next;
            RefreshLabel();
            OnFormInteracted?.Invoke(UIParams.UIIntParam.Init(_index));
        }

        private void RefreshLabel()
        {
            if (label != null && _index >= 0 && _index < options.Count)
                label.text = options[_index];
        }

        protected override void UpdateVisual(UIParam data)
        {
            _index = ((UIIntParam)data).Value;
            RefreshLabel();
        }
    }
}
