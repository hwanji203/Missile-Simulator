using System.Collections;
using EventChannelSystem;
using Events;
using UnityEngine;

namespace Players.Rendering
{
    public class TrySuperBoostRenderer : MonoBehaviour
    {
        [SerializeField] private EventChannelSO playerChannel;
        [SerializeField] private MeshRenderer missileRenderer;
        [SerializeField] private Color blinkColor = Color.red;
        [SerializeField] private float emission = 5;
        [SerializeField] private AnimationCurve emissionCurve;
        [SerializeField] private float speedMultiplier = 1.5f;
        [SerializeField] private float maxSpeed = 10f;
        [Tooltip("1인칭(Follow)일 때 부스트 Emission에 곱할 배율(0~1). 눈부심 방지용 부분 약화")]
        [SerializeField, Range(0f, 1f)] private float firstPersonEmissionScale = 0.25f;

        static readonly int EmissionID = Shader.PropertyToID("_EmissionColor");
        private Material _originMaterial;
        private Color _originColor;
        private Coroutine _blinkCoroutine;
        private bool _isFirstPerson;   // 기본 false = 3인칭(풀강도)

        // 1인칭이면 배율을 곱해 약화, 3인칭이면 풀강도.
        private float EffectiveEmission => _isFirstPerson ? emission * firstPersonEmissionScale : emission;

        private void Awake()
        {
            Debug.Assert(missileRenderer != null, "렌더러가 null 입니다.");

            _originMaterial = missileRenderer.material;
            _originColor = missileRenderer.material.GetColor(EmissionID);

            playerChannel.AddListener<PlayerSuperBoostEvent>(PlayerBoostHandler);
            playerChannel.AddListener<PlayerViewChangedEvent>(OnViewChanged);
        }

        private void OnDestroy()
        {
            playerChannel.RemoveListener<PlayerSuperBoostEvent>(PlayerBoostHandler);
            playerChannel.RemoveListener<PlayerViewChangedEvent>(OnViewChanged);

            if (_originMaterial != null)
                Destroy(_originMaterial);
        }

        private void OnViewChanged(PlayerViewChangedEvent evt) => _isFirstPerson = evt.IsFirstPerson;

        private void PlayerBoostHandler(PlayerSuperBoostEvent evt)
        {
            if (evt.IsPressed)
            {
                _blinkCoroutine = StartCoroutine(BlinkCoroutine());
            }
            else if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
                _blinkCoroutine = null;

                // 부스트 종료 시 emission을 꺼서 원래 상태로 복구
                _originMaterial.SetColor(EmissionID, Color.black);
            }
        }

        private IEnumerator BlinkCoroutine()
        {
            float speed = 1;

            while (true)
            {
                speed = Mathf.Min(speed * speedMultiplier, maxSpeed);

                float cur = 0;
                while (cur < 1)
                {
                    cur += Time.deltaTime * speed;
                    float emissionPercent = emissionCurve.Evaluate(cur / 2);
                    Color smoothColor = Color.Lerp(_originColor, blinkColor * EffectiveEmission, emissionPercent);
                    _originMaterial.SetColor(EmissionID, smoothColor);
                    yield return null;
                }
                _originMaterial.SetColor(EmissionID, blinkColor * EffectiveEmission);

                cur = 0;
                while (cur < 1)
                {
                    cur += Time.deltaTime * speed;
                    float emissionPercent = emissionCurve.Evaluate(cur / 2);
                    Color smoothColor = Color.Lerp(blinkColor * EffectiveEmission, _originColor, emissionPercent);
                    _originMaterial.SetColor(EmissionID, smoothColor);
                    yield return null;
                }
                _originMaterial.SetColor(EmissionID, _originColor);
            }
        }
    }
}