using System;
using System.Collections.Generic;
using UnityEngine;

namespace Enemies.Spawning
{
    // 스포너 단위 "동시 공격 수" 제한 관리자. 스포너와 같은 GameObject에 붙인다.
    // ZombieSpawner가 스폰한 좀비에게 자신을 주입하고, ThrowSkill이 던지기 시작~종료 동안 슬롯을 점유한다.
    // 슬롯이 없으면 ThrowSkill.CanUseSkill이 false → BT는 공격하지 않고 추격만 계속한다.
    // → 사거리를 크게 올려도(전방향 공격) 스포너당 동시 투사체 수가 이 값으로 캡된다.
    public class EnemyAttackCoordinator : MonoBehaviour
    {
        [Tooltip("이 스포너 출신 좀비 중 동시에 던질 수 있는 최대 수.")]
        [Min(1)] [SerializeField] private int maxConcurrentAttackers = 2;

        // 현재 슬롯을 점유 중인 주체(ThrowSkill 인스턴스). HashSet이라 중복 점유/반납이 안전(멱등).
        private readonly HashSet<object> _attackers = new();
        private EnemyAttackCoordinator _rootCoordinator;

        public bool HasFreeSlot => _attackers.Count < maxConcurrentAttackers;

        private void Awake()
        {
            _rootCoordinator = transform.parent.GetComponent<EnemyAttackCoordinator>();
        }

        public bool TryAcquire(object owner)
        {
            if (_attackers.Contains(owner)) return true; // 이미 점유 중 — 멱등
            if (_attackers.Count >= maxConcurrentAttackers) return false;
            if (_rootCoordinator != null && !_rootCoordinator.TryAcquire(owner)) return false;
            _attackers.Add(owner);
            return true;
        }

        public void Release(object owner)
        {
            _rootCoordinator?.Release(owner);
            _attackers.Remove(owner);
        }
    }
}
