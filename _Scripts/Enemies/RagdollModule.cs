using CombatSystem;
using ModuleSystem;
using UnityEngine;

namespace Enemies
{
    // 죽을 때: 애니메이션 비주얼 → 래그돌 비주얼로 교체하고 폭발 충격을 가하는 런타임 모듈.
    // 두 비주얼(liveVisual/ragdollVisual)은 같은 모델의 복제본이라 본 계층·순서가 동일하다.
    // 죽는 순간 애니 본의 "현재 포즈"를 래그돌 본에 그대로 복사("포즈 박아넣기")한 뒤
    // 물리를 깨워서 끊김 없이(=같은 자세 그대로) 늘어지게 한다.
    public class RagdollModule : MonoBehaviour, IModule
    {
        [Header("비주얼 (둘은 같은 모델의 복제본이어야 함)")]
        [Tooltip("애니메이션용 Body. 평소 보이는 쪽. 죽으면 끈다.")]
        [SerializeField] private GameObject liveVisual;
        [Tooltip("래그돌용 Body 복제본(Ragdoll Wizard 리그 있음, SlopeAligner 제거). 평소 꺼둔다.")]
        [SerializeField] private GameObject ragdollVisual;

        [Header("죽을 때 시체를 띄우는 힘")]
        [Tooltip("데미지 1당 발사 속도(m/s). 거리 반비례 데미지라 가까울수록 빨리 날아간다.")]
        [SerializeField] private float launchSpeedPerDamage = 0.3f;
        [Tooltip("발사 속도 상한(m/s). 데미지가 커도 시체가 로켓처럼 날아가지 않게.")]
        [SerializeField] private float maxLaunchSpeed = 12f;
        [Tooltip("발사 방향에 더하는 위쪽 가중. 0=수평, 클수록 위로 솟구친다.")]
        [SerializeField] private float upwardBias = 0.4f;
        [Tooltip("충격점 부근 본에만 추가로 주는 회전감 비율. 0=균일 발사만(팔 꺾임 최소).")]
        [SerializeField] private float localSpinRatio = 0.25f;

        [Tooltip("래그돌이 켜질 때 함께 끌 컴포넌트(이동 AI, NavMeshAgent, NavAgentRenderer 등).")]
        [SerializeField] private Behaviour[] disableOnRagdoll;

        private Animator _animator;
        private Rigidbody _mainRigidbody;
        private Collider _mainCollider;

        // 두 비주얼의 본 Transform을 인덱스로 1:1 대응시켜 캐시(복제본이라 순서 동일).
        private Transform[] _liveBones;    // 포즈를 "읽는" 쪽(애니 비주얼)
        private Transform[] _ragdollBones; // 포즈를 "쓰는" 쪽(래그돌 비주얼)

        // 래그돌 비주얼 본에 달린 물리 컴포넌트(평소 재워두고 죽을 때 깨운다).
        private Rigidbody[] _boneBodies;
        private Collider[] _boneColliders;

        public bool IsRagdolled { get; private set; }

        // 모듈 시스템 경로: ModuleOwner를 앵커로 그대로 넘긴다.
        public void Initialize(ModuleOwner owner) => Initialize((Component)owner);

        // ModuleOwner 없이 단편적으로 쓰는 경로(예: 풀링 투사체). 앵커는 GetComponent 기준점일 뿐이다.
        public void Initialize(Component anchor)
        {
            _animator = anchor.GetComponentInChildren<Animator>();
            _mainRigidbody = anchor.GetComponent<Rigidbody>();
            _mainCollider = anchor.GetComponent<Collider>();

            Debug.Assert(liveVisual != null && ragdollVisual != null,
                "RagdollModule: liveVisual / ragdollVisual을 인스펙터에 연결해야 합니다.");

            // 두 비주얼은 복제본 → 자식 Transform 순서가 동일하다. 인덱스로 짝짓는다.
            // (복제본이 아니면 순서가 어긋나 포즈가 깨진다. 반드시 Duplicate로 만들 것.)
            _liveBones = liveVisual.GetComponentsInChildren<Transform>(true);
            _ragdollBones = ragdollVisual.GetComponentsInChildren<Transform>(true);
            Debug.Assert(_liveBones.Length == _ragdollBones.Length,
                $"RagdollModule: 본 개수가 다릅니다(live {_liveBones.Length} vs ragdoll {_ragdollBones.Length}). " +
                "ragdollVisual은 liveVisual의 Duplicate여야 합니다.");

            // 래그돌 비주얼에서 Rigidbody가 달린 본만 추려 물리 제어용으로 캐시한다.
            _boneBodies = ragdollVisual.GetComponentsInChildren<Rigidbody>(true);
            _boneColliders = new Collider[_boneBodies.Length];
            for (int i = 0; i < _boneBodies.Length; i++)
                _boneColliders[i] = _boneBodies[i].GetComponent<Collider>();

            SetRagdollAsleep();
            ragdollVisual.SetActive(false); // 평소엔 래그돌 비주얼을 숨긴다.
        }

