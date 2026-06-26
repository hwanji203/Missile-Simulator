using CombatSystem;
using EventChannelSystem;
using ModuleSystem;
using ObjectPool.Runtime;
using Players.Movement;
using UnityEngine;

namespace Players.Skills
{
    // 미사일에 부착. Grenade 스킬 레벨>0이면 비행 중 일정 간격으로 수류탄을 떨어뜨린다.
    // 간격은 GetLevelValue(Grenade)(초)이며 레벨업하면 즉시 짧아진다.
    // 미사일이 멈추거나 사망하면(StopMovement) 이후 발사하지 않는다 — 이동 모듈과 동일한 정지 경로 공유.
    public class GrenadeLauncherModule : MonoBehaviour, IModule, IStoppableMovement
    {
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO grenadePoolItem;   // GrenadeProjectile 프리팹
        [SerializeField] private EventChannelSO createChannel; // 폭발 VFX 채널
        [Tooltip("수류탄 폭발 위력 = 메인 폭발 × 이 비율")]
        [SerializeField] private float grenadeRatio = 0.5f;
        [Tooltip("미사일 기준 발사 초기 속도(로컬). 보통 아래/뒤로 떨군다.")]
        [SerializeField] private Vector3 launchLocalVelocity = new Vector3(0f, -2f, -3f);

        private PlayerSkillInventory _inventory;
        private ExplosionDamageCaster _mainCaster;
        private float _timer;
        private bool _stopped; // 미사일 정지/사망 후 발사 금지

        public void Initialize(ModuleOwner owner)
        {
            _inventory = owner.GetModule<PlayerSkillInventory>();
            // 미사일 폭발 캐스터(ExplosionSkill 자식). 효과값을 그대로 비율 적용하기 위해 공유한다.
            _mainCaster = owner.GetComponentInChildren<ExplosionDamageCaster>();
            _timer = 0f;
        }

        // 폭발/접촉/사망으로 미사일이 멈출 때 MissileFuse가 IStoppableMovement 일괄 호출.
        public void StopMovement() => _stopped = true;

        private void Update()
        {
            if (_stopped) return;
            if (_inventory == null || _mainCaster == null) return;
            if (_inventory.GetLevel(SkillType.Grenade) <= 0) return;

            float interval = _inventory.GetLevelValue(SkillType.Grenade);
            if (interval <= 0f) return;

            _timer += Time.deltaTime;
            if (_timer < interval) return;
            _timer = 0f;
            Launch();
        }

        private void Launch()
        {
            GrenadeProjectile grenade = poolManager.Pop<GrenadeProjectile>(grenadePoolItem);
            if (grenade == null) return;

            grenade.transform.position = transform.position;
            grenade.transform.rotation = Quaternion.identity;
            Vector3 worldVel = transform.TransformDirection(launchLocalVelocity);
            grenade.Launch(_mainCaster, grenadeRatio, worldVel, poolManager, createChannel);
        }
    }
}
