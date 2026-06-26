using System;
using System.Collections;
using System.Collections.Generic;
using CombatSystem;
using ModuleSystem;
using UnityEngine;
using UnityEngine.AI;

namespace Players.Skills
{
    // 폭발 직전 훅. EnemySuck 레벨>0이면 고정 시간 동안 범위 내 적을 폭심으로 끌어당긴 뒤
    // 실제 폭발을 진행시킨다(레벨 = 끌어당기는 강도만, 지속 시간·범위는 고정).
    public class SuckEffect : MonoBehaviour
    {
        [SerializeField] private LayerMask whatIsEnemy;
        [Tooltip("흡입 지속 시간(초). 이 시간만큼 메인 폭발이 지연된다.")]
        [SerializeField] private float suckDuration = 0.5f;
        [SerializeField] private float suckRadius = 8f;

        private PlayerSkillInventory _inventory;

        // ExplosionSkill이 폭발 캐스터 초기화 시점에 오너를 넘겨 인벤토리를 잡아준다.
        public void Init(ModuleOwner owner)
        {
            _inventory = owner != null ? owner.GetModule<PlayerSkillInventory>() : null;
        }

        // 레벨 0이면 즉시 onComplete(무지연). 레벨>0이면 흡입 코루틴 후 onComplete.
        public void RunBeforeExplode(Vector3 center, Action onComplete)
        {
            int level = _inventory != null ? _inventory.GetLevel(SkillType.EnemySuck) : 0;
            if (level <= 0)
            {
                onComplete?.Invoke();
                return;
            }
            StartCoroutine(SuckRoutine(center, onComplete));
        }

        private IEnumerator SuckRoutine(Vector3 center, Action onComplete)
        {
            // 적은 키네마틱 Rigidbody + NavMeshAgent라 물리력(AddForce)이 무시된다 →
            // NavMeshAgent.Move로 폭심 방향으로 지면 위를 끌어당긴다(navmesh 위로 보장).
            float pullSpeed = _inventory.GetLevelValue(SkillType.EnemySuck); // 초당 끌기 속도(units/s)
            HashSet<NavMeshAgent> agents = new HashSet<NavMeshAgent>();
            float elapsed = 0f;
            while (elapsed < suckDuration)
            {
                Collider[] hits = Physics.OverlapSphere(center, suckRadius, whatIsEnemy);
                agents.Clear();
                foreach (Collider col in hits)
                {
                    // 한 적에 콜라이더가 여러 개여도 에이전트 단위로 1회만 끈다(중복 견인 방지).
                    NavMeshAgent agent = col.GetComponentInParent<NavMeshAgent>();
                    if (agent == null || !agent.enabled || !agent.isOnNavMesh) continue;
                    if (!agents.Add(agent)) continue;

                    Vector3 toCenter = center - agent.transform.position;
                    toCenter.y = 0f; // 지면 끌기 — 수직 성분 무시
                    float dist = toCenter.magnitude;
                    if (dist < 0.001f) continue;

                    // 폭심을 지나치지 않도록 이번 프레임 이동량을 남은 거리로 클램프.
                    float step = Mathf.Min(pullSpeed * Time.deltaTime, dist);
                    agent.Move(toCenter / dist * step);
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            onComplete?.Invoke();
        }
    }
}
