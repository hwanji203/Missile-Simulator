using EventChannelSystem;
using Events;
using ModuleSystem;
using UnityEngine;

namespace Cameras
{
    // 슈퍼 부스트 동안만 카메라 FOV(광각)를 넓힌다.
    // 슈퍼부스트 시작(PlayerSuperBoostEvent)에 boostFov로, 정지/폭발(Stopped/Exploded)에 baseFov로 복귀.
    // (슈퍼부스트는 별도 종료 이벤트가 없고 땅에 닿아 멈출 때 끝나므로 그 이벤트들로 복귀한다.)
    // FOV만 건드리므로 위치/회전 적용자(CameraOwner)와 충돌하지 않는다.
    public class CameraFovModule : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO playerEvent;

        [Tooltip("평상시 기본 FOV")]
        [SerializeField] private float baseFov = 60f;
        [Tooltip("슈퍼 부스트 중 도달할 FOV")]
        [SerializeField] private float boostFov = 80f;
        [Tooltip("FOV가 목표값으로 따라가는 속도 (클수록 민첩)")]
        [SerializeField] private float fovDamping = 4f;

        private Camera _camera;
        private float _targetFov;

        public void Initialize(ModuleOwner owner)
        {
            _camera = owner.GetComponentInChildren<Camera>();
            Debug.Assert(_camera != null, "CameraFovModule: owner 트리에서 Camera를 찾지 못했습니다.");

            _targetFov = baseFov;
            if (_camera != null) _camera.fieldOfView = baseFov;

            if (playerEvent == null) return;
            playerEvent.AddListener<PlayerSuperBoostEvent>(OnSuperBoost);
            playerEvent.AddListener<PlayerStoppedEvent>(OnReset);
            playerEvent.AddListener<PlayerExplodedEvent>(OnReset);
        }

        private void OnDestroy()
        {
            if (playerEvent == null) return;
            playerEvent.RemoveListener<PlayerSuperBoostEvent>(OnSuperBoost);
            playerEvent.RemoveListener<PlayerStoppedEvent>(OnReset);
            playerEvent.RemoveListener<PlayerExplodedEvent>(OnReset);
        }

        private void OnSuperBoost(PlayerSuperBoostEvent evt)
        {
            if (evt.IsStarted)
                _targetFov = boostFov;
        }

        private void OnReset(PlayerStoppedEvent _) => _targetFov = baseFov;
        private void OnReset(PlayerExplodedEvent _) => _targetFov = baseFov;

        private void LateUpdate()
        {
            if (_camera == null) return;
            float k = 1f - Mathf.Exp(-fovDamping * Time.deltaTime);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFov, k);
        }
    }
}
