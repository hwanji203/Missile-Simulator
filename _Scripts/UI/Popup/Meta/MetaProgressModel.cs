using Core;
using MVP.System.AbstractMVP.SaveMVP;
using MVP.UIData;
using UnityEngine;

namespace UI.Popup.Meta
{
    public class MetaProgressModel : ISaveableModel
    {
        // private 
        public MetaBuffSO BuffSO { get; set; }
        private MetaProgressData _data = new MetaProgressData();
        
        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            _data.totalCurrency += amount;
        }
        
        public void HealthBtnClickHandler(UIParam _)   
        {
            if (BuffSO.TryBuy(ref _data.totalCurrency, _data.healthBuffLevel))
                _data.healthBuffLevel++;
        } 
        
        public void SpareTimeBtnClickHandler(UIParam _) 
        {
            if (BuffSO.TryBuy(ref _data.totalCurrency, _data.spareTimeBuffLevel))
                _data.spareTimeBuffLevel++;
        }
        
        public void ExplosionDamageBtnClickHandler(UIParam _) 
        {
            if (BuffSO.TryBuy(ref _data.totalCurrency, _data.explosionDamageBuffLevel))
                _data.explosionDamageBuffLevel++;
        } 
        
        public void ExplosionRangeBtnClickHandler(UIParam _) 
        {
            if (BuffSO.TryBuy(ref _data.totalCurrency, _data.explosionRangeBuffLevel))
                _data.explosionRangeBuffLevel++;
        } 

        public UIParam UpdateCurrency() 
            => UIParams.UIStringParam.Init(_data.totalCurrency.ToString());
        
        public UIParam UpdateHealthBar() 
            => UIParams.UIBarParam.Init(BuffSO.needGoldPerLevels.Count, _data.healthBuffLevel);

        public UIParam UpdateSpareTimeBar()
            => UIParams.UIBarParam.Init(BuffSO.needGoldPerLevels.Count
            , _data.spareTimeBuffLevel);
        
        public UIParam UpdateExplosionDamageBar()
            => UIParams.UIBarParam.Init(BuffSO.needGoldPerLevels.Count
            , _data.explosionDamageBuffLevel);
        
        public UIParam UpdateExplosionRangeBar()
            => UIParams.UIBarParam.Init(BuffSO.needGoldPerLevels.Count
            , _data.explosionRangeBuffLevel);

        // 좌하단 상시 수치(D)와 우상단 호버(E)가 공유하는 단일 매핑. "현재값(+증가분)" 값 부분만.
        public string GetStatHoverText(MetaStatType stat) => stat switch
        {
            MetaStatType.Health          => BuffSO.FormatStat(BuffSO.baseHealth,          BuffSO.healthPerLevel,          _data.healthBuffLevel),
            MetaStatType.SpareTime       => BuffSO.FormatStat(BuffSO.baseSpareTime,       BuffSO.spareTimePerLevel,       _data.spareTimeBuffLevel),
            MetaStatType.ExplosionDamage => BuffSO.FormatStat(BuffSO.baseExplosionDamage, BuffSO.explosionDamagePerLevel, _data.explosionDamageBuffLevel),
            MetaStatType.ExplosionRange  => BuffSO.FormatStat(BuffSO.baseExplosionRange,  BuffSO.explosionRangePerLevel,  _data.explosionRangeBuffLevel),
            _                            => string.Empty,
        };

        // 좌하단 미사일 수치 텍스트(값 부분만, 라벨은 prefab 정적 텍스트). "현재값(+증가분)".
        public UIParam UpdateHealthStatText()
            => UIParams.UIStringParam.Init(GetStatHoverText(MetaStatType.Health));

        public UIParam UpdateSpareTimeStatText()
            => UIParams.UIStringParam.Init(GetStatHoverText(MetaStatType.SpareTime));

        public UIParam UpdateExplosionDamageStatText()
            => UIParams.UIStringParam.Init(GetStatHoverText(MetaStatType.ExplosionDamage));

        public UIParam UpdateExplosionRangeStatText()
            => UIParams.UIStringParam.Init(GetStatHoverText(MetaStatType.ExplosionRange));

        public string StoreData() => JsonUtility.ToJson(_data);

        public void RestoreData(string data)
        {
            _data = string.IsNullOrEmpty(data)
                ? new MetaProgressData()
                : JsonUtility.FromJson<MetaProgressData>(data);
        }
        
        // MetaBuffApplier가 PlayerPrefs에서 직접 읽을 때 사용하는 정적 헬퍼.
        public static MetaProgressData ReadBuffLevelsFromPrefs()
        {
            string json = PlayerPrefs.GetString(nameof(MetaProgressModel), "");
            if (string.IsNullOrEmpty(json)) return new();
            return JsonUtility.FromJson<MetaProgressData>(json);
        }
        
        #if UNITY_EDITOR
        public void ClearData()
        {
            PlayerPrefs.DeleteKey(nameof(MetaProgressModel));
        }
        #endif
    }
}
