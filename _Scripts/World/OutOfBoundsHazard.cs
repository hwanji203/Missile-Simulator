using System;
using CombatSystem;
using EventChannelSystem;
using Events;
using UnityEngine;
using UnityEngine.Rendering;

namespace World
{
    // 플레이어가 맵 중심에서 일정 반경 밖으로 나가면 PP 볼륨을 거리 비례로 어둡게 하고,
    // 바깥에 머무는 동안 고정 DPS로 데미지를 준다.
    // 플레이어 위치/체력은 PlayerInitEvent로 받아 추적한다(직접 씬 참조 없음, AerialItemSpawner와 동일 패턴).
    public class OutOfBoundsHazard : MonoBehaviour
    {
        [Header("경계")]
        [Tooltip("맵 중심. 비우면 자기 자신 Transform을 사용한다.")]
        [SerializeField] private Transform centerTransform;
        [Tooltip("이 거리까지는 안전")]
        [SerializeField] private float radius = 50f;
        [Tooltip("radius ~ radius+falloffRange 구간에서 어두움이 0→최대로 보간된다.")]
        [SerializeField] private float falloffRange = 15f;
        [Tooltip("높이를 무시하고 XZ 평면 거리만 사용(원형 평면 맵 가정).")]
        [SerializeField] private bool useHorizontalDistance = true;

        [Header("화면 어두워짐(PP)")]
        [Tooltip("어둡게 만들 Post-Processing Volume. weight를 거리 비례로 구동한다(프로파일은 Unity에서 저작).")]
        [SerializeField] private Volume darkenVolume;
        [Range(0f, 1f)]
        [Tooltip("바깥 끝(radius+falloffRange 이상)에서의 최대 weight")]
        [SerializeField] private float maxWeight = 1f;

        [Header("데미지")]
        [Tooltip("바깥(radius 초과)에 있는 동안 초당 데미지")]
        [SerializeField] private float damagePerSecond = 10f;
        [Tooltip("데미지를 이 간격으로 누적 적용(HUD가 매 프레임 갱신되지 않게)")]
        [SerializeField] private float damageTickInterval = 0.5f;

        [Header("이벤트")]
        [SerializeField] private EventChannelSO playerChannel; // PlayerInitEvent
        [SerializeField] private EventChannelSO gameChannel;   // GameEndedEvent

        private Transform _player;
        private HealthModule _health;
        private float _damageTimer;
        private bool _stopped;

        private void OnEnable()
        {
            playerChannel?.AddListener<PlayerInitEvent>(HandlePlayerInit);
            gameChannel?.AddListener<GameEndedEvent>(HandleGameEnded);
        }

        private void OnDisable()
        {
            playerChannel?.RemoveListener<PlayerInitEvent>(HandlePlayerInit);
            gameChannel?.RemoveListener<GameEndedEvent>(HandleGameEnded);
            SetWeight(0f);
        }

        private void HandlePlayerInit(PlayerInitEvent evt)
        {
            _player = evt.Player != null ? evt.Player.transform : null;
            _health = evt.Health;
        }

        private void HandleGameEnded(GameEndedEvent evt) => _stopped = true;

        private void Update()
        {
            // 정지/플레이어 없음/사망 → 어두움 해제하고 대기.
            if (_stopped || _player == null || darkenVolume == null
                || _health == null || _health.CurrentHealth <= 0f)
            {
                SetWeight(0f);
                _damageTimer = 0f;
                return;
            }

            Vector3 center = centerTransform != null ? centerTransform.position : transform.position;
            float dist = Distance(center, _player.position);

            float t = falloffRange > 0f
                ? Mathf.Clamp01((dist - radius) / falloffRange)
                : (dist > radius ? 1f : 0f);
            SetWeight(t * maxWeight);

            if (dist > radius)
            {
                _damageTimer += Time.deltaTime;
                if (_damageTimer >= damageTickInterval)
                {
                    _health.ApplyDamage(damagePerSecond * _damageTimer);
                    _damageTimer = 0f;
                }
            }
            else
            {
                _damageTimer = 0f;
            }
        }

        private float Distance(Vector3 a, Vector3 b)
        {
            if (useHorizontalDistance)
            {
                a.y = 0f;
                b.y = 0f;
            }
            return Vector3.Distance(a, b);
        }

        private void SetWeight(float w)
        {
            if (darkenVolume != null) darkenVolume.weight = w;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            
            Gizmos.DrawWireSphere(centerTransform.position, radius);
            Gizmos.DrawWireSphere(centerTransform.position, radius + falloffRange);
        }
    }
}
