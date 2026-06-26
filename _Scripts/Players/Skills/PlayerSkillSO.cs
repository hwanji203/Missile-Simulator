using CombatSystem;
using UnityEngine;

namespace Players
{
    [CreateAssetMenu(fileName = "PlayerSkill", menuName = "Player/Skill", order = 30)]
    public class PlayerSkillSO : ScriptableObject
    {
        public SkillType skillType;
        [Tooltip("Stat=공중 아이템 풀, Skill=날아가는 좀비 풀. UpgradeService가 카테고리별로 랜덤 획득.")]
        public UpgradeCategory category;
        public string skillName;
        public int maxLevel;
        [Tooltip("레벨 1, 2, 3... 순서의 증가량. 배열 크기 = maxLevel과 일치해야 함.")]
        public float[] perLevelValues;

        [Header("획득 알림(AcquisitionToast) 표시")]
        public string title;             // 토스트 제목(비우면 skillName 대체)
        [TextArea] public string desc;   // 토스트 설명
        public Sprite iconSprite;        // 아이콘 스프라이트
        public Color iconColor = Color.white; // 아이콘 색
    }
}
