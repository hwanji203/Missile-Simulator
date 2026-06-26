using System.Collections.Generic;
using DG.Tweening;
using MVP.System.AbstractMVP.Form;
using MVP.System.BaseMVP;
using MVP.UIData;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    public class SwapForm : AbstractVisualForm, IInitializable
    {
        [SerializeField] private float swapDuration = 0.3f;

        private Dictionary<int, int> _childDict;
        private RectTransform[] _currentChildren;
        private Sequence _sequence;
        private float _startSwapTime;

        public void Initialize()
        {
            _childDict = new Dictionary<int, int>();
            _currentChildren = new RectTransform[transform.childCount];
            
            for (int i = 0; i < transform.childCount; ++i)
            {
                RectTransform rectTrm = transform.GetChild(i).GetComponent<RectTransform>();
                _childDict.Add(i, i);
                _currentChildren[i] = rectTrm;
            }
            
            // LayoutGroup은 위치랑 모양만 잡고 끄기
            SetOffLayoutGroup();
            _sequence = DOTween.Sequence();
        }
        
        //그냥 enable을 끄면 Layout 깨짐.
        private void SetOffLayoutGroup()
        {
            VerticalLayoutGroup layoutGroup = GetComponent<VerticalLayoutGroup>();
            if (layoutGroup == null || layoutGroup.enabled == false)
                return;
            
            layoutGroup.CalculateLayoutInputHorizontal();
            layoutGroup.CalculateLayoutInputVertical();
            layoutGroup.SetLayoutHorizontal();
            layoutGroup.SetLayoutVertical();
            
            layoutGroup.enabled = false;
        }

        protected override void UpdateVisual(UIParam data)
        {
            UISwapParam swapData = (UISwapParam)data;
            
            SwapItem(swapData.ItemEnum, swapData.TargetIndex);
        }

        private void SwapItem(int itemEnum, int targetIndex)
        {
            int targetItemIdx = _childDict[itemEnum];
            if (targetItemIdx == targetIndex)
                return;

            DoMove(targetItemIdx, targetIndex);

            //Swap
            (_currentChildren[targetIndex], _currentChildren[targetItemIdx]) 
                = (_currentChildren[targetItemIdx], _currentChildren[targetIndex]);
            (_childDict[targetIndex], _childDict[targetItemIdx]) 
                = (_childDict[targetItemIdx], _childDict[targetIndex]);
        }

        private void DoMove(int item1Idx, int item2Idx)
        {
            if (_sequence.IsActive() == true)
            {
                _sequence.Complete();
                _sequence.Kill();
            }
                
            Vector2 item1Pos = _currentChildren[item1Idx].anchoredPosition;
            Vector2 item2Pos = _currentChildren[item2Idx].anchoredPosition;
            
            Vector2 item1Size = _currentChildren[item1Idx].sizeDelta;
            Vector2 item2Size = _currentChildren[item2Idx].sizeDelta;
            
            _sequence = DOTween.Sequence();
            _sequence
                .Append(_currentChildren[item1Idx]
                    .DOAnchorPos(item2Pos, swapDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true))
                .Join(_currentChildren[item1Idx]
                    .DOSizeDelta(item2Size, swapDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true))
                .Join(_currentChildren[item2Idx]
                    .DOAnchorPos(item1Pos, swapDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true))
                .Join(_currentChildren[item2Idx]
                    .DOSizeDelta(item1Size, swapDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true));
        }
        
        private void OnDestroy()
        {
            if (_sequence.IsActive() == true)
            {
                _sequence.Complete();
                _sequence.Kill();
            }
        }
    }
}