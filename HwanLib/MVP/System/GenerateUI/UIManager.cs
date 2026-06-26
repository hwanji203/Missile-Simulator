using System;
using System.Collections.Generic;
using EventChannelSystem;
using MVP.System.BaseMVP;
using MVP.Utility;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace MVP.System.GenerateUI
{
    public class UIManager : LightSingleton<UIManager>
    {
        [SerializeField] private UIRegistrySO   registry;
        [SerializeField] private UIScenePolicySO scenePolicy;
        [SerializeField] private EventChannelSO uiChannel;
        [SerializeField] private WorldUICamera  worldUICamera;
        [SerializeField] private LayerMask      uiLayer = 1 << 5;
        [SerializeField] private EventSystem    eventSystem;

        // Get<T>() 용 단일 인스턴스 타입 조회
        private readonly Dictionary<Type, BasePresenter>  _byType     = new();
        // 이벤트 라우팅 용 단일 인스턴스 (id != None)
        private readonly Dictionary<UIId, BasePresenter>  _single     = new();
        // Multiable 풀
        private readonly Dictionary<UIId, MultiablePool>  _pools      = new();
        // 모달 스택 (Popup/System 레이어, top이 최상위)
        private readonly Stack<BasePresenter>             _modalStack = new();

        protected override void Initialize()
        {
            base.Initialize();
            Instantiate(worldUICamera, Camera.main?.transform);
            eventSystem.gameObject.SetActive(true);

            var cameraData = Camera.main?.GetComponent<UniversalAdditionalCameraData>();
            Camera worldCam = cameraData?.cameraStack.Find(c => c.cullingMask == uiLayer.value);

            foreach (GameObject prefab in registry.Prefabs)
                SpawnEntry(prefab, worldCam);

            uiChannel.AddListener<OpenUIEvent>(HandleOpen);
            uiChannel.AddListener<CloseUIEvent>(HandleClose);
            uiChannel.AddListener<ToggleUIEvent>(HandleToggle);

            // 씬 전환마다 전체 리셋 + 시작-셋 적용. 초기(active) 씬도 같은 경로로 처리.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void SpawnEntry(GameObject prefab, Camera worldCam)
        {
            // 프리팹 컴포넌트를 직접 읽어 config 파악 (instantiate 없이)
            var config = prefab.GetComponent<BasePresenter>();
            if (config == null)
            {
                Debug.LogError($"UI 프리팹에 BasePresenter 없음: {prefab.name}");
                return;
            }

            if (config.MultiableCount > 0)
            {
                // Multiable: factory로 N개 초기 생성
                var pool = new MultiablePool(
                    () => CreateInstance(prefab, worldCam),
                    config.MultiableCount
                );
                _pools[config.Id] = pool;
            }
            else
            {
                BasePresenter presenter = CreateInstance(prefab, worldCam);
                _byType[presenter.GetType()] = presenter;
                if (presenter.Id != UIId.None)
                    _single[presenter.Id] = presenter;
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ApplyScenePolicy(scene.name);

        // 씬 전환 = 전체 리셋: 열린 UI 전부 Close 후, 새 씬의 시작-셋만 Open(단일 UI 한정).
        private void ApplyScenePolicy(string sceneName)
        {
            // 1) 열린 단일 Presenter 전부 Close (Close → OnClosed → 모달 스택 정리).
            //    단, 씬 전환 연출용(Fade 등 PersistsAcrossSceneReset)은 닫지 않고 유지.
            foreach (BasePresenter p in _single.Values)
                if (p.IsOpen && !p.PersistsAcrossSceneReset) p.Close();

            // 2) 활성 풀 인스턴스 전부 Close (Close → OnClosed → pool.Release).
            foreach (MultiablePool pool in _pools.Values)
                foreach (BasePresenter p in pool.SnapshotActive())
                    p.Close();

            _modalStack.Clear();

            // 3) 새 씬의 시작-셋만 Open. 시작-시-열림은 단일 UI만 대상(풀 Id는 무시).
            if (scenePolicy == null)
            {
                Debug.LogWarning("[UIManager] scenePolicy 미할당 — 시작-시-열림을 건너뜁니다.");
                return;
            }

            foreach (UIId id in scenePolicy.GetOpenOnStart(sceneName))
                if (_single.TryGetValue(id, out BasePresenter single))
                    single.Open<UIManager>(null);
        }

        // 프리팹 인스턴스화 + 초기화 + 카메라 할당 + OnClosed 구독.
        private BasePresenter CreateInstance(GameObject prefab, Camera worldCam)
        {
            GameObject go = Instantiate(prefab, transform);
            BasePresenter p = go.GetComponent<BasePresenter>();
            p.InitializePresenter();
            UICameraStack.AssignWorldCamera(p, worldCam);
            p.OnClosed += OnPresenterClosed;
            return p;
        }

        private void HandleOpen(OpenUIEvent evt)
        {
            if (evt.Id == UIId.None) return;

            if (_pools.TryGetValue(evt.Id, out MultiablePool pool))
            {
                BasePresenter target = pool.Acquire();
                if (!target.CanOpen) { pool.Release(target); return; }
                target.SortingOrder = (int)target.Layer + pool.ActiveCount;
                target.Open(evt.Payload);
            }
            else if (_single.TryGetValue(evt.Id, out BasePresenter single))
            {
                OpenSingle(single, evt.Payload);
            }
            else
            {
                Debug.LogWarning($"[UIManager] UIId.{evt.Id} 미등록");
            }
        }

        // 단일 UI 열기의 공통 경로. 이미 열린 UI 재오픈 시 모달 스택 중복 push를 막고
        // payload만 다시 전달(예: 토스트 내용 교체).
        private void OpenSingle(BasePresenter single, object payload)
        {
            if (!single.CanOpen) return;
            if (!single.IsOpen) AssignModalSortingOrder(single);
            single.Open(payload);
        }

        // 토글: 열려 있으면 닫고, 아니면 OpenSingle 경로로 연다.
        private void HandleToggle(ToggleUIEvent evt)
        {
            if (evt.Id == UIId.None) return;
            if (!_single.TryGetValue(evt.Id, out BasePresenter single)) return;

            if (single.IsOpen) single.Close();
            else OpenSingle(single, null);
        }

        // 외부에서 id로 특정 단일/모달 UI를 닫는 보조 API.
        private void HandleClose(CloseUIEvent evt)
        {
            if (evt.Id == UIId.None) return;
            if (_single.TryGetValue(evt.Id, out BasePresenter p))
                p.Close();
        }

        // 모든 닫힘의 수렴점.
        private void OnPresenterClosed(BasePresenter presenter)
        {
            if (_pools.TryGetValue(presenter.Id, out MultiablePool pool))
            {
                pool.Release(presenter);
            }
            else
            {
                RemoveFromModalStack(presenter);
            }
        }

        private void AssignModalSortingOrder(BasePresenter presenter)
        {
            if (presenter.Layer == UILayer.HUD) { presenter.SortingOrder = (int)UILayer.HUD; return; }
            _modalStack.Push(presenter);
            presenter.SortingOrder = (int)presenter.Layer + _modalStack.Count;
        }

        private void RemoveFromModalStack(BasePresenter presenter)
        {
            if (_modalStack.Count == 0) return;
            if (_modalStack.Peek() == presenter) { _modalStack.Pop(); return; }

            // 비-top 닫기: 스택 재구성 후 sortingOrder 재정규화
            var arr = _modalStack.ToArray(); // top-first
            _modalStack.Clear();
            for (int i = arr.Length - 1; i >= 0; i--)
                if (arr[i] != presenter) _modalStack.Push(arr[i]);

            var norm = _modalStack.ToArray();
            for (int i = 0; i < norm.Length; i++)
                norm[i].SortingOrder = (int)norm[i].Layer + (norm.Length - i);
        }

        // 단일 인스턴스 타입 조회 (Multiable 제외).
        public T Get<T>() where T : BasePresenter
            => _byType.TryGetValue(typeof(T), out BasePresenter p) ? (T)p : null;

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (uiChannel == null) return;
            uiChannel.RemoveListener<OpenUIEvent>(HandleOpen);
            uiChannel.RemoveListener<CloseUIEvent>(HandleClose);
            uiChannel.RemoveListener<ToggleUIEvent>(HandleToggle);
        }
    }
}
