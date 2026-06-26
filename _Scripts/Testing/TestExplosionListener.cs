using EventChannelSystem;
using Events;
using UnityEngine;

namespace Testing
{
    // 폭발 이벤트(PlayerExplodedEvent)를 구독해서, 콘솔 로그와 임시 구체로
    // 폭발이 일어난 위치를 눈으로 확인하게 해주는 테스트용 리스너.
    public class TestExplosionListener : MonoBehaviour
    {
        [SerializeField] private EventChannelSO eventChannel;   //MissileSkillModule에 넣은 것과 같은 채널
        [SerializeField] private float showRadius = 3f;         //보여줄 구체 반경 (SkillDataSO.skillRange와 맞추면 보기 좋음)
        [SerializeField] private float showSeconds = 0.5f;      //구체 표시 유지 시간

        private void OnEnable()
        {
            if (eventChannel != null)
                eventChannel.AddListener<PlayerExplodedEvent>(HandleExploded);
        }

        private void OnDisable()
        {
            if (eventChannel != null)
                eventChannel.RemoveListener<PlayerExplodedEvent>(HandleExploded);
        }

        private void HandleExploded(PlayerExplodedEvent evt)
        {
            Debug.Log($"<color=orange>[폭발]</color> 위치: {evt.Position}");
            ShowSphere(evt.Position);
        }

        // 충돌/물리에 영향 주지 않는 반투명용 임시 구체를 잠깐 띄운다.
        private void ShowSphere(Vector3 position)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "ExplosionGizmo(Test)";
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * (showRadius * 2f);

            Collider col = sphere.GetComponent<Collider>();
            if (col != null) Destroy(col); //다른 물체와 충돌하지 않도록 콜라이더 제거

            Destroy(sphere, showSeconds);
        }
    }
}
