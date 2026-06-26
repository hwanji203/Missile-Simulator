using System.Collections.Generic;

namespace MVP.System.GenerateUI
{
    /// <summary>
    /// UI 열림 차단의 전역 근거 저장소.
    /// 차단 "사유"를 등록/해제하고, 구체 Presenter가 <c>CanOpen</c>에서 조회한다.
    ///
    /// FSM이 아니라 단발 플래그(사유)들의 집합이다. 컷씬·대화처럼 서로 독립인 차단원이
    /// 동시에 걸려도, 각 사유가 모두 해제돼야 비로소 열린다(naive bool 한 개로는
    /// 한쪽만 풀려도 열려버리는 버그가 생김).
    ///
    /// 사용 예 — 차단원(게임 코드):
    /// <code>
    /// UIGate.Block("Cutscene");   // 컷씬 시작
    /// UIGate.Unblock("Cutscene"); // 컷씬 종료
    /// </code>
    /// 게이팅 대상(구체 Presenter):
    /// <code>
    /// public override bool CanOpen => !UIGate.IsBlocked;            // 모든 차단에 반응
    /// public override bool CanOpen => !UIGate.IsBlockedBy("Cutscene"); // 특정 사유만
    /// </code>
    /// 상황이 많아져 상호배타적 게임모드가 되면 차단 "소스"만 enum/FSM으로 교체하면 된다
    /// (게이트 계약 <c>CanOpen</c>은 불변).
    /// </summary>
    public static class UIGate
    {
        private static readonly HashSet<string> Reasons = new();

        /// <summary>차단 사유가 하나라도 있으면 true.</summary>
        public static bool IsBlocked => Reasons.Count > 0;

        /// <summary>해당 사유로 차단 중인지.</summary>
        public static bool IsBlockedBy(string reason) => Reasons.Contains(reason);

        /// <summary>해당 사유로 UI 열림을 차단한다. 같은 사유 중복 호출은 멱등.</summary>
        public static void Block(string reason)
        {
            if (!string.IsNullOrEmpty(reason)) Reasons.Add(reason);
        }

        /// <summary>해당 사유의 차단을 해제한다. 등록되지 않은 사유여도 무해.</summary>
        public static void Unblock(string reason) => Reasons.Remove(reason);

        /// <summary>모든 차단 사유를 제거한다(씬 전환/상태 리셋 시).</summary>
        public static void Clear() => Reasons.Clear();
    }
}
