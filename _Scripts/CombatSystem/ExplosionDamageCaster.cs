using System.Collections;
using System.Collections.Generic;
using ModuleSystem;
using UnityEngine;

namespace CombatSystem
{
    // 한 명만 때리는 RayDamageCaster와 달리, OverlapSphere로 반경 안의
    // 모든 IDamageable에게 한꺼번에 데미지를 주는 범위(AoE) 캐스터.
    //
    // 여러 "단계(stage)"를 배열로 정의할 수 있다. 각 단계는 (지연 / 반경 / 데미지)를 가지며,
    // CastDamage가 호출되면 코루틴이 각 단계의 지연만큼 기다린 뒤 그 반경·데미지로 피해를 준다.
    // 예: 0초에 작고 강한 폭심 타격 → 0.3초 뒤 넓고 약한 충격파, 처럼 시간차 다단 폭발을 만든다.
    public class ExplosionDamageCaster : AbstractDamageCaster
    {
        [System.Serializable]
        public struct DamageStage
        {
            [Tooltip("CastDamage 호출 시점으로부터 이 단계가 발동되기까지의 지연(초)")]
            public float delay;

            [Tooltip("이 단계의 피해 반경(Rad)")]
            public float radius;

            [Tooltip("이 단계의 폭심 기준 기본 피해량(거리 감쇠가 적용된다)")]
            public float damage;
        }

        // SkillDataSO가 아니라 캐스터에서 직접 조절한다(예시의 RayDamageCaster와 동일한 방식).
        [Header("다단 폭발 설정")]
        [Tooltip("시간차로 발동되는 폭발 단계들. delay 오름차순으로 정렬해 순서대로 적용된다.")]
        [SerializeField]
        private DamageStage[] stages =
        {
            new() { delay = 0f, radius = 5f, damage = 5f },
        };

        // 거리 반비례 감쇠. 폭심에서 멀수록 데미지가 준다.
        // 데미지 = stage.damage * Clamp01(1 - dist/stage.radius)^falloffExponent
        // exponent 1 = 선형, >1 = 중심 쪽으로 더 몰림, 0 = 감쇠 없음(균일).
        [Header("공통 설정")]
        [SerializeField] private float falloffExponent = 1f;
        [SerializeField] private bool isDebugMode;

        // 한 단계에서 같은 대상을 (자식 콜라이더가 여러 개여도) 한 번만 때리기 위한 집합.
        // 단계가 바뀌면 비우므로, 다른 단계끼리는 같은 대상을 다시 때릴 수 있다(다단 히트).
        private readonly HashSet<IDamageable> _hitThisStage = new();

        // 진행 중인 다단 폭발 코루틴(새 폭발이 들어오면 이전 것을 끊는다).
        private Coroutine _castRoutine;

        // 인런 스탯 보너스 공급원(플레이어 오너에만 존재, 적 캐스터면 null).
        private ISkillBonusProvider _bonusProvider;

        // 기즈모/디버그용 마지막 단계 중심·반경.
        private float _lastRadius;
        private Vector3 _lastCenter;

        // 등록된 모든 단계 중 가장 큰 반경. ExplosionRangeOutliner 등이 실시간으로 읽는다.
        public float MaxRadius
        {
            get
            {
                if (stages == null || stages.Length == 0) return 0f;
                float max = 0f;
                foreach (var s in stages) if (s.radius > max) max = s.radius;
                // 미리보기 링이 실제 폭발 반경과 일치하도록 메타 base + 인런 보너스를 더한다.
                return max + AddedRange + (_bonusProvider?.GetBonus(SkillType.ExplosionRange) ?? 0f);
            }
        }

        // 폭발 반경 배수(연출용). base 반경(stages 최대 radius) 대비 effective 반경(MaxRadius) 비율.
        // 반경 보너스가 없으면 1.0, 반경이 2배가 되면 2.0. 파티클 크기·카메라 후퇴가 공유하는 단일 소스.
        public float ExplosionScale
        {
            get
            {
                float baseMax = 0f;
                if (stages != null)
                    foreach (var s in stages) if (s.radius > baseMax) baseMax = s.radius;
                return baseMax <= 0f ? 1f : MaxRadius / baseMax;
            }
        }

        // 캐스터 주입 시 오너의 보너스 공급원을 함께 잡아둔다(없으면 null → 보너스 0).
        public override void InitCaster(ModuleOwner owner)
        {
            base.InitCaster(owner);
            _bonusProvider = owner != null ? owner.GetModule<ISkillBonusProvider>() : null;
        }

        // direction/skillData는 사용하지 않는다(구 범위라 방향 무관, 반경·데미지는 stages 필드).
        // 시그니처 호환을 위해 유지. 반환값은 "다단 폭발을 시작했는가"이다(실제 피격은 지연 후 발생).
        public override bool CastDamage(Vector3 position, Vector3 direction, SkillDataSO skillData)
        {
            if (stages == null || stages.Length == 0)
            {
                if (isDebugMode)
                    Debug.LogWarning($"{name}: ExplosionDamageCaster에 정의된 단계(stage)가 없습니다.", this);
                return false;
            }

            if (_castRoutine != null)
                StopCoroutine(_castRoutine);

            _castRoutine = StartCoroutine(CastStagesRoutine(position));
            return true;
        }

