using System;
using System.Collections.Generic;
using Enemies;
using EventChannelSystem;
using Events;
using UnityEngine;

namespace Core
{
    // 런 동안 좀비 처치 재화를 누적한다. GameEndedEvent 수신 후 집계를 멈춘다.
    public class ScoreTracker : MonoBehaviour
    {
        [SerializeField] private EventChannelSO enemyChannel;
        [SerializeField] private EventChannelSO gameChannel;

        public int TotalCurrency { get; private set; }

        private readonly Dictionary<ZombieTierSO, int> _killsPerTier = new();
        private bool _tracking = true;

        private void Awake()
        {
            enemyChannel?.AddListener<ZombieKilledEvent>(OnZombieKilled);
            gameChannel?.AddListener<GameEndedEvent>(GameEndHandler);
        }

        private void OnDestroy()
        {
            enemyChannel?.RemoveListener<ZombieKilledEvent>(OnZombieKilled);
            gameChannel?.RemoveListener<GameEndedEvent>(GameEndHandler);
        }

        private void GameEndHandler(GameEndedEvent _) => _tracking = false;

        private void OnZombieKilled(ZombieKilledEvent evt)
        {
            if (!_tracking || evt.Tier == null) return;

            if (!_killsPerTier.ContainsKey(evt.Tier))
                _killsPerTier[evt.Tier] = 0;

            _killsPerTier[evt.Tier]++;
            TotalCurrency += evt.Tier.reward;
        }

        public List<TierKillResult> GetSnapshot()
        {
            var list = new List<TierKillResult>();
            foreach (var kv in _killsPerTier)
            {
                list.Add(new TierKillResult
                {
                    TierName   = kv.Key.tierName,
                    Kills      = kv.Value,
                    RewardEach = kv.Key.reward,
                });
            }
            return list;
        }

#if UNITY_EDITOR
        [UnityEngine.ContextMenu("TEST: 현재 집계 로그")]
        private void DebugLog()
        {
            Debug.Log($"[ScoreTracker] TotalCurrency={TotalCurrency}");
            foreach (var kv in _killsPerTier)
                Debug.Log($"  {kv.Key.tierName}: {kv.Value}마리");
        }
#endif
    }
}
