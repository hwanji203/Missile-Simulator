using CombatSystem;
using EventChannelSystem;
using Players;
using Players.Movement;
using UnityEngine;

namespace Events
{
    public static class PlayerEvents
    {
        public static readonly PlayerExplodedEvent PlayerExplodedEvent = new PlayerExplodedEvent();
        public static readonly PlayerScreenVfxEvent PlayerScreenVfxEvent = new PlayerScreenVfxEvent();
        public static readonly PlayerStoppedEvent PlayerStoppedEvent = new PlayerStoppedEvent();
        public static readonly PlayerSuperBoostEvent PlayerSuperBoostEvent = new PlayerSuperBoostEvent();
        public static readonly PlayerPeekRequestedEvent PlayerPeekRequestedEvent = new PlayerPeekRequestedEvent();
        public static readonly PlayerHitEvent PlayerHitEvent = new PlayerHitEvent();
        public static readonly PlayerSkillAcquiredEvent PlayerSkillAcquiredEvent = new PlayerSkillAcquiredEvent();
        public static readonly StatItemPickedUpEvent StatItemPickedUpEvent = new StatItemPickedUpEvent();
        public static readonly PlayerInitEvent PlayerInitEvent = new PlayerInitEvent();
        public static readonly FuseTimeChangedEvent FuseTimeChangedEvent = new FuseTimeChangedEvent();
        public static readonly PlayerVelocityZero PlayerVelocityZero = new PlayerVelocityZero();
        public static readonly PlayerViewChangedEvent PlayerViewChangedEvent = new PlayerViewChangedEvent();
        public static readonly PlayerBounceActiveEvent PlayerBounceActiveEvent = new PlayerBounceActiveEvent();
        public static readonly CameraFreezeEvent CameraFreezeEvent = new CameraFreezeEvent();
    }

    // 카메라가 더 이상 플레이어를 따라가지 않고 현재 위치에 고정되게 한다(수중 추락 등 비폭발 종료 연출).
    // CameraOwner가 이 신호 이후 LateUpdate 추적을 멈춘다.
    public class CameraFreezeEvent : GameEvent
    {
    }

    // 바운스 시퀀스가 진행 중인 동안(Active=true) 게임 종료 카운트다운을 보류시키는 신호.
    // BounceEffect가 시퀀스 시작 시 true, 모든 바운스가 끝나면 false를 발행한다.
    // 첫 바운스 비행이 postExplosionDelay보다 길어도 종료가 먼저 나지 않게 한다.
    public class PlayerBounceActiveEvent : GameEvent
    {
        public bool Active { get; private set; }

        public PlayerBounceActiveEvent Init(bool active)
        {
            Active = active;
            return this;
        }
    }

    // 플레이어(미사일) 초기화 완료 시 1회 발행. 생성형 HUD·카메라 등 외부 구독자가
    // SerializeField/DI 없이 플레이어 모듈 참조를 받아가는 통로.
    // 발행 시점은 PlayerController.Start — 모든 Awake(UIManager의 HUD 구독 포함)가 끝난 뒤라
    // 늦은 구독으로 이벤트를 놓치지 않는다.
    public class PlayerInitEvent : GameEvent
    {
        public HealthModule    Health { get; private set; }
        public IRotateMovement Rotate { get; private set; }
        public MissileFuse Fuse { get; private set; }
        public GameObject Player { get; private set; } // 외부(스포너 등)가 플레이어 위치를 추적하는 통로

        public PlayerInitEvent Init(HealthModule health, IRotateMovement rotate, MissileFuse fuse, GameObject player)
        {
            Health = health;
            Rotate = rotate;
            Fuse = fuse;
            Player = player;
            return this;
        }
    }

    // 공중 스탯 아이템을 미사일이 먹었을 때 발행. StatPickupTriggerModule이 받아 해당 스탯을 지급.
    // Stat은 스폰 시점에 정해진 아이템의 스탯(아이템 색=받을 스탯). null이면 랜덤 폴백.
    public class StatItemPickedUpEvent : GameEvent
    {
        public Vector3 Position { get; private set; }
        public PlayerSkillSO Stat { get; private set; }

        public StatItemPickedUpEvent InitData(Vector3 position, PlayerSkillSO stat)
        {
            Position = position;
            Stat = stat;
            return this;
        }
    }

    // 미사일이 피해를 입었지만(아직 살아있음) 1회 발행. 카메라 피격 쉐이크 트리거.
    public class PlayerHitEvent : GameEvent
    {
    }

