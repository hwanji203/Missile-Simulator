using System.Collections.Generic;
using UnityEngine;

namespace UI.Popup.Meta
{
    [CreateAssetMenu(fileName = "MetaBuff", menuName = "Meta/MetaBuff")]
    public class MetaBuffSO : ScriptableObject
    {
        [Header("표시용 base 값 — 게임플레이 실제값과 맞출 것(좌하단 수치 텍스트 전용 미러)")]
        [Tooltip("PlayerDefaultStatusSO.DefaultHp와 일치")]
        public float baseHealth = 50f;
        [Tooltip("도화선 여유시간 base(보너스 자체라 보통 0)")]
        public float baseSpareTime = 0f;
        [Tooltip("ExplosionDamageCaster.stages 기본 damage와 일치")]
        public float baseExplosionDamage = 5f;
        [Tooltip("ExplosionDamageCaster.stages 기본 radius와 일치")]
        public float baseExplosionRange = 5f;

        [Header("레벨당 증가량")]
        public float healthPerLevel     = 10f;
        public float spareTimePerLevel = 2f;
        public float explosionDamagePerLevel = 7f;
        public float explosionRangePerLevel = 7f;
        
        [Header("레벨당 골드 요구량")]
        public List<int> needGoldPerLevels = new List<int>();

        public bool TryBuy(ref int remainingGold, int currentLevel)
        {
            if (needGoldPerLevels.Count <= currentLevel) return false;
            if (needGoldPerLevels[currentLevel] <= remainingGold)
            {
                remainingGold -= needGoldPerLevels[currentLevel];
                return true;
            }
            return false;
        }

        // 좌하단 미사일 수치 텍스트용. "현재값(+증가분)" — 만렙이면 증가분 괄호 생략.
        // 현재값 = baseValue + perLevel*level. maxLevel = needGoldPerLevels.Count.
        public string FormatStat(float baseValue, float perLevel, int level)
        {
            float current = baseValue + perLevel * level;
            string cur = current.ToString("0.##");
            return level < needGoldPerLevels.Count
                ? $"{cur}(+{perLevel.ToString("0.##")})"
                : cur;
        }
    }
}
