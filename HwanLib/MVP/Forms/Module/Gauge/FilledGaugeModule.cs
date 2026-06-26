using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MVP.Forms.Module.Gauge
{
    public class FilledGaugeModule : AbstractGaugeModule
    {
        private Image _targetImage;

        protected override void Init(GameObject gameObject)
        {
            _targetImage = gameObject.GetComponent<Image>();
            _targetImage.fillAmount = 1;
        }

        public override void SetGauge(float ratio, float duration = 0, Ease ease = Ease.Linear)
        {
            _targetImage.DOKill(true);
            _targetImage.DOFillAmount(ratio, duration).SetEase(ease).SetUpdate(true);
        }

        public override void OnDestroy()
        {
            _targetImage.DOKill();
        }

        public override void StopCooldown()
        {
            _targetImage.DOPause();
        }

        public override void StartCooldown()
        {
            _targetImage.DOPlay();
        }
    }
}