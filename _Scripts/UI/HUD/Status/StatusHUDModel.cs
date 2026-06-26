using MVP.System.BaseMVP;
using MVP.UIData;

namespace UI.HUD.Status
{
    // 체력바와 도화선(시간)바를 한 레이아웃으로 묶은 통합 HUD 모델.
    // 체력 Form은 UpdateHp*, 도화선 Form은 UpdateTime* 를 참조한다(프리팹 배선).
    public class StatusHUDModel : IModel
    {
        // ── 체력 ───────────────────────────────────────────────
        // 체력바의 기준 크기(저작 크기)는 MaxHp가 이 값일 때를 의미한다. MaxHp가 이보다 크면 바도 비례해 커진다.

        public float CurrentHp { get; private set; }
        public float MaxHp     { get; private set; }

        public float DefaultMaxHp { get; set; }
        
        public void SetHealth(float current, float max, float defaultMax)
        {
            CurrentHp = current;
            MaxHp     = max;
            DefaultMaxHp = defaultMax;
        }

        private UIParam UpdateHpRatio()
            => UIParams.UIFloatParam.Init(MaxHp > 0f ? CurrentHp / MaxHp : 0f);

        private UIParam UpdateHpText()
            => UIParams.UIStringParam.Init($"{CurrentHp:F0}");

        // 체력바 크기 배율. BarWidthScaleForm이 기준 크기에 이 값을 곱해 바 폭을 키운다(메타 시작·인게임 증가 공통).
        private UIParam UpdateHpBarScale()
            => UIParams.UIFloatParam.Init(MaxHp / DefaultMaxHp);

        // ── 도화선(시간) ──────────────────────────────────────
        public float RemainingTime { get; private set; }
        public float Duration      { get; private set; }
        public bool  IsInfinite    { get; private set; }
        public float DefaultDuration { get; private set; }
        public float SpareTime { get; private set; }

        public void SetTime(float remaining, float duration, bool isInfinite, float defaultDuration, float spareTime)
        {
            RemainingTime = remaining;
            Duration      = duration;
            IsInfinite    = isInfinite;
            DefaultDuration = defaultDuration;
            SpareTime = spareTime;
        }

        // 슈퍼 부스트(∞) 중엔 바를 꽉 채운다.
        private UIParam UpdateTimeRatio()
            => UIParams.UIFloatParam.Init(IsInfinite ? 1f : Duration > 0f ? RemainingTime / Duration : 0f);

        private UIParam UpdateTimeText()
            => UIParams.UIStringParam.Init(IsInfinite ? "∞" : $"{RemainingTime:0.0} (±{SpareTime:0.0})");

        private UIParam UpdateFuseBarScale()
            => UIParams.UIFloatParam.Init(Duration / DefaultDuration);
    }
}
