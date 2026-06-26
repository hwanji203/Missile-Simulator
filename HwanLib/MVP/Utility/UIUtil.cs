using System.Collections;
using UnityEngine;

namespace MVP.Utility
{
    public static class UIUtil
    {
        public static IEnumerator Fade(CanvasGroup group, float alpha, float duration)
        {
            var time = 0.0f;
            var originalAlpha = group.alpha;
            while (time < duration)
            {
                time += Time.deltaTime;
                group.alpha = Mathf.Lerp(originalAlpha, alpha, time / duration);
                yield return new WaitForEndOfFrame();
            }

            group.alpha = alpha;
        }

        public static void SetPivotWithoutScreenPosChange(this RectTransform rectTrm, Vector2 targetPivot)
        {
            if (rectTrm.pivot == targetPivot)
                return;
            
            Vector2 deltaPivot = targetPivot - rectTrm.pivot;
            Vector2 sizeDelta = rectTrm.sizeDelta;
            
            rectTrm.pivot = targetPivot;
            
            Vector2 deltaPosition = new Vector2(deltaPivot.x * sizeDelta.x, deltaPivot.y * sizeDelta.y);
            rectTrm.anchoredPosition += deltaPosition;
        }
    }
}