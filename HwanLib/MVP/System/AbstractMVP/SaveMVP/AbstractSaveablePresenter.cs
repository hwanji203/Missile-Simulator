using EventChannelSystem;
using MVP.System.BaseMVP;
using UnityEngine;

namespace MVP.System.AbstractMVP.SaveMVP
{
    public abstract class AbstractSaveablePresenter<TModel, TView>
        : BasePresenter<TModel, TView>
        where TModel : ISaveableModel, new()
        where TView : BaseView
    {
        [SerializeField] protected EventChannelSO saveChannel;

        private bool _isBroadcasting;

        protected string SaveKey => typeof(TModel).Name;

        private ISaveableModel SaveModel => (ISaveableModel)Model;

        public override void InitializePresenter()
        {
            base.InitializePresenter();

            saveChannel.AddListener<ModelSyncEvent>(OnSyncReceived);

            if (PlayerPrefs.HasKey(SaveKey))
                SaveModel.RestoreData(PlayerPrefs.GetString(SaveKey));

            OnClosed += HandleSaveOnClosed;
        }

        private void HandleSaveOnClosed()
        {
            string data = SaveModel.StoreData();
            PlayerPrefs.SetString(SaveKey, data);
            PlayerPrefs.Save();

            _isBroadcasting = true;
            saveChannel.RaiseEvent(SaveMVPEvents.ModelSyncEvent.Init(SaveKey, data));
            _isBroadcasting = false;
        }

        private void HandleSaveOnClosed(BasePresenter _)
            => HandleSaveOnClosed();

        private void OnSyncReceived(ModelSyncEvent evt)
        {
            if (_isBroadcasting) return;
            if (evt.SaveKey != SaveKey) return;

            SaveModel.RestoreData(evt.Data);
            if (View.IsOpen) View.UpdateView();
        }

        protected void SaveManually() => HandleSaveOnClosed();

        protected override void OnDestroy()
        {
            base.OnDestroy();
            OnClosed -= HandleSaveOnClosed;
            saveChannel.AddListener<ModelSyncEvent>(OnSyncReceived);
        }
    }
}
