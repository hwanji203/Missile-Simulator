using UnityEngine;

namespace MVP.Forms.Module.Fade
{
    // 씬 전환 모션 1종 = 머티리얼 1개 = 이 에셋 1개.
    // FadeForm은 preset만 받아 머티리얼을 교체하고 _Cutoff를 보간한다(효과별 분기 없음).
    // 방향/파라미터(_Angle/_Invert 등)는 머티리얼에 구워두고, 변형은 별 preset 에셋으로 둔다.
    [CreateAssetMenu(fileName = "TransitionPreset", menuName = "Transition/Preset")]
    public class TransitionPreset : ScriptableObject
    {
        public Material material;                 // 효과 셰이더 머티리얼 (방향/파라미터 구워둠)
        [Min(0.1f)] public float duration = 1f;   // 아웃+인 전체. 각 방향 duration/2
        [Min(0f)]   public float middleDelay = 0f;
        public AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("방향성 효과(Wipe 등)에서 켜면 들어올 때 _Angle을 +π 해서 같은 방향으로 계속 닦아 나간다(되돌아가지 않음). " +
                 "Fade처럼 방향 없는 효과는 끈다. 머티리얼에 _Angle 프로퍼티가 있어야 함.")]
        public bool flipAngleOnIn = false;
    }
}
