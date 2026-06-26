using System;
using MVP.System.BaseMVP.Form;
using MVP.System.GameState;
using MVP.System.GenerateUI;
using MVP.UIData;
using UnityEngine;

namespace MVP.System.BaseMVP
{
    public abstract class BasePresenter : MonoBehaviour
    {
        [SerializeField] private UILayer layer;
        [SerializeField] private UIId    id;
        [SerializeField] private int     multiableCount; // 0=단일, N>0=Multiable 초기 풀 개수

        // true면 열릴 때 시간정지+커서자유, 닫힐 때 복귀를 자동 처리(holder = 이 Presenter).
        // 겹쳐 열려도 holder-set이 처리하므로 _prevTimeScale 백업 같은 건 불필요.
        [SerializeField] private bool    pausesGame;

        public UILayer Layer          => layer;
        public UIId    Id             => id;
        public int     MultiableCount => multiableCount;

        // 현재 열려 있는가. 씬 전환 시 전체 리셋(열린 UI 전부 Close)에서 사용.
        public bool    IsOpen         { get; private set; }

        // 구체 Presenter에서 override해 게임상태 게이팅(컷씬 중 인벤토리 금지 등).
        public virtual bool CanOpen => true;

        // 씬 전환 시 전체 리셋(열린 UI 전부 Close)에서 제외할지 여부.
        // Fade 같은 씬 전환 연출용 UI는 true로 둬 전환 중 닫히지 않게 한다.
        public virtual bool PersistsAcrossSceneReset => false;

        // UIManager가 Canvas.sortingOrder를 직접 부여.
        public int SortingOrder
        {
            set { if (View?.RootCanvas != null) View.RootCanvas.sortingOrder = value; }
        }

        // 닫힘 완료(애니메이션 포함) 시 발행. UIManager가 구독해 스택/풀 갱신.
        public event Action<BasePresenter> OnClosed;

        protected BaseView  View  { get; private set; }
        protected IModel    Model { get; private set; }

        public virtual bool IsWorldPosition => false;

        protected abstract IModel    CreateModel();
        protected abstract BaseView  ResolveView();

        public virtual void InitializePresenter()
        {
            Model = CreateModel();
            View  = ResolveView();

            if (Model == null || View == null)
            {
                Debug.LogError($"{name}: Model 또는 View를 해석할 수 없습니다.");
                return;
            }

            BaseForm[] forms = GetComponentsInChildren<BaseForm>(true);

            foreach (BaseForm form in forms)
                if (form is IInitializable initializable)
                    initializable.Initialize();

            foreach (BaseForm form in forms)
            {
                if (form is IInteractable interactable && !string.IsNullOrEmpty(form.InteractMethod))
                {
                    Action<UIParam> call = MVPBinding.ResolveInteract(Model, form.InteractMethod);
                    if (call != null) interactable.OnFormInteracted += p => call(p);
                    else Debug.LogWarning($"{name}/{form.name}: interact '{form.InteractMethod}' 미해결");
                }

                if (form is IUpdatable updatable && !string.IsNullOrEmpty(form.UpdateMethod))
                {
                    Func<UIParam> call = MVPBinding.ResolveUpdate(Model, form.UpdateMethod);
                    if (call != null) updatable.BindUpdateSource(call);
                    else Debug.LogWarning($"{name}/{form.name}: update '{form.UpdateMethod}' 미해결");
                }
            }

            View.InitializeView(forms);
            View.OnViewClosed += RaiseOnClosed;
        }

            // payload를 Model에 반영하고 View를 연다. 구체 Presenter는 override해 payload 캐스팅만.
        public virtual void Open<T>(T payload)
        {
            IsOpen = true;
            if (pausesGame) { TimeManager.Stop(this); CursorManager.Free(this); }
            View.OpenView();
        }

        // View 닫기를 시작. 닫힘 완료 → View.OnViewClosed → OnClosed.
        public virtual void Close()
        {
            IsOpen = false;
            View.CloseView();
        }

        // 닫힘 애니메이션 완료 시점. 정지/커서를 여기서 복귀시켜야 닫히는 동안 입력이 안 샌다.
        private void RaiseOnClosed()
        {
            if (pausesGame) { TimeManager.Resume(this); CursorManager.Lock(this); }
            OnClosed?.Invoke(this);
        }

        protected virtual void OnDestroy()
        {
            // 열린 채 파괴(씬 전환 등)되면 RaiseOnClosed를 안 거치므로 여기서 holder 회수.
            if (IsOpen && pausesGame) { TimeManager.Resume(this); CursorManager.Lock(this); }
            if (View != null) View.OnViewClosed -= RaiseOnClosed;
            View?.OnDestroyView();
        }
    }

    public abstract class BasePresenter<TModel, TView> : BasePresenter
        where TModel : IModel, new()
        where TView  : BaseView
    {
        protected override IModel   CreateModel() => new TModel();
        protected override BaseView ResolveView()  => GetComponent<TView>();
    }
}