    // 파워업 좀비 요격 시 스킬 추가/강화됐을 때 발행. 알림 UI 트리거.
    // 표시용 데이터(타이틀/설명/아이콘/색)는 Skill SO가 단일 출처라 SO 참조를 그대로 싣는다.
    public class PlayerSkillAcquiredEvent : GameEvent
    {
        public PlayerSkillSO Skill { get; private set; }
        public int Level { get; private set; }
        public bool IsNew { get; private set; } // true=신규, false=강화

        public PlayerSkillAcquiredEvent InitData(PlayerSkillSO skill, int level, bool isNew)
        {
            Skill = skill;
            Level = level;
            IsNew = isNew;
            return this;
        }
    }

    public class PlayerTimerDoneEvent : GameEvent
    {
    }

    // 미사일이 조종을 멈춘 순간(접촉 또는 타이머 종료 중 먼저) 1회 발행. 카메라 3인칭 진입 트리거.
    public class PlayerStoppedEvent : GameEvent
    {
        public GameObject Player { get; set; }

        public PlayerStoppedEvent Init(GameObject player)
        {
            Player = player;
            return this;
        }
    }
    
    public class PlayerVelocityZero : GameEvent
    {
    }

    // 슈퍼 부스트 돌진 시작 시 발행 예정(이동 로직은 추후). 카메라 3인칭 진입 트리거.
    public class PlayerSuperBoostEvent : GameEvent
    {
        public bool IsPressed { get;  private set; }
        public bool IsCanceled { get; private set; }
        public bool IsStarted { get; private set; }

        public PlayerSuperBoostEvent InitData(bool isPressed, bool isCanceled, bool isStarted)
        {
            IsPressed = isPressed;
            IsCanceled = isCanceled;
            IsStarted = isStarted;
            return this;
        }
    }

    // 'peek' 카메라 요청. 카메라가 쏙 나왔다가 일정 시간 뒤 1인칭으로 복귀하는 시한부 연출 트리거.
    // 실제 발행은 게임 측(예: 타겟 명중 등)에서 한다.
    public class PlayerPeekRequestedEvent : GameEvent
    {
    }

    // 1인칭(Follow) ↔ 3인칭(QuarterView) 시점이 토글될 때 발행. 1인칭 의존 연출들이 구독한다.
    public class PlayerViewChangedEvent : GameEvent
    {
        public bool IsFirstPerson { get; private set; }

        public PlayerViewChangedEvent InitData(bool isFirstPerson)
        {
            IsFirstPerson = isFirstPerson;
            return this;
        }
    }

    // 화면(풀스크린) VFX 재생/정지 명령. 어떤 상황에서든 발행 가능.
    public class PlayerScreenVfxEvent : GameEvent
    {
        public int VfxHash { get; private set; }
        public bool Play { get; private set; }   // true=재생, false=정지

        public PlayerScreenVfxEvent InitData(int vfxHash, bool play)
        {
            VfxHash = vfxHash;
            Play = play;
            return this;
        }
    }

    // 플레이어(미사일)가 터졌을 때 발행. 터진 위치를 함께 전달한다.
    public class PlayerExplodedEvent : GameEvent
    {
        public Vector3 Position { get; private set; }
        public GameObject Player { get; set; }
        public float Scale { get; private set; } // 폭발 반경 배수(카메라 후퇴 강도). 1=기본.

        public PlayerExplodedEvent InitData(Vector3 position, GameObject player, float scale = 1f)
        {
            Position = position;
            Player = player;
            Scale = scale;
            return this;
        }
    }

    // 도화선 남은 시간이 변할 때(매 프레임) 발행. StatusHUD 시간바가 구독.
    // IsInfinite=true(슈퍼 부스트 중)면 HUD는 바를 꽉 채우고 "∞"를 띄운다.
    public class FuseTimeChangedEvent : GameEvent
    {
        public float MaxFuseTime { get; private set; }
        public float CurrentFuseTime { get; private set; }
        public bool IsInfinite { get; private set; }
        public float SpareTime { get; private set; } // 땅 접촉 조기 폭발 마진(초). HUD 마진 마커가 사용.

        public FuseTimeChangedEvent InitData(float maxFuseTime, float currentFuseTime, bool isInfinite, float SpareTime)
        {
            MaxFuseTime = maxFuseTime;
            CurrentFuseTime = currentFuseTime;
            IsInfinite = isInfinite;
            this.SpareTime = SpareTime;
            return this;
        }
    }
}
