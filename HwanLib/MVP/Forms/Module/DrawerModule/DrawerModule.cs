using System;
using DG.Tweening;
using MVP.Utility;
using UnityEngine;

namespace MVP.Forms.Module.DrawerModule
{
    public class DrawerModule
    {
        public event Action<bool> OnDrawEnd;
        
        private RectTransform _rectTrm;
        private Vector2 _originalPos;
        private DrawDirection _drawDirection;
        
        public DrawerModule(RectTransform rectTrm, DrawDirection direction)
        {
            _rectTrm = rectTrm;
            Vector2 targetPivot = direction switch
            {
                DrawDirection.Up => new Vector2(0.5f, 0),
                DrawDirection.Down => new Vector2(0.5f, 1),
                DrawDirection.Left => new Vector2(1, 0.5f),
                DrawDirection.Right => new Vector2(0, 0.5f),
                _ => Vector2.zero
            };
            
            _rectTrm.SetPivotWithoutScreenPosChange(targetPivot);
            
            _originalPos = _rectTrm.anchoredPosition;
            _drawDirection = direction;
        }

        public void Draw(bool isIn, float duration, bool setActive)
        {
            _rectTrm.DOComplete();
            _rectTrm.DOKill();
            
            if (setActive && isIn)
                _rectTrm.gameObject.SetActive(true);

            if (_drawDirection is DrawDirection.Up or DrawDirection.Down)
            {
                _rectTrm.DOAnchorPosY(isIn ? _originalPos.y : 0, duration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (setActive && isIn == false)
                            _rectTrm.gameObject.SetActive(false);
                        OnDrawEnd?.Invoke(isIn);
                    });
            }
            else
            {
                _rectTrm.DOAnchorPosX(isIn ? _originalPos.x : 0, duration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (setActive && isIn == false)
                            _rectTrm.gameObject.SetActive(false);
                        OnDrawEnd?.Invoke(isIn);
                    });
            }
        }
    }
}