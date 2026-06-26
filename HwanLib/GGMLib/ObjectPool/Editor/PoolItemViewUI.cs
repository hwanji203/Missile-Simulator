using System;
using ObjectPool.Runtime;
using UnityEngine.UIElements;

namespace ObjectPool.Editor
{
    public class PoolItemViewUI
    {
        private Label _nameLabel;
        private Button _deleteBtn;
        private VisualElement _rootElement;
        private Label _warningLabel;

        public event Action<PoolItemViewUI> OnDeleteEvent;
        public event Action<PoolItemViewUI> OnSelectEvent;

        public string Name
        {
            get => _nameLabel.text;
            set => _nameLabel.text = value;
        }

        public PoolItemSO ItemSO { get; private set; }

        public bool IsActive
        {
            get => _rootElement.ClassListContains("active");
            //active라는 클래스를 가지고 있으면 true
            set => _rootElement.EnableInClassList("active", value);
            // true 가 들어오면 active를 껴주고
            // false면 빼줌
        }

        public bool IsEmpty
        {
            get => _warningLabel.ClassListContains("on");
            set => _warningLabel.EnableInClassList("on", value);
        }

        public PoolItemViewUI(VisualElement root, PoolItemSO itemSo)
        {
            ItemSO = itemSo;
            _rootElement = root.Q("PoolItem");
            _nameLabel = _rootElement.Q<Label>("ItemName");
            _deleteBtn = _rootElement.Q<Button>("DeleteBtn");
            _warningLabel = _rootElement.Q<Label>("WarningLabel");
            
            _deleteBtn.RegisterCallback<ClickEvent>(evt =>
            {
                OnDeleteEvent?.Invoke(this);
                evt.StopPropagation();
                //아래로 전파되는 이벤트 중단
            });
            
            _rootElement.RegisterCallback<ClickEvent>(evt =>
            {
                OnSelectEvent?.Invoke(this);
                evt.StopPropagation();
            });
        }
    }
}