using System;
using System.Collections;
using CombatSystem;
using EventChannelSystem;
using Events;
using ModuleSystem;
using Players.Movement;
using UnityEngine;

namespace Players
{
    // 입력만 받아 상태로 보관하고, 회전/이동 모듈에 읽기 전용으로 노출한다.
    public class PlayerInputState : MonoBehaviour, IModule, IInputable
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private float multiplyValue = 3f;
        [SerializeField] private float superBoostHoldTime = 1.5f;

        private Vector3 ScaledRotationInput => _isMultiplierKeyPressed
            ? _inputRotation * multiplyValue
            : _inputRotation;

        private Vector3 _inputRotation;
        private bool _isMultiplierKeyPressed;
        private bool _isSuperBoostStarted;
        private bool _isStopped;
        private Coroutine _superBoostCoroutine;

        private IRotateMovement _rotation;
        private IMovement _movement;
        private PlayerSkillInventory _inventory;

        public void Initialize(ModuleOwner owner)
        {
            _isSuperBoostStarted = false;
            _rotation = owner.GetModule<IRotateMovement>();
            _movement = owner.GetModule<IMovement>();
            _inventory = owner.GetModule<PlayerSkillInventory>();

            playerChannel.AddListener<PlayerStoppedEvent>(PlayerStoppedHandler);

            _isStopped = false;
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<PlayerStoppedEvent>(PlayerStoppedHandler);
        }

        private void PlayerStoppedHandler(PlayerStoppedEvent evt)
            => _isStopped = true;

        public void SetYawPitchRotation(Vector2 input)
        {
            _inputRotation.x = input.x;
            _inputRotation.y = input.y;
        }

        private void Update()
        {
            _rotation.UpdateDesiredRotation(ScaledRotationInput);
        }

        public void SetSuperBoost(bool isPress)
        {
            // SuperBoost는 스킬로 획득해야(레벨>0) 발동. 미획득이면 차지·경고UI·이벤트 모두 안 띄운다.
            if (_inventory == null || _inventory.GetLevel(SkillType.SuperBoost) == 0)
                return;
            if (_isSuperBoostStarted || _isStopped)
                return;
            if (isPress)
            {
                playerChannel.RaiseEvent(PlayerEvents.PlayerSuperBoostEvent
                    .InitData(true, false, false));
                _superBoostCoroutine = StartCoroutine(TrySuperBoost());
            }
            else
            {
                playerChannel.RaiseEvent(PlayerEvents.PlayerSuperBoostEvent
                    .InitData(false, true, false));
                if (_superBoostCoroutine != null)
                    StopCoroutine(_superBoostCoroutine);
            }
        }

        private IEnumerator TrySuperBoost()
        {
            yield return new WaitForSeconds(superBoostHoldTime);
            if (_movement.TryStartSuperBoost())
            {
                _isSuperBoostStarted = true;
                _superBoostCoroutine = null;
            }
        }
    }
}
