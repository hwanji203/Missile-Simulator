// using System.Collections;
// using DG.Tweening;
// using EventChannelSystem;
// using Events;
// using MVP.System.BaseMVP;
// using UnityEngine;
// using UnityEngine.UI;
//
// namespace Game.UI.Popup.Warning
// {
//     public class SuperBoostWarningPresenter : BasePresenter<SuperBoostModel, SuperBoostView>
//     {
//         [SerializeField] private EventChannelSO playerChannel;
//         [SerializeField] private float displayDuration = 3;
//         [SerializeField] private Image warningImage;
//
//         private Sequence _seq;
//         
//         public override void InitializePresenter()
//         {
//             base.InitializePresenter();
//             
//             playerChannel.AddListener<PlayerSuperBoostEvent>(CloseOnSuperBoost);
//         }
//
//         protected override void OnDestroy()
//         {
//             base.OnDestroy();
//             playerChannel.RemoveListener<PlayerSuperBoostEvent>(CloseOnSuperBoost);
//             if (_seq != null && _seq.IsActive()) _seq.Kill();
//         }
//
//         // 트리거(PlayerSuperBoostEvent 구독)는 UIOpener가 담당. UIManager가 Open을 호출한다.
//         public override void Open(object payload)
//         {
//             if (View.IsOpen) return;
//             base.Open(payload);
//             StartWarning();
//         }
//
//         private void StartWarning()
//         {
//             if (_seq != null && _seq.IsActive()) _seq.Kill();
//             
//             warningImage.DOFade(0, 0);
//             
//             _seq = DOTween.Sequence();
//             _seq.Append(warningImage.DOFade(1, displayDuration))
//                 .Append(warningImage.DOFade(0.25f, displayDuration))
//                 .Loops();
//         }
//
//         private void CloseOnSuperBoost(PlayerSuperBoostEvent evt)
//         {
//             if (View.IsOpen && evt.IsStarted) Close();
//         }
//     }
// }
