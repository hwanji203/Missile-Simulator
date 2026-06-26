using System.Collections.Generic;
using CombatSystem;
using EventChannelSystem;
using Events;
using UnityEngine;

namespace Enemies
{
    // 씬에 하나 배치.
    // 로켓이 멈추면 ExplosionDamageCaster.MaxRadius 를 자동으로 읽어 매 프레임 범위 내외를 갱신한다.
    //  - 범위 안에 들어온 에너미 → 아웃라인 ON
    //  - 범위 밖으로 나간 에너미 → 아웃라인 OFF
    //  - 폭발(PlayerExplodedEvent) → 전체 아웃라인 OFF + 추적 종료
    //
    // playerChannel = 플레이어 이벤트 채널 SO
    // enemyLayer    = 에너미 콜라이더 레이어
    public class ExplosionRangeOutliner : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private LayerMask enemyLayer;

        public int RemainingTargets => _active.Count; 
        
        private readonly List<EnemyOutline> _active = new();
        private readonly HashSet<EnemyOutline> _inRangeSet = new();

        // 매 프레임 OverlapSphere 할당/GetComponent 탐색을 피하기 위한 캐시.
        private readonly Collider[] _hitBuffer = new Collider[128];
        private readonly Dictionary<Collider, EnemyOutline> _outlineCache = new();

        private bool _tracking;
        private Vector3 _rocketPos;
        private ExplosionDamageCaster _caster;

        private void OnEnable()
        {
            if (playerChannel == null) return;
            playerChannel.AddListener<PlayerStoppedEvent>(OnStopped);
            playerChannel.AddListener<PlayerExplodedEvent>(OnExploded);
        }

        private void OnDisable()
        {
            if (playerChannel == null) return;
            playerChannel.RemoveListener<PlayerStoppedEvent>(OnStopped);
            playerChannel.RemoveListener<PlayerExplodedEvent>(OnExploded);
        }

        private void OnStopped(PlayerStoppedEvent data)
        {
            if (_tracking) return;
            if (data.Player == null) return;
            _rocketPos = data.Player.transform.position;
            _caster = data.Player.GetComponentInChildren<ExplosionDamageCaster>();
            _tracking = true;
        }

        private void Update()
        {
            if (!_tracking) return;
            if (_caster == null)
            {
                // 로켓이 파괴됐지만 이벤트를 못 받은 경우 방어적으로 정리
                HideAll();
                _tracking = false;
                return;
            }
            float radius = _caster.MaxRadius;
            int hitCount = Physics.OverlapSphereNonAlloc(_rocketPos, radius, _hitBuffer, enemyLayer);

            // 이번 프레임에 범위 안에 있는 아웃라인 수집
            _inRangeSet.Clear();
            for (int i = 0; i < hitCount; i++)
            {
                var outline = GetOutline(_hitBuffer[i]);
                if (outline != null) _inRangeSet.Add(outline);
            }

            // 새로 들어온 것 켜기
            foreach (var o in _inRangeSet)
            {
                if (_active.Contains(o)) continue;
                o.Show();
                _active.Add(o);
            }

            // 범위 밖으로 나간 것 끄기
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                if (_inRangeSet.Contains(_active[i])) continue;
                _active[i]?.Hide();
                _active.RemoveAt(i);
            }
        }

        // 콜라이더→아웃라인 매핑을 캐시한다(못 찾은 콜라이더도 null로 캐시해 반복 탐색 방지).
        private EnemyOutline GetOutline(Collider col)
        {
            if (_outlineCache.TryGetValue(col, out EnemyOutline outline)) return outline;

            outline = col.GetComponent<EnemyOutline>();
            _outlineCache.Add(col, outline);
            return outline;
        }

        private void OnExploded(PlayerExplodedEvent _)
        {
            _tracking = false;
            HideAll();
        }

        private void HideAll()
        {
            foreach (var o in _active) o?.Hide();
            _active.Clear();
        }
    }
}
