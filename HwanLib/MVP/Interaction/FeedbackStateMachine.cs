namespace MVP.Interaction
{
    public enum FeedbackState
    {
        Normal,
        Highlighted,
        Pressed,
        Disabled,
    }

    /// <summary>
    /// 입력 플래그 → 피드백 상태 계산 (순수 로직).
    /// Heat의 "마지막 이벤트 승리" 방식 대신 우선순위로 결정해 호버/포커스 중첩 시에도 일관됨.
    /// Disabled 동안 입력 플래그는 무시되고, Disabled 진입 시 전부 클리어된다.
    /// </summary>
    public class FeedbackStateMachine
    {
        public bool IsHovered { get; private set; }
        public bool IsSelected { get; private set; }
        public bool IsPressed { get; private set; }
        public bool IsInteractable { get; private set; } = true;

        public FeedbackState Current
        {
            get
            {
                if (IsInteractable == false) return FeedbackState.Disabled;
                if (IsPressed) return FeedbackState.Pressed;
                if (IsHovered || IsSelected) return FeedbackState.Highlighted;
                return FeedbackState.Normal;
            }
        }

        public void SetHovered(bool value)
        {
            if (IsInteractable == false) return;
            IsHovered = value;
        }

        public void SetSelected(bool value)
        {
            if (IsInteractable == false) return;
            IsSelected = value;
        }

        public void SetPressed(bool value)
        {
            if (IsInteractable == false) return;
            IsPressed = value;
        }

        public void SetInteractable(bool value)
        {
            IsInteractable = value;

            if (value == false)
            {
                IsHovered = false;
                IsSelected = false;
                IsPressed = false;
            }
        }
    }
}
