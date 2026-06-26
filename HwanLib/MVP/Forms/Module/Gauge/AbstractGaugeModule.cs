using DG.Tweening;
using UnityEngine;

namespace MVP.Forms.Module.Gauge
{
    public abstract class AbstractGaugeModule
    {
        public void InitGauge(GameObject gameObject)
        {
            Init(gameObject);
            SetGauge(1);
        }

        protected abstract void Init(GameObject gameObject);

        public abstract void SetGauge(float ratio, float duration = 0f, Ease ease = Ease.Linear);

        public abstract void OnDestroy();

        public abstract void StopCooldown();
        
        public abstract void StartCooldown();
    }
}