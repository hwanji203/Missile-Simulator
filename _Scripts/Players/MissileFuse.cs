using System.Collections;
using Agents;
using CombatSystem;
using EventChannelSystem;
using Events;
using ModuleSystem;
using Players.Movement;
using UnityEngine;

namespace Players
{
    // 미사일 도화선.
    // 폭발은 오직 "타이머 종료"가 트리거한다 — 타이머가 끝나면 그 자리에서 즉시 폭발(공중이면 공중에서).
    //  - 땅/적에 닿으면: 조종을 멈추고 중력만 받지만, 폭발하지는 않는다(타이머를 기다린다).
    //  - 슈퍼 부스트 중: 타이머가 끝나도 폭발하지 않고 계속 돌진. 땅에 닿아 종료되는 순간,
    //    타이머가 이미 끝나 있으면 그때 폭발한다.
    // → 한 줄로: 폭발 조건 = (타이머 종료) && (슈퍼 부스트 아님).
    [RequireComponent(typeof(Rigidbody))]
    public class MissileFuse : MonoBehaviour, IModule
    {
        [SerializeField] private PlayerDefaultStatusSO status;
        [SerializeField] private LayerMask whatStopsMissile;   //멈춤을 유발하는 레이어 (땅 + 적)
        [SerializeField] private int explosionSkillIndex;  //발동할 폭발 스킬 인덱스
        [SerializeField] private EventChannelSO playerEvent;   //정지 이벤트 발행 채널 (카메라 3인칭 트리거)

        [Header("슈퍼 부스트 관통")]
        [Tooltip("슈퍼 부스트 중 초당 깎이는 HP(오버로드). HP가 0이 되면 사망 → 그 자리 폭발.")]
        [SerializeField] private float overloadPerSecond = 5f;
        
        private ModuleOwner _owner;
        private ISkillModule _skillModule;
        private IMovement _movement;
        private Agent _combatAgent;
        private HealthModule _healthModule;
        
        private float _fuseDuration;      //발사 후 도화선 시간(초)

        private float _spareFuseTime; // 땅/적에 닿아 정지하는 순간 도화선에서 깎을 시간(초). 메타 버프로 주입.
        private float _baseSpareTime;             // 메타가 주입하는 base(초)
        private ISkillBonusProvider _bonusProvider; // 인런 스탯 보너스 공급원
        private bool _timerDone;

        private bool _hasContacted;
        private bool _exploded;
        private bool _stopRaised;
        private float _fuseElapsed;
        private bool _startDecrease;

        public float RemainingTime => Mathf.Max(0f, _fuseDuration - _fuseElapsed);
        public bool IsInfinite => _movement != null && _movement.IsSuperBoosting; // 부스트 중엔 폭발 지연 → UI는 ∞

        public void Initialize(ModuleOwner owner)
        {
            _owner = owner;
            _skillModule = owner.GetModule<ISkillModule>();
            Debug.Assert(_skillModule != null, "MissileFuse는 ISkillModule이 필요합니다.");

            _movement = owner.GetModule<IMovement>();
            _healthModule = owner.GetModule<HealthModule>();

            // 인런 FuseSpareTime 획득 시 즉시 재계산(HpUp이 비행 중 max를 올리는 것과 동일 UX).
            _bonusProvider = owner.GetModule<ISkillBonusProvider>();
            if (_bonusProvider != null)
                _bonusProvider.OnBonusChanged += RecalcFuse;

            // HP가 0이 되면(적 피격 또는 슈퍼 부스트 과열) 타이머/부스트와 무관하게 그 자리에서 즉시 폭발.
            _combatAgent = owner as Agent;
            if (_combatAgent != null)
                _combatAgent.OnDeath += Explode;
            
            playerEvent.AddListener<PlayerSuperBoostEvent>(HandleSuperBooster);
            _startDecrease = false;
        }

        private void HandleSuperBooster(PlayerSuperBoostEvent evt)
        {
            if (evt.IsStarted)
                StartCoroutine(ApplyOverload());
        }

        private void OnDestroy()
        {
            if (_combatAgent != null)
                _combatAgent.OnDeath -= Explode;
            playerEvent.RemoveListener<PlayerSuperBoostEvent>(HandleSuperBooster);
            if (_bonusProvider != null)
                _bonusProvider.OnBonusChanged -= RecalcFuse;
        }

        private void OnEnable()
        {
            // 발사 시작: 도화선 타이머 시작 (Initialize는 Awake 단계에서 이미 끝나 있음)
            _timerDone = false;
            _hasContacted = false;
            _exploded = false;
            _stopRaised = false;
            _fuseElapsed = 0f;
        }

        // 도화선 경과를 직접 센다(WaitForSeconds 대신) — StatusHUD 시간바가 남은 시간을 읽을 수 있게.
        private void Update()
        {
            if (_startDecrease == false)
                return;
            
            // 폭발 전까지 매 프레임 남은 시간을 이벤트로 발행(슈퍼 부스트 중이면 IsInfinite=true → HUD ∞).
            if (!_exploded)
                playerEvent?.RaiseEvent(
                    PlayerEvents.FuseTimeChangedEvent.InitData(_fuseDuration, RemainingTime
                        , IsInfinite, _spareFuseTime));

            if (_timerDone) return;

            _fuseElapsed += Time.deltaTime;
            if (_fuseElapsed < _fuseDuration - (_stopRaised ? _spareFuseTime : 0)) return;

            _timerDone = true;
            // 폭발 조건을 시도한다. 슈퍼 부스트 중이면 여기선 안 터지고,
            // 나중에 땅에 닿아 부스트가 끝나는 순간 OnCollisionEnter에서 터진다.
            TryExplode();
        }

