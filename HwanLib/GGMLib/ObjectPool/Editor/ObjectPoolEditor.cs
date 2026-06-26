using System;
using System.Collections.Generic;
using System.IO;
using ObjectPool.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Path = System.IO.Path;

namespace ObjectPool.Editor
{
    public class ObjectPoolEditor : EditorWindow
    {
        [SerializeField] private VisualTreeAsset visualAsset = default;
        [SerializeField] private PoolManagerSO poolManagerAsset = default;
        [SerializeField] private VisualTreeAsset itemAsset = default;

        private string _rootFolder;
        private Button _createButton;
        private ScrollView _itemView;

        private List<PoolItemViewUI> _itemUiList;
        private PoolItemViewUI _selectedItemUI;

        private UnityEditor.Editor _cachedEditor;
        private VisualElement _inspector;
        
        [MenuItem("Tools/PoolManager")]
        public static void ShowExample()
        {
            ObjectPoolEditor wnd = GetWindow<ObjectPoolEditor>();
            wnd.titleContent = new GUIContent("ObjectPoolEditor");
        }

        #region Utility section

        private string GetCurrentDirectory()
        {
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            return Path.GetDirectoryName(scriptPath);
        }

        private void InitializeRootFolder()
        {
            string dirName = GetCurrentDirectory();
            DirectoryInfo parentDir = Directory.GetParent(dirName);
            Debug.Assert(parentDir != null, $"부모 디렉터리가 없습니다. {dirName}");

            string dataPath = Application.dataPath;
            _rootFolder = parentDir.FullName.Replace('\\', '/');
            if (_rootFolder.StartsWith(dataPath))
            {
                _rootFolder = $"Assets{_rootFolder.Substring(dataPath.Length)}";
            }
        }

        #endregion
        public void CreateGUI()
        {
            InitializeRootFolder();
            VisualElement root = rootVisualElement;
            if (visualAsset == null)
            {
                string dirName = GetCurrentDirectory();
                visualAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirName}/ObjectPoolEditor.uxml");
            }

            visualAsset.CloneTree(root);
            InitializeItems(root);
            GeneratePoolingItemUI();
        }


        private void InitializeItems(VisualElement root)
        {
            _createButton = root.Q<Button>("CreateBtn");
            _createButton.clicked += HandleCreateItem;
            _itemView = root.Q<ScrollView>("ItemView");
            
            _itemView.Clear();
            _itemUiList = new();
            _inspector = root.Q<VisualElement>("InspectorView");
        }

        private void GeneratePoolingItemUI()
        {
            _itemView.Clear();
            _itemUiList.Clear();
            _inspector.Clear();

            if (poolManagerAsset == null)
            {
                string filePath = $"{_rootFolder}/PoolManager.asset";
                poolManagerAsset = AssetDatabase.LoadAssetAtPath<PoolManagerSO>(filePath);
                if (poolManagerAsset == null)
                {
                    Debug.LogWarning("풀매니저 에셋이 없어서 새로 만들어집니다.");
                    poolManagerAsset = ScriptableObject.CreateInstance<PoolManagerSO>();
                    AssetDatabase.CreateAsset(poolManagerAsset, filePath);
                }
            }

            if (itemAsset == null)
            {
                string dirName = GetCurrentDirectory();
                itemAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{dirName}/PoolItem View UI.uxml");
            }
            
            foreach (PoolItemSO item in poolManagerAsset.itemList)
            {
                TemplateContainer container = itemAsset.Instantiate();
                // USS까지 들고있는 VisualElement
                PoolItemViewUI itemViewUI = new PoolItemViewUI(container, item);
                _itemUiList.Add(itemViewUI); // UI에는 아이템으로 넣어주고
                _itemView.Add(container);    // 스크롤 뷰에는 컨테이너로 넣어준다

                itemViewUI.Name = item.itemName;
                itemViewUI.IsEmpty = item.prefab == null;
                itemViewUI.IsActive = false;

                itemViewUI.OnSelectEvent += HandleSelectionEvent;
                itemViewUI.OnDeleteEvent += HandleDeleteEvent;
            }
        }

        #region  Event Handling

            private void HandleSelectionEvent(PoolItemViewUI targetUI)
            {
                if (_selectedItemUI != null)
                {
                    _selectedItemUI.IsActive = false;
                }

                _selectedItemUI = targetUI;
                _selectedItemUI.IsActive = true;
                
                _inspector.Clear();
                UnityEditor.Editor.CreateCachedEditor(_selectedItemUI.ItemSO, null, ref _cachedEditor);
                VisualElement inspectorElement = _cachedEditor.CreateInspectorGUI();

                SerializedObject so = new SerializedObject(_selectedItemUI.ItemSO);
                inspectorElement.Bind(so);
                inspectorElement.TrackSerializedObjectValue(so, serializeObject =>
                {
                    //직렬화 되었으면 모든 데이터가 문자열로 되니까 FindProperty는 무조건 object를 반환함. 원하는거 체크해야함
                    _selectedItemUI.Name = serializeObject.FindProperty("itemName").stringValue;
                    _selectedItemUI.IsEmpty = serializeObject.FindProperty("prefab").objectReferenceValue == null;
                });
                //직렬화된 오브젝트 값이 바뀌는걸 추적함 
                _inspector.Add(inspectorElement);
            }
            
            private void HandleDeleteEvent(PoolItemViewUI targetUI)
            {
                poolManagerAsset.itemList.Remove(targetUI.ItemSO);
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(targetUI.ItemSO));
                EditorUtility.SetDirty(poolManagerAsset);
                AssetDatabase.SaveAssets();

                if (targetUI == _selectedItemUI)
                {
                    _selectedItemUI = null;
                    
                }
                
                GeneratePoolingItemUI();
            }
            
        #endregion

        

        private void HandleCreateItem()
        {
            Guid itemGuid = Guid.NewGuid();
            PoolItemSO item = ScriptableObject.CreateInstance<PoolItemSO>();
            item.itemName = itemGuid.ToString();

            if (Directory.Exists($"{_rootFolder}/Items") == false)
            {
                Directory.CreateDirectory($"{_rootFolder}/Items");
            }
            
            AssetDatabase.CreateAsset(item, $"{_rootFolder}/Items/{item.itemName}.asset");
            poolManagerAsset.itemList.Add(item);
            
            EditorUtility.SetDirty(poolManagerAsset);
            AssetDatabase.SaveAssets();
            
            GeneratePoolingItemUI();
        }
    }
}
