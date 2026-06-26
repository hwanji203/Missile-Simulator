using UnityEngine;

namespace Enemies.Projectiles
{
    // 던지기 윈드업 동안 LineRenderer로 포물선 궤적을 미리 보여준다.
    // 투사체와 같은 식 p(t) = origin + v·t + ½·g·t² 으로 샘플링하므로 실제 비행 경로와 일치한다.
    [RequireComponent(typeof(LineRenderer))]
    public class ThrowIndicator : MonoBehaviour
    {
        [SerializeField] private int sampleCount = 24;
        // 이 y좌표(월드 기준)에 궤적이 닿을 때까지만 선을 그린다.
        private float _stopY = -5f;

        [SerializeField] private LineRenderer line;
        private Vector3[] _samples;  // Show() 시점에 계산된 포물선 샘플 전체
        private float _endTime;      // 샘플이 커버하는 시간 범위

        private void Awake()
        {
            line.useWorldSpace = true;
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        // [임시 진단] 라인이 켜진 동안, 0번 점이 투사체에서 크게 벗어나거나 useWorldSpace가
        // 풀린 "엉뚱한 곳" 프레임만 출력한다. 깜빡임 원인 프레임을 잡으면 제거한다.
        private void LateUpdate()
        {
            if (line == null || !line.enabled || line.positionCount == 0) return;
            Vector3 p0 = line.GetPosition(0);
            float d = (p0 - transform.position).magnitude;
            if (!line.useWorldSpace || d > 2f)
                Debug.LogWarning($"[ThrowIndicator] f={Time.frameCount} worldSpace={line.useWorldSpace} " +
                                 $"pos0={p0} trans={transform.position} d={d:F2} count={line.positionCount} active={gameObject.activeInHierarchy}");
        }

        public void Show(Vector3 origin, Vector3 velocity, Vector3 gravity, float flightTime, float stopY)
        {
            if (line == null) line = GetComponent<LineRenderer>();
            // 풀 재사용/실행순서 엣지에서 한 프레임 로컬공간으로 해석돼 엉뚱한 곳으로 튀는 것을 막는다.
            line.useWorldSpace = true;

            float w = transform.lossyScale.x;
            line.startWidth = w;
            line.endWidth   = w;

            _stopY = stopY;
            _endTime = TimeToReachY(origin.y, velocity.y, gravity.y, _stopY, flightTime);

            int count = Mathf.Max(2, sampleCount);
            if (_samples == null || _samples.Length != count)
                _samples = new Vector3[count]; // 풀 재사용 시 매번 새로 할당하지 않는다

            line.positionCount = count;
            for (int i = 0; i < count; i++)
            {
                float t = _endTime * i / (count - 1);
                _samples[i] = origin + velocity * t + gravity * (0.5f * (t * t));
                line.SetPosition(i, _samples[i]);
            }
            line.enabled = true;
        }

        // 투사체의 현재 위치를 받아 이미 지나간 구간을 라인에서 제거한다.
        // 가장 가까운 샘플 이전 구간을 잘라내고 첫 점을 현재 위치로 교체한다.
        public void TrimToPosition(Vector3 currentPos)
        {
            if (_samples == null || !line.enabled) return;

            int closestIdx = 0;
            float minSqDist = float.MaxValue;
            for (int i = 0; i < _samples.Length; i++)
            {
                float d = (_samples[i] - currentPos).sqrMagnitude;
                if (d < minSqDist) { minSqDist = d; closestIdx = i; }
            }

            // closestIdx 이후 샘플 + 현재 위치를 첫 점으로
            int remaining = _samples.Length - closestIdx;
            if (remaining < 2) { Hide(); return; }

            line.positionCount = remaining;
            line.SetPosition(0, currentPos);
            for (int i = 1; i < remaining; i++)
                line.SetPosition(i, _samples[closestIdx + i]);
        }

        // y(t) = y0 + vy·t + ½·gy·t² 가 targetY에 닿는 (하강하며 도달하는) 가장 큰 양수 t를 구한다.
        private static float TimeToReachY(float y0, float vy, float gy, float targetY, float fallback)
        {
            // ½·gy·t² + vy·t + (y0 - targetY) = 0
            float a = 0.5f * gy;
            float b = vy;
            float c = y0 - targetY;

            if (Mathf.Abs(a) < 1e-6f)
            {
                // 중력이 없으면 선형: vy·t + c = 0
                if (Mathf.Abs(b) < 1e-6f) return fallback;
                float tLin = -c / b;
                return tLin > 0f ? tLin : fallback;
            }

            float disc = b * b - 4f * a * c;
            if (disc < 0f) return fallback; // 목표 높이에 절대 닿지 않음

            float sqrt = Mathf.Sqrt(disc);
            float t1 = (-b + sqrt) / (2f * a);
            float t2 = (-b - sqrt) / (2f * a);
            float t = Mathf.Max(t1, t2); // 하강하며 닿는 더 늦은 시점
            return t > 0f ? t : fallback;
        }

        public void Hide()
        {
            if (line == null) line = GetComponent<LineRenderer>();
            line.enabled = false;
            line.positionCount = 0;
        }
    }
}
