using CombatSystem;
using Core;
using EventChannelSystem;
using Events;
using ModuleSystem;
using Players;
using UnityEngine;
using NotImplementedException = System.NotImplementedException;

namespace UI.Popup.Meta
{
    // 게임 씬 시작 시 메타 버프(체력/퓨즈 시간)를 플레이어에게 적용한다.
    // PlayerPrefs에서 직접 읽어 Presenter 초기화 순서에 의존하지 않는다.
    public class MetaBuffApplier : MonoBehaviour, IModule
    {
        [SerializeField] private EventChannelSO gameChannel;
        [SerializeField] private PlayerDefaultStatusSO defaultStatus;
        [SerializeField] private MetaBuffSO   buffDef;
        [SerializeField] private AbstractDamageCaster damageCaster;
        
        private HealthModule _playerHealth;
        private MissileFuse  _missileFuse;

        public void Initialize(ModuleOwner owner)
        {
            if (buffDef == null) return;

            _playerHealth = owner.GetModule<HealthModule>();
            _missileFuse = owner.GetModule<MissileFuse>();
            
            gameChannel.AddListener<GameStartEvent>(GameStartHandler);
        }

        private void OnDestroy()
        {
            gameChannel.RemoveListener<GameStartEvent>(GameStartHandler);
        }

        private void GameStartHandler(GameStartEvent obj)
        {
            MetaProgressData data = MetaProgressModel.ReadBuffLevelsFromPrefs();

            float bonus = buffDef.healthPerLevel * data.healthBuffLevel;
            _playerHealth.SetMaxHealth(defaultStatus.DefaultHp + bonus);

            _missileFuse.SetSpareTime(buffDef.spareTimePerLevel * data.spareTimeBuffLevel);

            damageCaster.AddedDamage = buffDef.explosionDamagePerLevel * data.explosionDamageBuffLevel;
            damageCaster.AddedRange = buffDef.explosionRangePerLevel * data.explosionRangeBuffLevel;
        }
    }
}
