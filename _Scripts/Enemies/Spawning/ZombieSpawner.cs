using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Enemies.Spawning
{
    // 서브시스템 4 — 좀비 스포너.
    // 레벨 시작 시(게임 매니저가 SpawnAll 호출) 지정한 존 안에 좀비를 한 번에 배치한다.
    //
    // 배치 규칙 = "가중치 비례 공정성(weighted fairness)":
    //  - 매 다트마다 "가중치 대비 가장 덜 배치된"(placed/spawnWeight 최소) 티어를 골라 던진다.
    //    → 실제 스폰 마릿수의 비율이 spawnWeight 비율을 따라간다(소 1 : 중 0.5 : 대 0.25 …).
    //  - 동률일 때는 리시가 큰 티어를 먼저 고른다(정렬상 앞). 시작 시점엔 모두 0이므로
    //    가장 큰 티어부터 자리잡아 공간을 선점한다 → 큰 좀비가 들어갈 자리를 확보한다.
    //  - 어떤 티어가 더 들어갈 공간이 없으면(연속 실패 누적) 그 티어는 "포화"로 빠지고 나머지가 계속 채운다.
    //    → 리시가 큰 티어가 0마리로 밀려나거나, 특정 티어만 나오는 현상을 막는다.
    //
    // 각 다트는 아래로 레이캐스트해 지면 높이를 찾고 NavMesh에 스냅(실패 시 버림)하며,
    // 이미 배치된 좀비들과 리시 원이 겹치면(또는 minSpacing보다 가까우면) 버린다.
    public class ZombieSpawner : MonoBehaviour, ISpawner
    {
        private enum ZoneShape { Box, Circle }

        [Header("스폰 대상")]
        [Tooltip("티어를 주입할 단일 좀비 프리팹.")]
        [SerializeField] private ZombieEnemy prefab;

        [Tooltip("배치할 티어 목록(소/중/대). spawnWeight가 각 티어의 배치 시도 비율을 정한다.")]
        [SerializeField] private List<ZombieTierSO> tiers = new();

        [Header("존 (스포너 transform 중심, 회전 반영)")]
        [SerializeField] private ZoneShape shape = ZoneShape.Box;

        [Tooltip("Box일 때 가로(X)·세로(Z) 크기.")]
        [SerializeField] private Vector2 boxSize = new(40f, 40f);

        [Tooltip("Circle일 때 반경.")]
        [SerializeField] private float circleRadius = 20f;

        [Header("분포")]
        [Tooltip("총 배치 시도(다트) 횟수의 상한. 매 다트는 가중치 대비 가장 덜 찬 티어로 간다. 모든 티어가 포화되면 도중에 멈추므로 크게 잡아도 비용이 작다.")]
        [Min(1)] [SerializeField] private int spawnAttempts = 500;

        [Tooltip("한 티어가 연속으로 이만큼 배치에 실패하면 '포화'로 판단해 더 시도하지 않는다(공간 부족). 작으면 일찍 포기, 크면 끝까지 밀어넣는다.")]
        [Min(1)] [SerializeField] private int saturationStreak = 300;

        [Tooltip("좀비 사이 물리적 최소 간격(바닥값). 리시 비침범이 보통 이보다 크므로, 0.1처럼 작게 두면 리시가 배치를 지배한다.")]
        [Min(0f)] [SerializeField] private float minSpacing = 0.1f;

        [Tooltip("켜면 좀비들의 리시 원이 서로 겹치지 않도록 배치(겹치면 버림). 끄면 minSpacing만 사용.")]
        [SerializeField] private bool preventLeashOverlap = true;

        [Header("지면/네비메시 배치")]
        [Tooltip("아래로 레이캐스트할 지면 레이어(Enemy 레이어는 제외할 것).")]
        [SerializeField] private LayerMask groundMask = ~0;

        [Tooltip("존 평면에서 이만큼 위에서 아래로 레이캐스트 시작.")]
        [SerializeField] private float raycastHeight = 50f;

        [Tooltip("레이캐스트 최대 거리(raycastHeight + 여유 깊이).")]
        [SerializeField] private float raycastDistance = 100f;

        [Tooltip("지면 점에서 가장 가까운 NavMesh로 스냅 허용 거리. 초과하면 그 점은 버린다.")]
        [SerializeField] private float navSampleMaxDistance = 2f;

        [Header("기타")]
        [Tooltip("스폰된 좀비를 모아둘 부모(선택, Hierarchy 정리용).")]
        [SerializeField] private Transform spawnParent;

        [Tooltip("켜두면 단독 테스트용으로 Start에서 자동 스폰. 평소엔 게임 매니저가 SpawnAll 호출.")]
        [SerializeField] private bool spawnOnStart;

        [Tooltip("기즈모 색.")]
        [SerializeField] private Color gizmoColor = new(1f, 0.3f, 0.1f, 0.5f);

        // 이미 배치한 좀비의 (위치, 리시 반경). 새 후보가 이들과 겹치는지 검사한다.
        private readonly List<(Vector3 position, float leashRadius)> _placed = new();

        private void Start()
        {
            if (spawnOnStart)
                SpawnAll();
        }

        // 게임 매니저가 레벨 시작 시 호출. 가중치 비례로 티어를 골라 다트를 던져 배치한다.
        public void SpawnAll()
        {
            if (prefab == null)
            {
                Debug.LogError("[ZombieSpawner] prefab이 비어 있습니다.", this);
                return;
            }
            if (tiers.Count == 0)
            {
                Debug.LogError("[ZombieSpawner] tiers가 비어 있습니다.", this);
                return;
            }

            _placed.Clear();

            // 같은 GO의 공격 제한 관리자(없으면 null = 제한 없음)를 스폰한 좀비 모두에게 주입한다.
            EnemyAttackCoordinator coordinator = GetComponent<EnemyAttackCoordinator>();

            // 유효 티어(null 아님, 가중치>0)만 추림. 동률 시 리시 큰 쪽이 먼저 자리잡도록 내림차순 정렬.
            List<ZombieTierSO> active = new(tiers);
            active.RemoveAll(t => t == null || t.spawnWeight <= 0f);
            active.Sort((a, b) => b.leashRadius.CompareTo(a.leashRadius));
            if (active.Count == 0)
            {
                Debug.LogWarning("[ZombieSpawner] 배치 가능한 티어가 없습니다(가중치 0 또는 전부 null).", this);
                return;
            }

            int n = active.Count;
            int[] placed = new int[n];      // 티어별 실제 배치 수
            int[] failStreak = new int[n];  // 티어별 연속 실패 수(성공 시 0으로 리셋)
            bool[] saturated = new bool[n]; // 더 들어갈 공간이 없다고 판단된 티어
            int saturatedCount = 0;

            int spawned = 0;
            for (int attempt = 0; attempt < spawnAttempts && saturatedCount < n; attempt++)
            {
                // 가중치 대비 가장 덜 배치된(placed/weight 최소) 티어 선택. 동률이면 정렬상 앞(리시 큰) 티어.
                int pick = -1;
                float bestRatio = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    if (saturated[i]) continue;
                    float ratio = placed[i] / active[i].spawnWeight;
                    if (ratio < bestRatio)
                    {
                        bestRatio = ratio;
                        pick = i;
                    }
                }
                if (pick < 0) break;

                ZombieTierSO tier = active[pick];
                if (TryGetGroundPoint(out Vector3 pos) && !TooCloseToExisting(pos, tier.leashRadius))
                {
                    Quaternion rot = Quaternion.Euler(0f, Random.value * 360f, 0f);
                    ZombieEnemy zombie = Instantiate(prefab, pos, rot, spawnParent);
                    zombie.AssignTier(tier);
                    zombie.AttackCoordinator = coordinator;
                    _placed.Add((pos, tier.leashRadius));
                    placed[pick]++;
                    failStreak[pick] = 0;
                    spawned++;
                }
                else if (++failStreak[pick] >= saturationStreak)
                {
                    saturated[pick] = true;
                    saturatedCount++;
                }
            }

            // 티어별 결과 로그(가중치 대비 비율 확인용).
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < n; i++)
                sb.Append($" {active[i].tierName}={placed[i]}(w{active[i].spawnWeight})");
        }

        // 존 안에서 무작위 점을 골라 → 위에서 아래로 레이캐스트 → NavMesh 스냅. 모두 통과해야 true.
        private bool TryGetGroundPoint(out Vector3 result)
        {
            result = default;

            Vector2 local = RandomLocalPointInZone();
            Vector3 planePos = transform.position
                               + transform.right * local.x
                               + transform.forward * local.y;

            Vector3 rayOrigin = planePos + Vector3.up * raycastHeight;
            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                    raycastDistance, groundMask, QueryTriggerInteraction.Ignore))
                return false;

            if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit,
                    navSampleMaxDistance, NavMesh.AllAreas))
                return false;

            result = navHit.position;
            return true;
        }

        // 존(박스/원) 안의 무작위 로컬 점. x = 우(right) 오프셋, y = 전(forward) 오프셋.
        private Vector2 RandomLocalPointInZone()
        {
            if (shape == ZoneShape.Box)
                return new Vector2(
                    Random.Range(-boxSize.x * 0.5f, boxSize.x * 0.5f),
                    Random.Range(-boxSize.y * 0.5f, boxSize.y * 0.5f));

            return Random.insideUnitCircle * circleRadius; // 원반 내부 균등 분포
        }

        // 새 후보가 이미 배치된 좀비와 너무 가까운가? 리시는 바닥 평면의 원이므로 XZ(수평) 거리로 검사한다.
        // 필요 거리 = 리시 비침범(두 반경의 합)과 minSpacing 바닥값 중 큰 값.
        private bool TooCloseToExisting(Vector3 pos, float leash)
        {
            foreach ((Vector3 position, float leashRadius) other in _placed)
            {
                float minDist = preventLeashOverlap
                    ? Mathf.Max(minSpacing, leash + other.leashRadius)
                    : minSpacing;

                Vector3 d = pos - other.position;
                d.y = 0f;
                if (d.sqrMagnitude < minDist * minDist)
                    return true;
            }
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

            if (shape == ZoneShape.Box)
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(boxSize.x, 0.1f, boxSize.y));
            else
                Gizmos.DrawWireSphere(Vector3.zero, circleRadius);

            Gizmos.DrawRay(Vector3.up * raycastHeight, Vector3.down * raycastDistance);
            
            Gizmos.matrix = oldMatrix;
        }
    }
}
