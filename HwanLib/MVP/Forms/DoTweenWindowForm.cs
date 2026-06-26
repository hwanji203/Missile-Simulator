using System;
using DG.Tweening;
using MVP.System.BaseMVP;
using MVP.System.BaseMVP.Form;
using UnityEngine;

namespace MVP.Forms
{
    public class DoTweenWindowForm : BaseForm, IInitializable
    {
        [SerializeField] private float openDuration = 0.25f;
        [SerializeField] private float closeDuration = 0.225f;
        
        public event Action OnAnimationEnd;

        private Sequence _sequence;

        public void Initialize()
        {
            _sequence = DOTween.Sequence();
        }

        public void PlayAnimation(bool isOpen)
            => PlayAnimation(isOpen, isOpen ? openDuration : closeDuration);

        public void PlayAnimation(bool isOpen, float duration)
        {
            if (_sequence.IsActive() == true)
            {
                _sequence.Complete();
                _sequence.Kill();
                OnAnimationEnd?.Invoke();
            }
            transform.localScale = isOpen ? Vector3.zero : transform.localScale;
            float curDuration = isOpen ? Mathf.Clamp01(1 - transform.localScale.x) * duration
                : transform.localScale.x * closeDuration;
            Vector3 targetScale = isOpen ? Vector3.one : Vector3.zero;
            Ease ease = isOpen ? Ease.InCirc : Ease.InBack;

            _sequence = DOTween.Sequence();
            _sequence.Append(transform.DOScale(targetScale, curDuration).SetEase(ease))
                .SetUpdate(true)
                .OnComplete(() => OnAnimationEnd?.Invoke());
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