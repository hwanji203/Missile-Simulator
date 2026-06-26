using DG.Tweening;
using MVP.Utility;
using UnityEngine;

namespace MVP.Forms.Module.Gauge
{
    internal class PosYGaugeModule : AbstractGaugeModule
    {
        private RectTransform _targetTransform;

        protected override void Init(GameObject gameObject)
        {
            _targetTransform = gameObject.GetComponent<RectTransform>();
            Vector3 prevPos = _targetTransform.localPosition;
            _targetTransform.SetPivotWithoutScreenPosChange(new Vector2(0.5f, 0));
            _targetTransform.localPosition = prevPos;
        }

        public override void SetGauge(float ratio, float duration = 0, Ease ease = Ease.Linear)
        {
            _targetTransform.DOKill(true);
            _targetTransform.DOScaleY(ratio, duration).SetEase(ease).SetUpdate(true);
        }

        public override void OnDestroy()
        {
            _targetTransform.DOKill();
        }

        public override void StopCooldown()
        {
            _targetTransform.DOPause();
        }

        public override void StartCooldown()
        {
            _targetTransform.DOPlay();
        }
    }
}