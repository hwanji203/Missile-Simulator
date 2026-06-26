using MVP.Forms.Module.Gauge;
using MVP.System.AbstractMVP.Form;
using MVP.UIData;

namespace MVP.Forms
{
    public class CooldownForm : AbstractVisualForm
    {
        private GaugeType _gaugeType;
        private AbstractGaugeModule _cooldown;

        public void InitCooldownForm(GaugeType gaugeType)
        {
            _gaugeType = gaugeType;
            
            switch (_gaugeType)
            {
                case GaugeType.PosY:
                    _cooldown = new PosYGaugeModule();
                    break;
            }
            
            _cooldown.InitGauge(gameObject);
        }

        protected override void UpdateVisual(UIParam data)
        {
            UICooldownParam cooldownData = (UICooldownParam)data;
            _cooldown.SetGauge(cooldownData.Ratio);
            _cooldown.SetGauge(0, cooldownData.Cooldown);
        }

        public void StopCooldown()
            => _cooldown.StopCooldown();

        public void StartCooldown()
            => _cooldown.StartCooldown();

        private void OnDestroy()
            => _cooldown.OnDestroy();
    }
}