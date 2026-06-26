using System.Collections.Generic;
using UnityEngine;

namespace MVP.System.GameState
{
    /// <summary>
    /// 커서 자유(보이기+잠금해제)의 전역 holder 저장소.
    /// <see cref="TimeManager"/>와 동일한 holder-set 패턴. 하나라도 커서를 요구하면 자유,
    /// 마지막 holder가 빠질 때만 게임 기본값(숨김+Locked)으로 복귀한다.
    ///
    /// 여러 팝업이 겹쳐 열려도 안쪽이 닫혔다고 커서가 잠겨버리지 않는다.
    ///
    /// 사용 예:
    /// <code>
    /// CursorManager.Free(this); // 팝업 열림 — 마우스 조작 필요
    /// CursorManager.Lock(this); // 팝업 닫힘 — 게임 조작으로 복귀
    /// </code>
    /// </summary>
    public static class CursorManager
    {
        private static readonly HashSet<object> Holders = new();

        /// <summary>하나라도 커서를 요구하는 holder가 있으면 true.</summary>
        public static bool IsFree => Holders.Count > 0;

        /// <summary>해당 holder로 커서를 자유롭게 한다. 같은 holder 중복 호출은 멱등.</summary>
        public static void Free<T>(T holder) where T : class
        {
            if (holder != null && Holders.Add(holder) && Holders.Count == 1)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
        }

        /// <summary>해당 holder의 요구를 푼다. 남은 holder가 있으면 커서는 그대로 자유.</summary>
        public static void Lock<T>(T holder) where T : class
        {
            if (holder != null && Holders.Remove(holder) && Holders.Count == 0)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        /// <summary>모든 holder를 제거하고 게임 기본값(숨김+Locked)으로 복귀한다.</summary>
        public static void Clear()
        {
            Holders.Clear();
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
