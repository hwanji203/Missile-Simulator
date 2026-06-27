using System;
using MVP.System.AbstractMVP.SaveMVP;
using MVP.UIData;
using Players;
using UnityEngine;
using UnityEngine.Audio;

namespace UI.Popup.Setting
{
    /// <summary>
    /// 설정창 데이터/로직. 볼륨 3종(Master/BGM/SFX) + 마우스 감도.
    /// Form은 메서드명 문자열로 아래 private 메서드에 바인딩된다(인스펙터에서 선택).
    /// - interact(슬라이더 조작 시): void XxxChangeHandler(UIParam)
    /// - update(창 열 때 동기화):    UIParam UpdateXxx()
    /// </summary>
    public class SettingModel : ISaveableModel
    {
        [Serializable]
        private class SettingInfo
        {
            public float masterVolume = 0.5f;
            public float bgmVolume = 0.5f;
            public float sfxVolume = 0.5f;
            public float mouseSensitivity = 1f;
        }

        // Presenter가 주입한다(프리팹 인스펙터 참조).
        public AudioMixer AudioMixer { get; set; }
        public PlayerInputSO PlayerInput { get; set; }

        private SettingInfo _info = new SettingInfo();

        // ── ISaveableModel ──
        public string StoreData() => JsonUtility.ToJson(_info);

        public void RestoreData(string data)
        {
            _info = string.IsNullOrEmpty(data)
                ? new SettingInfo()
                : JsonUtility.FromJson<SettingInfo>(data);
            ApplyAll();
        }

        /// <summary>현재 값(기본 또는 복원)을 믹서·입력에 즉시 반영. Presenter가 의존성 주입 직후 호출.</summary>
        public void ApplyAll()
        {
            if (_info == null) return;
            ApplyMaster();
            ApplyBgm();
            ApplySfx();
            ApplySensitivity();
        }

        // ── interact: 슬라이더가 움직이면 호출 (void M(UIParam)) ──
        private void MasterVolumeChangeHandler(UIParam data)
        {
            _info.masterVolume = ((UIFloatParam)data).Value;
            ApplyMaster();
        }

        private void BgmVolumeChangeHandler(UIParam data)
        {
            _info.bgmVolume = ((UIFloatParam)data).Value;
            ApplyBgm();
        }

        private void SfxVolumeChangeHandler(UIParam data)
        {
            _info.sfxVolume = ((UIFloatParam)data).Value;
            ApplySfx();
        }

        private void MouseSensitivityChangeHandler(UIParam data)
        {
            _info.mouseSensitivity = ((UIFloatParam)data).Value;
            ApplySensitivity();
        }

        // ── update: 창 열 때 슬라이더를 현재값으로 맞춤 (UIParam M()) ──
        private UIParam UpdateMasterVolume() => UIParams.UIFloatParam.Init(_info.masterVolume);
        private UIParam UpdateBgmVolume() => UIParams.UIFloatParam.Init(_info.bgmVolume);
        private UIParam UpdateSfxVolume() => UIParams.UIFloatParam.Init(_info.sfxVolume);
        private UIParam UpdateMouseSensitivity() => UIParams.UIFloatParam.Init(_info.mouseSensitivity);

        // ── 적용 ──
        private void ApplyMaster() => SetMixer("Master", _info.masterVolume);
        private void ApplyBgm() => SetMixer("BGM", _info.bgmVolume);
        private void ApplySfx() => SetMixer("SFX", _info.sfxVolume);

        private void ApplySensitivity()
        {
            if (PlayerInput != null) PlayerInput.Sensitivity = _info.mouseSensitivity;
        }

        // 0~1 선형 볼륨 → dB(AudioMixer 노출 파라미터). 0은 -80dB(무음).
        private void SetMixer(string exposedParam, float linear01)
        {
            if (AudioMixer == null) return;
            float db = linear01 <= 0.0001f ? -80f : Mathf.Log10(linear01) * 20f;
            AudioMixer.SetFloat(exposedParam, db);
        }
    }
}
