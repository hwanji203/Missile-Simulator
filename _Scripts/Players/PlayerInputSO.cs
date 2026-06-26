using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Players
{
    [CreateAssetMenu(fileName = "InputSO", menuName = "Player/InputSO", order = 0)]
    public class PlayerInputSO : ScriptableObject, Controls.IPlayerActions
    {
        // 로켓 조향(WASD/화살표). LookBack 홀드 중엔 로켓엔 zero를 보내 직진시킨다.
        public event Action<Vector2> OnYawPitchRotationChanged;
        // 스페이스 슈퍼 부스트.
        public event Action<bool> OnSuperBoostChange;
        // Shift 누름 = 1인칭(Follow)↔3인칭(QuarterView) 시점 토글 요청(카메라가 구독).
        public event Action OnViewToggleRequested;
        // 마우스 이동량 = 3인칭 시점 회전 입력(카메라가 구독).
        public event Action<Vector2> OnCameraLookChanged;
        // Escape = 설정창 토글 요청(UIOpener가 구독해 UIManager로 라우팅).
        public event Action OnToggleSettingRequested;

        // 마우스 감도 배율(설정창에서 조절). OnCameraLook 델타에 곱해 모든 카메라 상태에 일괄 적용.
        // 런타임엔 SettingModel이 시작 시 덮어쓴다(에셋 직렬화 값은 초기 기본값일 뿐).
        [field: SerializeField] public float Sensitivity { get; set; } = 1f;

        private Controls _controls;

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            if (_controls != null)
                _controls.Player.Disable();
        }

        public void OnYawPitchRotate(InputAction.CallbackContext context)
        {
            OnYawPitchRotationChanged?.Invoke(context.ReadValue<Vector2>());
        }

        // 스페이스 홀드(Hold)로 슈퍼 부스트 발동. 발동은 취소 불가라 false는 상태 정리용일 뿐.
        public void OnSuperBoost(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnSuperBoostChange?.Invoke(true);
            else if (context.canceled)
                OnSuperBoostChange?.Invoke(false);
        }

        // Shift 누름 순간 1회 시점 토글 요청. 토글 중에도 로켓 조향은 계속 살아 있어야 하므로 조향엔 손대지 않는다.
        public void OnLookBack(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnViewToggleRequested?.Invoke();
        }

        // 마우스 이동량(델타) → 3인칭 시점 회전. 카메라가 활성 3인칭일 때만 소비한다.
        public void OnCameraLook(InputAction.CallbackContext context)
        {
            if (context.canceled)
                OnCameraLookChanged?.Invoke(Vector2.zero);
            else
                OnCameraLookChanged?.Invoke(context.ReadValue<Vector2>() * Sensitivity);
        }

        // Escape(Button) → 설정창 토글 요청. 누름(performed) 순간 1회 발행.
        public void OnToggleSetting(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnToggleSettingRequested?.Invoke();
        }
    }
}