        private void OnCollisionEnter(Collision collision)
        {
            int layer = collision.gameObject.layer;
            bool inStop = (whatStopsMissile.value & (1 << layer)) != 0;

            // 슈퍼 부스트 중: 적/기믹은 파괴·데미지 후 뚫고 지나간다(정지하지 않음).
            if (!inStop) return;

            if (!_hasContacted)
            {
                _hasContacted = true;

                // 슈퍼 부스트 중 땅 접촉 = 타이머와 무관하게 그 자리에서 즉시 폭발.
                if (_movement != null && _movement.IsSuperBoosting)
                {
                    ForceExplode();
                    return;
                }

                StopOnContact(); //멈추고 중력만 받음. 폭발은 타이머가 끝날 때.
            }
        }

        // 추진 정지: 조종을 끄고 이후 물리(중력)에 맡긴다. 슈퍼 부스트도 여기서 종료된다.
        private void StopMovementModules()
        {
            foreach (IStoppableMovement movement in _owner.GetComponentsInChildren<IStoppableMovement>())
                movement.StopMovement();
            if (!_stopRaised)
            {
                _stopRaised = true;
                playerEvent?.RaiseEvent(PlayerEvents.PlayerStoppedEvent.Init(gameObject));
            }
        }

        // 속도/회전을 즉시 0으로 — 그 자리에 멈췄다가 중력으로 추락한다.
        private void FreezeAll()
        {
            foreach (IContactFreezable freezable in _owner.GetComponentsInChildren<IContactFreezable>())
                freezable.Freeze();
        }

        // 땅/적 접촉으로 멈출 때: 정지 + 회전 프리즈 + 1회 PlayerStoppedEvent(카메라 Stop 3인칭).
        private void StopOnContact()
        {
            StopMovementModules();
            FreezeAll();
        }

        // 폭발 조건: 타이머 종료 && 슈퍼 부스트 아님. (타이머/접촉 양쪽에서 호출)
        private void TryExplode()
        {
            if (_exploded) return;
            if (!_timerDone) return;
            if (_movement != null && _movement.IsSuperBoosting) return; //부스트 중엔 지연(땅 접촉 때까지)

            Explode();
        }

        // 실제 폭발 처리. 타이머 만료(TryExplode)와 사망(CombatAgent.OnDeath) 양쪽이 공유하며,
        // _exploded 가드로 한 번만 터진다(타이머/사망 중복 방지).
        private void Explode()
        {
            if (_exploded) return;
            _exploded = true;
            // 폭발: StopMovementModules가 속도를 0으로 만들고 중력만 남긴다. 회전은 얼리지 않으므로
            // (FreezeAll 호출 안 함) 0이 된 속도가 중력으로 아래로 자라며 MissileRotation이 그 방향을
            // 따라가 코를 아래로 박고 추락한다. 땅에 닿는 순간 StopOnContact의 FreezeAll이 회전을 잠근다.
            StopMovementModules();
            _skillModule.UseSkill(explosionSkillIndex); //ExplosionSkill이 PlayerExplodedEvent(카메라 Explode) 발행
        }

        // 외부에서 강제 폭발 트리거(조기 종료 등). 슈퍼 부스트 체크 없이 즉시 폭발.
        public void ForceExplode()
        {
            _timerDone = true;
            Explode();
        }

        // 외부(수중 추락 등 비폭발 종료)에서 호출: 폭발 없이 그 자리에서 완전히 멈춘다(추진 정지 + 프리즈).
        // _exploded 가드를 세워, 대기 중 도화선 만료/사망으로도 더는 폭발하지 않게 한다.
        public void StopForHazard()
        {
            _exploded = true;
            StopMovementModules();
            FreezeAll();
        }
        
        private IEnumerator ApplyOverload()
        {
            while (_healthModule != null && _healthModule.CurrentHealth >= 0)
            {
                yield return new WaitForSeconds(1);
                _healthModule?.ApplyDamage(overloadPerSecond);
            }
        }

        // MetaBuffApplier가 게임 시작 시 메타 버프(base)를 주입한다. 값(초)만큼,
        // 땅/적 접촉 시 도화선을 앞당겨 조기 폭발시키는 마진의 base를 설정하고 타이머를 시작시킨다.
        // 실효값은 인런 FuseSpareTime 보너스가 가산된다(RecalcFuse).
        public void SetSpareTime(float seconds)
        {
            _baseSpareTime = seconds;
            _startDecrease = true; // 타이머 시작은 메타 주입(게임 시작) 시점 기준.
            RecalcFuse();
        }

        // base(메타) + bonus(인런)로 도화선 시간/마진을 재계산. SetSpareTime과 OnBonusChanged가 공유.
        private void RecalcFuse()
        {
            float effective = _baseSpareTime + (_bonusProvider?.GetBonus(SkillType.FuseSpareTime) ?? 0f);
            _spareFuseTime = effective;
            _fuseDuration = status.DefaultFuseTime + effective / 2f;
        }
    }
}
