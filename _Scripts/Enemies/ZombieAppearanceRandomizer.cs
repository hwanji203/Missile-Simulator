using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Enemies
{
    // Awake 에서 자식 SkinnedMeshRenderer 를 모두 캐시하고,
    // OnEnable 마다(풀 재사용 포함) 슬롯별 랜덤 외형을 적용한다.
    //
    // 사용법:
    //  1) 이 컴포넌트를 좀비 비주얼 루트(SkinnedMeshRenderer 를 자식으로 가진 오브젝트)에 부착.
    //  2) slots 배열에 슬롯 추가:
    //     slotName  : "Body" / "Head" / "Hair" / "Hat" / "Eyes" / "Eyelids" /
    //                 "Pants" / "Shoes" / "Outwear" / "Overall" / "Gloves" 등
    //     variants  : ithappy Prefabs/ 폴더의 파트 프리팹 (Body_A_1, Hair_5 … 등)
    //     enableProbability : 1 = 항상, 0.5 = 50% 확률 적용 (optional 파츠용)
    public class ZombieAppearanceRandomizer : MonoBehaviour
    {
        [Serializable]
        public class SlotData
        {
            [Tooltip("Body / Head / Hair / Hat / Eyes / Eyelids / Pants / Shoes / Outwear / Overall / Gloves ...")]
            public string slotName;
            [Tooltip("ithappy Prefabs 폴더의 파트 프리팹 목록")]
            public GameObject[] variants;
            [Range(0f, 1f), Tooltip("1 = 항상 적용, 0.5 = 50% 확률 적용 (optional 파츠)")]
            public float enableProbability = 1f;
        }

        [SerializeField] private SlotData[] slots;

        // Awake 에서 한 번만 탐색해 캐시. 이후 OnEnable 마다 이 목록으로 렌더러를 찾는다.
        private SkinnedMeshRenderer[] _cachedRenderers;

        private void Awake()
        {
            _cachedRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        }

        private void OnEnable()
        {
            if (slots == null) return;
            foreach (var slot in slots)
                ApplySlot(slot);
        }

        private void ApplySlot(SlotData slot)
        {
            if (slot.variants == null || slot.variants.Length == 0) return;

            var dst = FindSlotRenderer(slot.slotName);

            if (Random.value > slot.enableProbability)
            {
                if (dst != null) dst.sharedMesh = null;
                return;
            }

            var variant = slot.variants[Random.Range(0, slot.variants.Length)];
            if (variant == null || dst == null) return;

            var src = variant.GetComponentInChildren<SkinnedMeshRenderer>();
            if (src == null) return;

            dst.sharedMesh = src.sharedMesh;
            dst.sharedMaterials = src.sharedMaterials;
        }

        // 캐시된 렌더러 중에서 슬롯명과 이름이 매칭되는 것을 반환한다.
        // CharacterCustomizationWindow.SavePrefab() 과 동일한 규칙:
        //  ① 슬롯명 끝 's' 제거 (Eyelids → Eyelid)
        //  ② 렌더러 이름에서 '_' 제거 후 ① 로 시작하면 매칭
        private SkinnedMeshRenderer FindSlotRenderer(string slotName)
        {
            string n = slotName.EndsWith("s") ? slotName[..^1] : slotName;
            foreach (var r in _cachedRenderers)
            {
                if (r == null) continue;
                string flat = string.Join("", r.name.Split('_'));
                if (flat.StartsWith(n, StringComparison.OrdinalIgnoreCase))
                    return r;
            }
            return null;
        }
    }
}
