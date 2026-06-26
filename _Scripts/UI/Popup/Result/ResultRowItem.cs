using Core;
using TMPro;
using UnityEngine;

namespace UI.Popup.Result
{
    // 결과 UI 한 행: "좀비 소형  ×  5마리  =  25G"
    public class ResultRowItem : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tierText;
        [SerializeField] private TextMeshProUGUI killsText;
        [SerializeField] private TextMeshProUGUI subtotalText;

        public void Populate(TierKillResult data)
        {
            if (tierText     != null) tierText.text     = data.TierName;
            if (killsText    != null) killsText.text    = $"x {data.Kills}";
            if (subtotalText != null) subtotalText.text = $"{data.Subtotal}G";
        }
    }
}
