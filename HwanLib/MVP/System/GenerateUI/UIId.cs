namespace MVP.System.GenerateUI
{
    // 라우팅 키. UI 추가 시 이 enum에 항목 하나만 추가.
    // <generated> 영역은 MVP 생성기가 자동 관리한다. 손으로 값을 재배열/삭제하지 말 것
    // (프리팹 직렬화 안전). 새 항목은 항상 max+1로만 append.
    public enum UIId
    {
        None = 0,
        // <generated>
        Setting = 1,
        AcquisitionToast = 2,
        SuperBoostWarning = 3,
        StatusHUD = 4,
        MetaProgress = 5,
        Result = 6,
        Fade = 7,
        Select = 8,
        Title = 9,
        // </generated>
    }
}