        // 단계들을 delay 오름차순으로 정렬해, 누적 시간 기준으로 차례차례 적용한다.
        private IEnumerator CastStagesRoutine(Vector3 center)
        {
            // 원본 배열을 건드리지 않도록 복사 후 정렬.
            DamageStage[] ordered = (DamageStage[])stages.Clone();
            System.Array.Sort(ordered, (a, b) => a.delay.CompareTo(b.delay));

            float elapsed = 0f;

            for (var i = 0; i < ordered.Length; i++)
            {
                var stage = ordered[i];
                
                // base(메타: AddedDamage/AddedRange) + bonus(인런 GetBonus). 폭발 시점에 읽어 항상 최신값.
                stage.damage += AddedDamage + (_bonusProvider?.GetBonus(SkillType.ExplosionDamage) ?? 0f);
                stage.radius += AddedRange  + (_bonusProvider?.GetBonus(SkillType.ExplosionRange)  ?? 0f);
                
                float wait = stage.delay - elapsed;
                if (wait > 0f)
                {
                    yield return new WaitForSeconds(wait);
                    elapsed += wait;
                }

                ApplyStage(center, stage);
            }

            _castRoutine = null;
        }

        // 추가 폭발(Grenade 착탄·Bounce 점프) 전용: 메인 폭발의 효과값(메타 AddedDamage/AddedRange +
        // 스탯 보너스)에 ratio를 곱해 pos에 단발로 즉시 적용한다. 다단 딜레이는 무시(즉시 1회).
        // 상태성 이벤트는 발행하지 않는다(데미지만) — VFX는 호출자가 ShowPoolingVfxEvent로 따로 낸다.
        public void CastScaledAt(Vector3 pos, float ratio)
        {
            if (stages == null || stages.Length == 0 || ratio <= 0f) return;

            float dmgBonus = _bonusProvider?.GetBonus(SkillType.ExplosionDamage) ?? 0f;
            float radBonus = _bonusProvider?.GetBonus(SkillType.ExplosionRange) ?? 0f;

            foreach (DamageStage s in stages)
            {
                // CastStagesRoutine과 동일한 effective 값(base+bonus)에 ratio를 곱한다.
                DamageStage scaled = new DamageStage
                {
                    delay = 0f,
                    damage = (s.damage + AddedDamage + dmgBonus) * ratio,
                    radius = (s.radius + AddedRange + radBonus) * ratio,
                };
                ApplyStage(pos, scaled);
            }
        }

        // 한 단계의 실제 피해 적용: center를 중심으로 stage.radius 안의 모든 대상에게 감쇠 데미지.
        private void ApplyStage(Vector3 center, DamageStage stage)
        {
            _lastCenter = center;
            _lastRadius = stage.radius;
            _hitThisStage.Clear();

            Collider[] hits = Physics.OverlapSphere(center, stage.radius, whatIsEnemy);

            foreach (Collider col in hits)
            {
                // 콜라이더가 자식(FBX 메쉬 등)에 있어도 부모 쪽 IDamageable을 찾는다.
                IDamageable damageable = col.GetComponentInParent<IDamageable>();
                if (damageable == null || !_hitThisStage.Add(damageable))
                    continue;

                Vector3 hitPoint = col.ClosestPoint(center);
                Vector3 hitNormal = (hitPoint - center).sqrMagnitude > 0.0001f
                    ? (hitPoint - center).normalized
                    : Vector3.up;

                // 감쇠는 폭심 → 대상 루트 중심 거리로 계산(큰 콜라이더라도 안정적).
                Vector3 targetCenter = damageable is Component comp ? comp.transform.position : hitPoint;
                float normalizedDist = stage.radius > 0f
                    ? Vector3.Distance(center, targetCenter) / stage.radius
                    : 0f;
                float falloff = Mathf.Pow(Mathf.Clamp01(1f - normalizedDist), falloffExponent);

                LastHitPosition = hitPoint;
                LastHitNormal = hitNormal;
                LastHitCritical = false;

                damageable.ApplyDamage(new DamageData
                {
                    DamageAmount = stage.damage * falloff,
                    Attacker = CasterOwner,
                    HitPoint = hitPoint,
                    HitNormal = hitNormal,
                    IsCritical = false,
                });
            }
        }

        private void OnDisable()
        {
            // 비활성/파괴 시 진행 중인 다단 폭발을 정리한다(Unity가 코루틴을 멈추지만 참조도 비운다).
            _castRoutine = null;
        }

        // 선택 시 폭발 반경을 그린다. 에디트 모드 = 각 단계 radius를 캐스터 위치에,
        // 플레이 모드 = 마지막으로 실제 발동된 단계의 중심/반경.
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            if (Application.isPlaying)
            {
                Gizmos.DrawWireSphere(_lastCenter, _lastRadius);
                return;
            }

            if (stages == null) return;
            foreach (DamageStage stage in stages)
                Gizmos.DrawWireSphere(transform.position, stage.radius);
        }
    }
}
