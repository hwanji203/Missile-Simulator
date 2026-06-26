using System;
using EventChannelSystem;
using Events;
using ObjectPool.Runtime;
using UnityEngine;

namespace Players.Items
{
    // 주기적으로 지정 볼륨 내 랜덤 지점(플레이어 반경 제외)에 공중 스탯 아이템을 스폰한다.
    // 플레이어 위치는 PlayerInitEvent로 받아 추적한다(직접 씬 참조 없음).
    public class AerialItemSpawner : MonoBehaviour
    {
        [SerializeField] private PoolManagerSO poolManager;
        [SerializeField] private PoolItemSO statItem;
        [Tooltip("스폰 시 이 중 랜덤 1개의 스탯을 아이템에 부여(아이템 머티리얼 매핑·픽업 지급에 사용).")]
        [SerializeField] private PlayerSkillSO[] statPool;
        [SerializeField] private BoxCollider spawnVolume;        // 스폰 영역
        [SerializeField] private float spawnInterval = 3f;       // 스폰 주기(초)
        [SerializeField] private int maxAlive = 5;               // 동시 생존 상한
        [SerializeField] private float minDistanceFromPlayer = 8f;
        [SerializeField] private int maxResampleTries = 8;       // 반경 밖 재추첨 한도
        [SerializeField] private EventChannelSO playerChannel;   // PlayerInitEvent
        [SerializeField] private EventChannelSO gameChannel;     // GameEndedEvent

        private Transform _player;
        private int _alive;
        private float _timer;
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
        }

        private void HandlePlayerInit(PlayerInitEvent evt)
        {
            if (evt.Player != null) _player = evt.Player.transform;
        }

        private void HandleGameEnded(GameEndedEvent evt) => _stopped = true;

        private void Update()
        {
            if (_stopped || spawnVolume == null || statItem == null || poolManager == null) return;
            if (_alive >= maxAlive) return;

            _timer += Time.deltaTime;
            if (_timer < spawnInterval) return;
            _timer = 0f;

            Spawn();
        }

        private void Spawn()
        {
            Bounds b = spawnVolume.bounds;
            Vector3? playerPos = _player != null ? _player.position : (Vector3?)null;
            Vector3 point = PickSpawnPoint(b, playerPos, minDistanceFromPlayer, maxResampleTries,
                () => new Vector3(
                    UnityEngine.Random.Range(b.min.x, b.max.x),
                    UnityEngine.Random.Range(b.min.y, b.max.y),
                    UnityEngine.Random.Range(b.min.z, b.max.z)));

            StatItem item = poolManager.Pop<StatItem>(statItem);
            if (item == null) return;

            item.transform.position = point;
            if (statPool != null && statPool.Length > 0)
                item.SetStat(statPool[UnityEngine.Random.Range(0, statPool.Length)]);
            item.OnReturnToPool += HandleReturn;
            _alive++;
        }

        private void HandleReturn(StatItem item)
        {
            item.OnReturnToPool -= HandleReturn;
            poolManager.Push(item);
            _alive = Mathf.Max(0, _alive - 1);
        }

        // 볼륨 내 랜덤 지점을 뽑되, 플레이어 반경 내면 maxTries까지 재추첨. 끝까지 실패하면 마지막 후보 반환.
        // sample은 볼륨 내 무작위 점을 돌려주는 함수(테스트에서 주입 가능).
        public static Vector3 PickSpawnPoint(Bounds volume, Vector3? player, float minDist,
                                             int maxTries, Func<Vector3> sample)
        {
            Vector3 point = sample();
            if (player == null) return point;

            float sqrMin = minDist * minDist;
            for (int i = 0; i < maxTries && (point - player.Value).sqrMagnitude < sqrMin; i++)
                point = sample();

            return point;
        }
    }
}
