using EventChannelSystem;
using Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Testing
{
    // [임시/디버그] L 키 = 레벨업 시뮬레이션 → Peek 카메라 활성화(쏙 나왔다 들어감).
    // 실제로는 공중 링 통과 시 능력 상승(폭발력/범위 ↑)과 함께 이 이벤트를 발행하게 된다.
    // 지금은 그 연출만 미리 확인하려고 L 키로 PlayerPeekRequestedEvent를 쏜다.
    public class DebugLevelUpTrigger : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerEvent;   //CameraModeModule이 듣는 것과 같은 채널 에셋

        private void Update()
        {
            if (Keyboard.current == null) return;
            if (Keyboard.current.mKey.wasPressedThisFrame)
                playerEvent?.RaiseEvent(PlayerEvents.PlayerPeekRequestedEvent);
        }
    }
}
