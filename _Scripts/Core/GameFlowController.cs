using System.Collections;
using CombatSystem;
using Enemies;
using EventChannelSystem;
using Events;
using HwanLib.GGMLib.SoundSystem;
using Players;
using UnityEngine;

namespace Core
{
    // 게임 종료 판정.
    //  1) PlayerExplodedEvent 수신 → postExplosionDelay 초 대기 → GameEndedEvent 발행
    //  2) 조기 종료: 살아있는 좀비 전원이 폭발 사정거리 밖이면 ForceExplode → 경로 합류
    public class GameFlowController : MonoBehaviour
    {
        [SerializeField] private ExplosionRangeOutliner outliner;
        [SerializeField] private EventChannelSO       playerChannel;
        [SerializeField] private EventChannelSO       gameChannel;
        [SerializeField] private float postExplosionDelay = 2f;

        [Header("수중 추락 종료")]
        [Tooltip("플레이어 y가 이 값 미만으로 내려가면(물에 빠짐) 폭발 없이 정지 후 종료한다.")]
        [SerializeField] private float deathY = -10f;
        [Tooltip("수중 추락 정지 후 Result까지 대기 시간(초).")]
        [SerializeField] private float waterDeathDelay = 2f;

        [Header("사운드")]
        [SerializeField] private EventChannelSO soundChannel;
        [SerializeField] private SoundClipSO waterDeathSound;
        [SerializeField] private SoundClipSO ingameBgm;

        private bool _gameEnded;
        private bool _bounceActive; // 바운스 시퀀스 진행 중이면 종료 카운트다운 보류
        private bool _waterDeathTriggered;
        private MissileFuse _missileFuse;
        private Transform _playerTransform;

        private void Awake()
        {
            playerChannel?.AddListener<PlayerExplodedEvent>(ExplodedHandler);
            playerChannel?.AddListener<PlayerInitEvent>(PlayerInitHandler);
            playerChannel?.AddListener<PlayerVelocityZero>(PlayerStopHandler);
            playerChannel?.AddListener<PlayerBounceActiveEvent>(BounceActiveHandler);
            gameChannel?.AddListener<GameStartEvent>(GameStartHandler);
        }

        private void OnDestroy()
        {
            playerChannel?.RemoveListener<PlayerExplodedEvent>(ExplodedHandler);
            playerChannel?.RemoveListener<PlayerInitEvent>(PlayerInitHandler);
            playerChannel?.RemoveListener<PlayerVelocityZero>(PlayerStopHandler);
            playerChannel?.RemoveListener<PlayerBounceActiveEvent>(BounceActiveHandler);
            gameChannel?.RemoveListener<GameStartEvent>(GameStartHandler);
            StopBgm();
        }

        private void PlayerStopHandler(PlayerVelocityZero evt)
        {
            StartCoroutine(CheckRemainingTarget());
        }

        private IEnumerator CheckRemainingTarget()
        {
            yield return new WaitUntil(() => outliner.RemainingTargets == 0);
            yield return new WaitForSeconds(1f);
            _missileFuse.ForceExplode();
        }

        private void PlayerInitHandler(PlayerInitEvent evt)
        {
            _missileFuse = evt.Fuse;
            _playerTransform = evt.Player != null ? evt.Player.transform : null;
        }

        // 수중 추락 감지: 도화선과 무관하게 y가 deathY 미만이면 카메라를 그 자리에 멈추고 종료한다.
        private void Update()
        {
            if (_gameEnded || _waterDeathTriggered || _playerTransform == null) return;
            if (_playerTransform.position.y < deathY)
                WaterDeath();
        }

        // 카메라만 현재 위치에 고정하고 N초 뒤 Result. (플레이어 정지=3인칭 당김 연출은 하지 않는다.)
        private void WaterDeath()
        {
            _waterDeathTriggered = true;
            playerChannel?.RaiseEvent(PlayerEvents.CameraFreezeEvent); // 카메라 추적 중단(현재 위치 고정)
            
            if (soundChannel != null && waterDeathSound != null && _playerTransform != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(_playerTransform.position, waterDeathSound));
            }

            StopAllCoroutines();
            StartCoroutine(EndAfterDelay(waterDeathDelay));            // N초 뒤 Result
        }

        private void ExplodedHandler(PlayerExplodedEvent _)
            => ExplodedHandler();

        private void ExplodedHandler()
        {
            if (_waterDeathTriggered) return; // 수중 추락 종료가 우선
            StopAllCoroutines();
            // 바운스 진행 중이면 종료 카운트다운을 시작하지 않는다(시퀀스 종료 시 시작).
            if (!_bounceActive)
                StartCoroutine(EndAfterDelay(postExplosionDelay));
        }

        // 바운스 시퀀스 동안 종료를 보류했다가, 끝나는 순간 카운트다운을 시작한다.
        private void BounceActiveHandler(PlayerBounceActiveEvent evt)
        {
            _bounceActive = evt.Active;
            StopAllCoroutines();
            if (!_bounceActive)
                StartCoroutine(EndAfterDelay(postExplosionDelay));
        }

        private IEnumerator EndAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndGame();
        }

        private void EndGame()
        {
            if (_gameEnded) return;
            _gameEnded = true;
            StopBgm();
            gameChannel?.RaiseEvent(GameEvents.GameEndedEvent.Init(GetComponent<ScoreTracker>()));
        }

        private void GameStartHandler(GameStartEvent evt)
        {
            if (soundChannel != null && ingameBgm != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.PlaySoundEvent.Init(Vector3.zero, ingameBgm, ingameBgm));
            }
        }

        private void StopBgm()
        {
            if (soundChannel != null && ingameBgm != null)
            {
                soundChannel.RaiseEvent(SoundSystemEvents.StopSoundEvent.Init(ingameBgm));
            }
        }
    }
}
