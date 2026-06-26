using System.Collections.Generic;
using Core;
using MVP.System.BaseMVP;

namespace UI.Popup.Result
{
    public class ResultModel : IModel
    {
        public IReadOnlyList<TierKillResult> TierResults { get; private set; }
            = new List<TierKillResult>();

        public int TotalCurrency { get; private set; }

        public void SetResults(List<TierKillResult> results, int total)
        {
            TierResults    = results;
            TotalCurrency  = total;
        }
    }
}
