using System;
using DG.Tweening;
using HwanLib.GGMLib.SoundSystem;
using UnityEngine;

namespace MVP.Interaction
{
    /// <summary>
    /// 상태별 연출값 + 사운드 클립. 프로젝트에서 SO 몇 개로 공유해 "버튼 느낌"을 한 곳에서 관리한다.
    /// </summary>
    [CreateAssetMenu(fileName = "InteractionFeedbackProfile", menuName = "MVP/Interaction Feedback Profile")]
    public class InteractionFeedbackProfile : ScriptableObject
    {
        [Serializable]
        public class StateStyle
        {
            public float alpha = 1f;
            public float scale = 1f;
            public Color tint = Color.white;
            public float duration = 0.12f;
            public Ease ease = Ease.OutQuad;
        }

        public StateStyle normal = new();
        public StateStyle highlighted = new() { scale = 1.05f };
        public StateStyle pressed = new() { alpha = 0.85f, scale = 0.96f, duration = 0.06f };
        public StateStyle disabled = new() { alpha = 0.4f };

        [Header("사운드 — 비워두면 무음")]
        public SoundClipSO hoverClip;
        public SoundClipSO clickClip;
        public SoundClipSO popupOpenClip;
        public SoundClipSO popupCloseClip;

        public StateStyle Get(FeedbackState state) => state switch
        {
            FeedbackState.Highlighted => highlighted,
            FeedbackState.Pressed => pressed,
            FeedbackState.Disabled => disabled,
            _ => normal,
        };

        private static InteractionFeedbackProfile _fallback;

        /// <summary>프로파일 미할당 시 사용하는 코드 기본값 인스턴스.</summary>
        public static InteractionFeedbackProfile Fallback
        {
            get
            {
                if (_fallback == null)
                {
                    _fallback = CreateInstance<InteractionFeedbackProfile>();
                    _fallback.hideFlags = HideFlags.HideAndDontSave;
                }
                return _fallback;
            }
        }
    }
}
