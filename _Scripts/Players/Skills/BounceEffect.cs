using System.Collections;
using CombatSystem;
using EventChannelSystem;
using Events;
using ModuleSystem;
using ObjectPool.Runtime;
using UnityEngine;

namespace Players.Skills
{
    // 폭발 직후 훅. Bounce 레벨(=N)만큼 "위로 포물선" 물리 바운스를 반복한다.
    //   [위+랜덤 수평 임펄스 발사 → 중력으로 낙하 → 땅 감지(아래 레이캐스트) → 그 자리 비율 폭발] × N
    // 인스턴트 텔레포트(구버전)와 달리 시간차가 있어 실제로 "다시 터지는" 연출이 보인다.
    //
    // [의도] 시간차 바운스에선 각 착지 폭발이 PlayerExplodedEvent를 발행한다.
    //   GameFlowController가 PlayerExplodedEvent마다 종료 타이머를 리셋하므로 → 마지막 바운스 뒤에야 Result.
    //   (구버전은 추가 폭발이 상태성 이벤트를 안 냈지만, 타임드 바운스에선 의도적으로 매번 발행한다.)
    // 추가 폭발은 다시 Bounce/Suck를 일으키지 않는다(무한연쇄 방지) — CastScaledAt은 데미지+VFX만 한다.
    public class BounceEffect : MonoBehaviour
    {
        [Tooltip("재폭발 위력 = 메인 폭발 × 이 비율 (착지마다 고정).")]
        [SerializeField] private float bounceRatio = 0.6f;
        [SerializeField] private PoolItemSO bounceVfx; // 재폭발 VFX(풀)

        [Header("포물선 발사")]
        [Tooltip("튕길 때 위로 주는 속도(m/s). 높을수록 더 높이 뜬다.")]
        [SerializeField] private float launchUpSpeed = 9f;
        [Tooltip("튕길 때 랜덤 수평 방향으로 주는 속도(m/s). 높을수록 더 멀리 간다.")]
        [SerializeField] private float launchHorizontalSpeed = 6f;

        [Header("착지 감지")]
        [Tooltip("땅으로 인식할 레이어(보통 Ground).")]
        [SerializeField] private LayerMask groundMask;
        [Tooltip("아래로 이 거리 안에 groundMask가 잡히면 착지로 본다(m).")]
        [SerializeField] private float groundCheckDistance = 0.6f;
        [Tooltip("이 시간(초)을 넘기면 착지 못 해도 강제로 그 자리 폭발(안전장치).")]
        [SerializeField] private float maxFlightTime = 3f;

        private PlayerSkillInventory _inventory;
        private ExplosionDamageCaster _caster;
        private EventChannelSO _createChannel;
        private EventChannelSO _playerChannel;
        private Rigidbody _rb;

        public void Init(ModuleOwner owner, ExplosionDamageCaster caster,
                         EventChannelSO createChannel, EventChannelSO playerChannel)
        {
            _inventory = owner != null ? owner.GetModule<PlayerSkillInventory>() : null;
            _caster = caster;
            _createChannel = createChannel;
            _playerChannel = playerChannel;
            _rb = owner != null ? owner.GetComponent<Rigidbody>() : null;
        }

        // origin에서 시작해 N회 포물선 바운스. 비동기(코루틴) — 호출 측은 즉시 반환한다.
        public void RunAfterExplode(Vector3 origin)
        {
            int n = _inventory != null ? _inventory.GetLevel(SkillType.Bounce) : 0;
            if (n <= 0 || _caster == null || _rb == null) return;

            StartCoroutine(BounceRoutine(n));
        }

        private IEnumerator BounceRoutine(int n)
        {
            // 시퀀스 동안 종료 카운트다운 보류(첫 바운스 비행이 길어도 Result가 먼저 안 나게).
            // 도중에 코루틴이 멈춰도(오브젝트 비활성 등) finally에서 반드시 해제한다.
            _playerChannel?.RaiseEvent(PlayerEvents.PlayerBounceActiveEvent.Init(true));
            try
            {
            for (int i = 0; i < n; i++)
            {
                // 1) 위 + 랜덤 수평으로 발사. 미사일은 폭발 시 이미 _stopped 상태라 추진이 속도를 덮어쓰지 않는다.
                Vector2 dir2 = Random.insideUnitCircle.normalized;
                _rb.linearVelocity = new Vector3(dir2.x * launchHorizontalSpeed,
                                                 launchUpSpeed,
                                                 dir2.y * launchHorizontalSpeed);

                // 2) 착지 대기: 하강(velocity.y<=0) 중이고 아래로 땅이 잡히면 착지. 발사 직후 즉시 잡히지
                //    않도록 하강 시작 후부터만 검사. maxFlightTime 초과 시 안전 탈출.
                float elapsed = 0f;
                bool descending = false;
                while (true)
                {
                    yield return new WaitForFixedUpdate();
                    elapsed += Time.fixedDeltaTime;

                    if (_rb.linearVelocity.y <= 0f) descending = true;

                    bool landed = descending &&
                                  Physics.Raycast(_rb.position, Vector3.down,
                                                  groundCheckDistance, groundMask);
                    if (landed || elapsed >= maxFlightTime) break;
                }

                // 3) 착지점 비율 폭발(데미지+VFX) + 상태성 이벤트(종료 타이머 리셋 + 카메라 흔들림).
                Vector3 pos = _rb.position;
                _rb.linearVelocity = Vector3.zero;
                _caster.CastScaledAt(pos, bounceRatio);

                if (_createChannel != null && bounceVfx != null)
                    _createChannel.RaiseEvent(
                        CreateEvents.ShowPoolingVfxEvent.InitData(bounceVfx, pos, Quaternion.identity));

                _playerChannel?.RaiseEvent(
                    PlayerEvents.PlayerExplodedEvent.InitData(pos, gameObject, _caster.ExplosionScale));
            }
            }
            finally
            {
                _playerChannel?.RaiseEvent(PlayerEvents.PlayerBounceActiveEvent.Init(false));
            }
        }
    }
}
