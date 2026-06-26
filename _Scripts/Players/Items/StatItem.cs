using System;
using EventChannelSystem;
using Events;
using ObjectPool.Runtime;
using UnityEngine;

namespace Players.Items
{
    // 스탯 ↔ 머티리얼 매핑 한 쌍.
    [Serializable]
    public struct StatMaterial
    {
        public PlayerSkillSO stat;
        public Material material;
    }

    // 공중에 떠 있는 스탯 아이템. 미사일(targetMask)이 트리거에 닿으면 StatItemPickedUpEvent를 발행하고 풀로 반납.
    // 어떤 스탯이 오를지는 모른다 — UpgradeService.AcquireRandom(Stat)이 결정.
    [RequireComponent(typeof(Collider))]
    public class StatItem : AbstractMonoPoolable
    {
        [SerializeField] private LayerMask targetMask;         // 미사일(로켓) 레이어
        [SerializeField] private EventChannelSO playerChannel; // StatItemPickedUpEvent 발행
        [SerializeField] private EventChannelSO gameChannel;   // GameEndedEvent 반납

        [Header("픽업 이펙트(선택)")]
        [SerializeField] private EventChannelSO createChannel; // 풀링 VFX 채널
        [SerializeField] private PoolItemSO pickupVfx;

        [Header("연출")]
        [Tooltip("초당 회전 각도(deg). 0이면 회전 안 함.")]
        [SerializeField] private float spinSpeed = 90f;

        [Header("외형(스탯별 머티리얼)")]
        [Tooltip("머티리얼을 교체할 렌더러. 비워두면 교체 안 함.")]
        [SerializeField] private Renderer targetRenderer;
        [Tooltip("스탯 ↔ 머티리얼 매핑. 스폰 시 지정된 스탯에 해당하는 머티리얼을 적용.")]
        [SerializeField] private StatMaterial[] statMaterials;

        public event Action<StatItem> OnReturnToPool;

        private bool _picked;
        private PlayerSkillSO _stat; // 스폰 시 정해진 이 아이템의 스탯(픽업 시 확정 지급).

        // 풀에서 꺼낼 때마다 초기화 + 게임 종료 반납 구독(ThrownProjectile 패턴).
        public override void ResetItem()
        {
            _picked = false;
            _stat = null; // SetStat 호출 전까지는 미지정(스탯 누수 방지).
            gameChannel?.AddListener<GameEndedEvent>(ReturnOnGameEnd);
        }

        // 스폰너가 Pop 직후 호출. 이 아이템이 줄 스탯을 정하고 매핑된 머티리얼로 외형 교체.
        public void SetStat(PlayerSkillSO stat)
        {
            _stat = stat;
            if (targetRenderer == null || statMaterials == null || stat == null) return;

            foreach (StatMaterial pair in statMaterials)
            {
                if (pair.stat == stat)
                {
                    if (pair.material != null) targetRenderer.sharedMaterial = pair.material;
                    return;
                }
            }
        }

        private void Update()
        {
            if (spinSpeed != 0f)
                transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_picked) return;
            if ((targetMask.value & (1 << other.gameObject.layer)) == 0) return;

            _picked = true;

            if (createChannel != null && pickupVfx != null)
                createChannel.RaiseEvent(
                    CreateEvents.ShowPoolingVfxEvent.InitData(pickupVfx, transform.position, Quaternion.identity));

            playerChannel?.RaiseEvent(
                PlayerEvents.StatItemPickedUpEvent.InitData(transform.position, _stat));

            Return();
        }

        private void ReturnOnGameEnd(GameEndedEvent _) => Return();

        private void Return()
        {
            gameChannel?.RemoveListener<GameEndedEvent>(ReturnOnGameEnd);
            OnReturnToPool?.Invoke(this);
        }
    }
}