        // 평소(살아있을 때): 래그돌 본은 키네매틱 + 콜라이더 꺼서 물리가 자게 둔다.
        private void SetRagdollAsleep()
        {
            for (int i = 0; i < _boneBodies.Length; i++)
            {
                if (_boneBodies[i] != null) _boneBodies[i].isKinematic = true;
                if (_boneColliders[i] != null) _boneColliders[i].enabled = false;
            }
        }

        // 죽음 처리에서 호출. 마지막으로 맞은 데미지의 방향/세기로 시체를 띄운다.
        public void EnableRagdoll(DamageData lastDamage)
        {
            Vector3 dir = lastDamage.HitNormal.sqrMagnitude > 0.0001f
                ? lastDamage.HitNormal.normalized
                : Vector3.up;
            EnableRagdoll(dir, lastDamage.DamageAmount, lastDamage.HitPoint);
        }

        // direction = 폭심→시체 방향(정규화), damageAmount = 거리 반비례 데미지(가까울수록 큼).
        public void EnableRagdoll(Vector3 direction, float damageAmount, Vector3 hitPoint)
        {
            if (IsRagdolled) return;
            IsRagdolled = true;

            // 애니/AI/루트 물리를 멈춘다(NavAgentRenderer까지 꺼야 시체가 안 끌려다닌다).
            if (_animator != null) _animator.enabled = false;
            if (_mainCollider != null) _mainCollider.enabled = false;
            if (_mainRigidbody != null) _mainRigidbody.isKinematic = true;
            if (disableOnRagdoll != null)
                foreach (Behaviour b in disableOnRagdoll)
                    if (b != null) b.enabled = false;

            // 래그돌 비주얼을 켜고, 애니 본의 현재 포즈를 그대로 박아넣는다.
            ragdollVisual.SetActive(true);
            CopyPose();

            // 같은 포즈의 래그돌이 준비됐으니 애니 비주얼은 끈다(자연스러운 바통터치).
            liveVisual.SetActive(false);

            // 위로 살짝 띄운 발사 방향 + 데미지 비례 속도(과하지 않게 클램프).
            Vector3 launchDir = (direction + Vector3.up * upwardBias).normalized;
            float speed = Mathf.Min(damageAmount * launchSpeedPerDamage, maxLaunchSpeed);
            Vector3 launchVel = launchDir * speed;

            // 핵심: 본을 깨운다(콜라이더 on + 키네매틱 해제) + 전체에 같은 속도 → 몸 전체가 같이 솟구침.
            for (int i = 0; i < _boneBodies.Length; i++)
            {
                if (_boneColliders[i] != null) _boneColliders[i].enabled = true;
                if (_boneBodies[i] == null) continue;
                _boneBodies[i].isKinematic = false;
                _boneBodies[i].useGravity = true;
                _boneBodies[i].linearVelocity = launchVel;
            }

            // 충격점 근처 본에만 약한 추가 속도 → 약간의 회전감(과하면 팔 꺾임).
            if (localSpinRatio > 0f)
            {
                Rigidbody nearest = FindNearestBone(hitPoint);
                if (nearest != null)
                    nearest.AddForceAtPosition(launchDir * (speed * localSpinRatio),
                        hitPoint, ForceMode.VelocityChange);
            }
        }

        // 애니 본의 현재 world pos/rot를 래그돌 본에 그대로 복사한다(포즈 일치).
        // GetComponentsInChildren는 부모→자식 순서라, world로 덮어써도 자식이 부모를 덮어 안전하다.
        private void CopyPose()
        {
            for (int i = 0; i < _liveBones.Length; i++)
            {
                _ragdollBones[i].position = _liveBones[i].position;
                _ragdollBones[i].rotation = _liveBones[i].rotation;
            }
        }

        // 풀 재사용 등으로 시체를 다시 살아있는 상태로 되돌린다.
        // EnableRagdoll이 바꾼 것(비주얼 교체/본 깨움/메인 비활성/애니메이터/AI)을 모두 원복한다.
        public void ResetRagdoll()
        {
            IsRagdolled = false;

            SetRagdollAsleep();             // 래그돌 본 다시 재움
            ragdollVisual.SetActive(false); // 래그돌 비주얼 숨김
            liveVisual.SetActive(true);     // 애니 비주얼 복귀

            if (_mainRigidbody != null) { _mainRigidbody.isKinematic = false; _mainRigidbody.useGravity = true; }
            if (_mainCollider != null) _mainCollider.enabled = true;
            if (_animator != null) _animator.enabled = true;

            if (disableOnRagdoll != null)
                foreach (Behaviour b in disableOnRagdoll)
                    if (b != null) b.enabled = true;
        }

        private Rigidbody FindNearestBone(Vector3 point)
        {
            Rigidbody nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < _boneBodies.Length; i++)
            {
                if (_boneBodies[i] == null) continue;
                float d = (_boneBodies[i].worldCenterOfMass - point).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    nearest = _boneBodies[i];
                }
            }
            return nearest;
        }
    }
}
