using UnityEngine;

namespace Enemies
{
    // 좀비 한 등급(소/중/대)의 프리셋. 에셋 하나 = 한 티어.
    // 크기 ↔ 체력 ↔ 리시 반경(이동 가능 원)이 이 한 덩어리로 함께 묶인다.
    // 스폰 시 spawnWeight 가중치로 추첨된다(서브시스템 4). 공격 파라미터는 서브시스템 2에서 추가.
    [CreateAssetMenu(fileName = "ZombieTier", menuName = "Enemies/Zombie Tier", order = 0)]
    public class ZombieTierSO : ScriptableObject
    {
        public string tierName = "Small";

        [Tooltip("프리팹 기준 균등 스케일 배수.")]
        public float scale = 1f;

        [Tooltip("Visual 아래 Body의 로컬 Y 위치. 티어 크기에 맞춰 발이 바닥에 닿도록 조정.")]
        public float bodyY;

        [Tooltip("이 티어의 최대 체력.")]
        public float maxHealth = 30f;

        [Tooltip("스폰 지점 중심의 보이지 않는 이동 가능 원 반경.")]
        public float leashRadius = 8f;

        [Tooltip("스폰 추첨 가중치(클수록 자주 등장).")]
        [Min(0f)] public float spawnWeight = 1f;

        [Header("보상")]
        [Tooltip("이 티어의 좀비를 처치했을 때 지급하는 재화.")]
        [Min(0)] public int reward = 5;

        [Header("공격(던지기) — 서브시스템 2")]
        [Tooltip("던지기 쿨다운(초). 티어마다 다르게 설정 가능.")]
        [Min(0f)] public float throwCooldown = 2.5f;

        [Tooltip("던진 무기가 미사일에 주는 데미지. 큰 티어일수록 크게.")]
        [Min(0f)] public float throwDamage = 10f;
        
        [Tooltip("사거리.")]
        [Min(0f)] public float detectRadius = 50;
    }
}
