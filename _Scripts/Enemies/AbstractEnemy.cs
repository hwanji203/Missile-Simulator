using Agents;
using CombatSystem;
using Enemies.BT;
using Enemies.BT.Events;
using Unity.Behavior;
using UnityEngine;

namespace Enemies
{
    [RequireComponent(typeof(BehaviorGraphAgent))]
    public abstract class AbstractEnemy : Agent
    {
        [field: SerializeField] public float DetectRadius { get; set; } = 5f; //감지 거리
        [field: SerializeField] public float ViewAngle { get; set; } = 160f; //시야각
        [field: SerializeField] public float StopDistance { get; set; } = 1f; //정지 최소 거리
        [field: SerializeField] protected bool CanDrawDebug { get; private set; }

        public INavMovement NavMovement { get; private set; }
        public BehaviorGraphAgent BtAgent { get; private set; }
        public IRenderer AgentRenderer { get; private set; }
        public AgentSensor Sensor { get; private set; }
        public ISkillModule SkillModule { get; private set; }
        public AgentTrigger Trigger { get; private set; }

        public StateChannel StateChannel { get; private set; }

        // 이동 가능한 "집" 원(leash). 스폰 위치 중심, 반경은 티어가 정한다(ZombieEnemy.ApplyTier).
        // 예시의 WayPoint 순찰을 대체한다 — 좀비는 이 원 안에서만 배회/추격한다.
        public Vector3 LeashCenter { get; private set; }
        public float LeashRadius { get; private set; }

        // 체력/피격 조회(BT 조건이 사용). LastDamageFraction = 마지막 데미지 / 최대 체력 → 도망 모션 티어(CROWL/RUN) 분기.
        public float NormalizedHealth => Health != null ? Health.Normalized : 0f;

        public void SetLeash(Vector3 center, float radius)
        {
            LeashCenter = center;
            LeashRadius = radius;
        }


        protected override void InitializeComponents()
        {
            base.InitializeComponents();
            NavMovement = GetModule<INavMovement>();
            AgentRenderer = GetModule<IRenderer>();
            BtAgent = GetComponent<BehaviorGraphAgent>();
            Sensor = GetModule<AgentSensor>();
            SkillModule = GetModule<ISkillModule>();
            Trigger = GetModule<AgentTrigger>();
        }

        protected virtual void Start()
        {
            // BT 그래프에 StateChannel 변수가 있으면 가져온다(없어도 코어 동작은 가능 — null-safe).
            if (BtAgent != null && GetVariable<StateChannel>(BtVars.StateChannel, out var channelVariable))
            {
                StateChannel = channelVariable.Value;
                SetVariableValue(BtVars.Enemy, this);
            }
        }

        protected override void HandleHitEvent()
        {
            base.HandleHitEvent();
            StateChannel?.SendEventMessage(EnemyState.HIT);
        }

        #region BT helpers

        public void SetVariableValue<T>(string variableName, T value)
        {
            Debug.Assert(!string.IsNullOrEmpty(variableName), "변수 이름을 null일 수 없습니다.");

            if (BtAgent.GetVariable(variableName, out BlackboardVariable<T> variable))
            {
                variable.Value = value;
            }
            else
            {
                Debug.LogError($"Var : {variableName} 을 찾을 수 없습니다.");
            }
        }

        public bool GetVariable<T>(string variableName, out BlackboardVariable<T> variable)
        {
            Debug.Assert(!string.IsNullOrEmpty(variableName), "변수 이름은 null일 수 없습니다.");
            return BtAgent.GetVariable(variableName, out variable);
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (!CanDrawDebug) return;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, DetectRadius);
        }
    }
}
