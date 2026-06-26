using System;
using System.Collections.Generic;
using MVP.System.BaseMVP;

namespace MVP.System.GenerateUI
{
    public class MultiablePool
    {
        private readonly Func<BasePresenter>  _factory;
        private readonly Queue<BasePresenter> _free   = new();
        private readonly List<BasePresenter>  _active = new();

        public int ActiveCount => _active.Count;

        // factory: 새 인스턴스를 만들고 InitializePresenter까지 완료해 반환.
        // initialCount: 미리 만들어둘 개수(모두 free 상태).
        public MultiablePool(Func<BasePresenter> factory, int initialCount = 0)
        {
            _factory = factory;
            for (int i = 0; i < initialCount; i++)
                _free.Enqueue(_factory());
        }

        // free 없으면 factory로 증식. 반환된 presenter는 active 상태.
        public BasePresenter Acquire()
        {
            BasePresenter p = _free.Count > 0 ? _free.Dequeue() : _factory();
            _active.Add(p);
            return p;
        }

        // active → free 반납.
        public void Release(BasePresenter p)
        {
            _active.Remove(p);
            _free.Enqueue(p);
        }

        // 현재 active 인스턴스의 복사본. 전체 리셋 시 순회하며 Close하기 위함
        // (Close → Release가 _active를 변형하므로 스냅샷이 필요).
        public BasePresenter[] SnapshotActive() => _active.ToArray();
    }
}
