using System.Collections.Generic;
using Core;
using DG.Tweening;
using MVP.Forms;
using MVP.System.AbstractMVP;
using TMPro;
using UnityEngine;

namespace UI.Popup.Result
{
    public class ResultView : AbstractPopupView
    {
        [SerializeField] private Transform      rowContainer;
        [SerializeField] private ResultRowItem  rowPrefab;
        [SerializeField] private TextMeshProUGUI totalText;
        [SerializeField] public  ButtonForm     restartButton;
        [SerializeField] public  ButtonForm     homeButton;

        private readonly List<ResultRowItem> _rows = new();

        public void ShowRows(IReadOnlyList<TierKillResult> results)
        {
            foreach (ResultRowItem r in _rows) Destroy(r.gameObject);
            _rows.Clear();

            if (rowContainer == null || rowPrefab == null) return;

            foreach (TierKillResult data in results)
            {
                ResultRowItem row = Instantiate(rowPrefab, rowContainer);
                row.Populate(data);
                _rows.Add(row);
            }
        }

        public void PlayCountUp(int target, float duration)
        {
            if (totalText == null) return;
            int current = 0;
            DOTween.To(() => current, x =>
            {
                current = x;
                totalText.text = $"{x}G";
            }, target, duration).SetUpdate(true);
        }
    }
}
