using System;
using System.Collections.Generic;
using MVP.System.AbstractMVP.Form;
using MVP.System.BaseMVP;
using MVP.UIData;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    public class CountBarForm : AbstractVisualForm, IInitializable
    {
        [SerializeField] private Image rectPrefab;
        [SerializeField] private int firstInstantiateCount = 10;
        [SerializeField] private Color fillColor = Color.white;
        
        private List<(Image, GameObject)> _rectPool;

        // 새로 켜진 칸(fill)의 RectTransform을 알린다. 연출(CountBarUpgradeFx)이 구독한다.
        public event Action<RectTransform> SlotFilled;

        private int _prevLevel;     // 직전 currentLevel
        private bool _initialized;  // 첫 UpdateVisual(초기 세팅) 가드

        public void Initialize()
        {
            _rectPool = new List<(Image, GameObject)>();
            
            for (int i = 0; i < firstInstantiateCount; ++i)
            {
                AddRect();
            }
        }

        protected override void UpdateVisual(UIParam data)
        {
            int maxLevel = (int)((UIBarParam)data).MaxValue;
            int currentLevel = (int)((UIBarParam)data).CurrentValue;
            
            for (int i = 0; i < _rectPool.Count; ++i)
            {
                var (image, fill) = PopRect(i);
                image.gameObject.SetActive(i < maxLevel);
                fill.gameObject.SetActive(i < currentLevel);
            }

            // 첫 호출(초기 세팅)은 연출 없이 상태만 기록. 이후 증가한 칸에만 SlotFilled 발행(감소는 무시).
            if (_initialized && currentLevel > _prevLevel)
            {
                for (int i = _prevLevel; i < currentLevel && i < _rectPool.Count; ++i)
                {
                    if (_rectPool[i].Item2.transform is RectTransform fillRect)
                        SlotFilled?.Invoke(fillRect);
                }
            }
            _initialized = true;
            _prevLevel = currentLevel;
        }
        
        private (Image, GameObject) PopRect(int idx)
        {
            if (_rectPool.Count - 1 < idx)
                AddRect();
            return _rectPool[idx];
        }

        private void AddRect()
        {
            Image rect = Instantiate(rectPrefab, transform);
            rect.gameObject.SetActive(false);
            GameObject fill = rect.transform.GetChild(0).gameObject;
            fill.SetActive(false);
            fill.GetComponent<Image>().color = fillColor;
            _rectPool.Add((rect, fill));
        }
    }
}