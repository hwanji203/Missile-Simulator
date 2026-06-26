using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Popup.Meta
{
    public enum MetaStatType { Health, SpareTime, ExplosionDamage, ExplosionRange }

    // 스탯 아이템 호버 시 우상단 고정 정보 영역 갱신을 요청한다(레이아웃을 흔들던 MetaShowTooltip 대체).
    // 값 계산은 Presenter/Model이 담당하고, 이 컴포넌트는 어떤 스탯인지와 호버 상태만 알린다.
    public class MetaStatHoverInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private MetaStatType stat;
        [Tooltip("우상단에 함께 표시할 스탯 이름(예: 폭발 위력)")]
        [SerializeField] private string displayName;
        [Tooltip("우상단에 제목과 함께 표시할 설명 문구")]
        [TextArea]
        [SerializeField] private string description;
        [Tooltip("호버 시 켜질 선택 하이라이트(선택)")]
        [SerializeField] private GameObject selectBackground;

        public MetaStatType Stat => stat;
        public string DisplayName => displayName;
        public string Description => description;

        // (source, entered) — Presenter가 구독해 우상단 정보를 갱신.
        public event Action<MetaStatHoverInfo, bool> HoverChanged;

        private void Awake()
        {
            if (selectBackground != null) selectBackground.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectBackground != null) selectBackground.SetActive(true);
            HoverChanged?.Invoke(this, true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (selectBackground != null) selectBackground.SetActive(false);
            HoverChanged?.Invoke(this, false);
        }
    }
}
