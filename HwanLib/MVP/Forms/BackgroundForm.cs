using System;
using DG.Tweening;
using MVP.System.AbstractMVP.Form;
using MVP.System.BaseMVP;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms
{
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class BackgroundForm : AbstractClickForm, IInitializable
    {
        [Header("Default Fade Alpha")]
        [SerializeField] private float fadeInAlpha = 0.6f;
        
        [Header("Default Duration")]
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.3f;
        
        [Header("Settings")]
        [field: SerializeField] public bool ResetOnStart { get; set; } = false;
        [field: SerializeField] public bool CompleteOnStart { get; set; } = false;
        
        public event Action<bool> OnFadeEnd;
        
        private Image _backgroundImage;
        private CanvasGroup _bgCanvasGroup;

        public void Initialize()
        {
            _bgCanvasGroup = GetComponent<CanvasGroup>();
            _bgCanvasGroup.alpha = 0;
            
            GenerateBackground();
        }
        
        private void GenerateBackground()
        {
            Canvas rootCanvas = GetComponentInParent<Canvas>();
            
            var image = GetComponent<Image>();

            // stretch, stretch 설정
            transform.localScale = Vector3.one;
            RectTransform rectTrm = GetComponent<RectTransform>();
            rectTrm.anchorMin = Vector2.zero;
            rectTrm.anchorMax = Vector2.one;
            rectTrm.offsetMax = Vector2.zero;
            rectTrm.offsetMin = Vector2.zero;
            
            transform.SetParent(rootCanvas.transform, false);
            transform.SetAsFirstSibling();

            _backgroundImage = image;
        }
        
        public void DoFade(bool isFadeIn, float duration, float originAlpha)
        {
            if (CompleteOnStart) _bgCanvasGroup.DOComplete();
            _bgCanvasGroup.DOKill();
            if (ResetOnStart) _bgCanvasGroup.alpha = isFadeIn ? 0 : 1;

            _backgroundImage.color = new Color(0, 0, 0, originAlpha);
            _bgCanvasGroup.DOFade(isFadeIn ? 1 : 0, duration).SetUpdate(true).SetEase(Ease.Linear)
                .OnComplete(() => OnFadeEnd?.Invoke(isFadeIn));
        }
        
        public void DoFade(bool isFadeIn)
            => DoFade(isFadeIn, isFadeIn ? fadeInDuration : fadeOutDuration, fadeInAlpha);
                
        public void DoFade(bool isFadeIn, float duration)
            => DoFade(isFadeIn, duration, fadeInAlpha);
    }
}