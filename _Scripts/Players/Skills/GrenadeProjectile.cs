using CombatSystem;
using EventChannelSystem;
using Events;
using ObjectPool.Runtime;
using UnityEngine;

namespace Players.Skills
{
    // 미사일이 비행 중 떨어뜨리는 수류탄(풀링 대상). 중력으로 낙하하다
    // 착탄(whatStopsGrenade) 또는 신관시간(fuseTime) 만료 중 먼저 오는 쪽에서 폭발한다.
    // 폭발은 메인 폭발 캐스터의 효과값×ratio(CastScaledAt) — 상태성 이벤트는 내지 않고 VFX만 낸다.
    [RequireComponent(typeof(Rigidbody))]
    public class GrenadeProjectile : AbstractMonoPoolable
    {
        [SerializeField] private LayerMask whatStopsGrenade; // 착탄 판정 레이어(땅+적)
        [SerializeField] private float fuseTime = 1.5f;      // 착탄 전 자동 폭발까지 시간(초)
        [SerializeField] private PoolItemSO explosionVfx;    // 폭발 VFX(풀)

        private Rigidbody _rb;
        private ExplosionDamageCaster _caster;
        private PoolManagerSO _pool;
        private EventChannelSO _createChannel;
        private float _ratio;
        private float _elapsed;
        private bool _active;

        private void Awake() => _rb = GetComponent<Rigidbody>();

        // 풀에서 꺼낼 때 상태 초기화.
        public override void ResetItem()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            _active = false;
            _elapsed = 0f;
        }

        // 발사: 메인 캐스터/비율/풀/VFX 채널을 주입받고 초기 속도로 던진다.
        public void Launch(ExplosionDamageCaster mainCaster, float ratio, Vector3 velocity,
            PoolManagerSO pool, EventChannelSO createChannel)
        {
            _caster = mainCaster;
            _ratio = ratio;
            _pool = pool;
            _createChannel = createChannel;
            _elapsed = 0f;
            _active = true;

            _rb.isKinematic = false;
            _rb.useGravity = true;
            _rb.linearVelocity = velocity;
            _rb.angularVelocity = Vector3.zero;
        }

        private void Update()
        {
            if (!_active) return;
            if ((_elapsed += Time.deltaTime) >= fuseTime)
                Explode();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!_active) return;
            if ((whatStopsGrenade.value & (1 << collision.gameObject.layer)) == 0) return;
            Explode();
        }

        // 비율 폭발 + VFX → 풀 반납. 메인 캐스터가 사라졌으면(미사일 선폭발) 데미지는 건너뛰고 반납만.
        private void Explode()
        {
            if (!_active) return;
            _active = false;

            Vector3 pos = transform.position;
            if (_caster != null)
                _caster.CastScaledAt(pos, _ratio);

            if (_createChannel != null && explosionVfx != null)
                _createChannel.RaiseEvent(
                    CreateEvents.ShowPoolingVfxEvent.InitData(explosionVfx, pos, Quaternion.identity));

            _pool?.Push(this);
        }
    }
}
