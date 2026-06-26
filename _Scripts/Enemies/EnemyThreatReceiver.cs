using System;
using System.Collections;
using Enemies.BT;
using EventChannelSystem;
using Events;
using Unity.Behavior;
using UnityEngine;

namespace Enemies
{
    // 플레이어(미사일)가 멈추거나(PlayerStoppedEvent) 터지면(PlayerExplodedEvent) 위협으로 받아,
    // DetectRadius 안에 든 적을 FLEE 상태로 보낸다. (BT 그래프의 FLEE 분기 배선은 사용자가 한다.)
    //
    // 두 이벤트는 같은 채널에서 온다. 이미 위협 도망 중인 적은 나중에 온 이벤트를 무시한다.
    // 단, 첫 이벤트 때 사거리 밖이던 적은 위협 플래그가 꺼져 있으므로 — 미사일이 공중 폭발 후
    // 땅에 떨어지며 두 번째 이벤트(정지)가 올 때, 그제야 사거리에 든 적도 도망친다.
    [RequireComponent(typeof(AbstractEnemy))]
    public class EnemyThreatReceiver : MonoBehaviour
    {
        [Tooltip("PlayerStoppedEvent/PlayerExplodedEvent가 발행되는 플레이어 채널(둘 다 같은 채널).")] [SerializeField]
        private EventChannelSO playerChannel;
        [SerializeField] private float stopAfterExplosionTime = 4;

        private AbstractEnemy _enemy;
        private Coroutine _waitStopRoutine; // 폭발 후 IDLE 복귀 대기(바운스로 갱신 가능)

        private void Awake()
        {
            _enemy = GetComponent<AbstractEnemy>();
        }

        private void OnEnable()
        {
            if (playerChannel == null) return;
            playerChannel.AddListener<PlayerStoppedEvent>(OnThreat);
            playerChannel.AddListener<PlayerExplodedEvent>(OnThreat);
        }

        private void OnDisable()
        {
            if (playerChannel == null) return;
            playerChannel.RemoveListener<PlayerStoppedEvent>(OnThreat);
            playerChannel.RemoveListener<PlayerExplodedEvent>(OnThreat);
        }

        // 두 이벤트를 같은 처리로 합류시킨다(메서드 그룹 오버로드 해석).
        private void OnThreat(PlayerStoppedEvent data) => TryFlee(data.Player);
        private void OnThreat(PlayerExplodedEvent data) => BeginWaitForStop();

        // "이미 도망 중"의 단일 기준은 IsThreatened 블랙보드 변수다(중복 latch 제거).
        // 위협이 끝나면(WaitForStop) false로 리셋되므로 다음 로켓에 다시 도망갈 수 있다.
        private void TryFlee(GameObject player)
        {
            if (_enemy == null || _enemy.IsDead || _enemy.Sensor == null) return;

            if (_enemy.GetVariable(BtVars.IsThreatened, out BlackboardVariable<bool> threatened)
                && threatened.Value)
                return;

            // 도망 대상 = 플레이어(FleeFromTargetAction이 반대편으로, leash 무시하고 달아난다).
            _enemy.SetVariableValue(BtVars.TargetGameObject, player);
            _enemy.SetVariableValue(BtVars.IsThreatened, true);
            _enemy.StateChannel?.SendEventMessage(EnemyState.FLEE);
        }

        // 바운스로 PlayerExplodedEvent가 여러 번 와도 타이머를 갱신(재시작)한다.
        private void BeginWaitForStop()
        {
            if (_enemy == null || _enemy.IsDead) return;
            if (_waitStopRoutine != null) StopCoroutine(_waitStopRoutine);
            _waitStopRoutine = StartCoroutine(WaitForStop());
        }

        private IEnumerator WaitForStop()
        {
            yield return new WaitForSeconds(stopAfterExplosionTime);
            // 위협 종료: IDLE 복귀 + 타겟 비우기 + IsThreatened 해제(다음 로켓에 재무장).
            _enemy.StateChannel?.SendEventMessage(EnemyState.IDLE);
            _enemy.SetVariableValue<GameObject>(BtVars.TargetGameObject, null);
            _enemy.SetVariableValue(BtVars.IsThreatened, false);
            _waitStopRoutine = null;
        }
    }
}
