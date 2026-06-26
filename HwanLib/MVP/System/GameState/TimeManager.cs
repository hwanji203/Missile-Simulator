using System.Collections.Generic;
using UnityEngine;

namespace MVP.System.GameState
{
    /// <summary>
    /// 시간정지의 전역 holder 저장소.
    /// "누가(holder) 멈췄는지"를 집합으로 들고, 하나라도 잡고 있으면 <c>Time.timeScale = 0</c>.
    ///
    /// naive bool 한 개로는 A가 멈추고 B가 멈춘 뒤 A가 풀면 B가 아직 필요해도 시간이
    /// 돌아가버린다. holder 집합으로 "마지막 하나가 빠질 때만" 재개한다.
    ///
    /// 사용 예:
    /// <code>
    /// TimeManager.Stop(this);   // 팝업 열림
    /// TimeManager.Resume(this); // 팝업 닫힘
    /// </code>
    /// holder가 Resume 없이 파괴되면 시간이 영영 멈추므로, 씬 전환 시
    /// <see cref="GameStateReset"/>가 <see cref="Clear"/>를 호출한다.
    ///
    /// 슬로모 등 가변 timeScale은 아직 없으므로 기본값 1을 가정한다.
    /// </summary>
    public static class TimeManager
    {
        private static readonly HashSet<object> Holders = new();

        /// <summary>하나라도 멈춘 holder가 있으면 true.</summary>
        public static bool IsStopped => Holders.Count > 0;

        /// <summary>해당 holder로 시간을 멈춘다. 같은 holder 중복 호출은 멱등.</summary>
        public static void Stop<T>(T holder) where T : class
        {
            if (holder != null && Holders.Add(holder) && Holders.Count == 1)
                Time.timeScale = 0f;
        }

        /// <summary>해당 holder의 정지를 푼다. 남은 holder가 있으면 timeScale은 그대로 0.</summary>
        public static void Resume<T>(T holder) where T : class
        {
            if (holder != null && Holders.Remove(holder) && Holders.Count == 0)
                Time.timeScale = 1f;
        }

        /// <summary>모든 holder를 제거하고 시간을 복원한다(씬 전환/상태 리셋 시).</summary>
        public static void Clear()
        {
            Holders.Clear();
            Time.timeScale = 1f;
        }
    }
}
