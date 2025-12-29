using System;
using System.Collections.Generic;

namespace WaiJigsaw.Data
{
    /// <summary>
    /// Observer 인터페이스 (기본)
    /// </summary>
    public interface IObserver
    { }

    /// <summary>
    /// 제네릭 Observer 인터페이스
    /// </summary>
    public interface IObserver<T> : IObserver
    {
        void OnChanged(T value);
    }

    /// <summary>
    /// Observable 인터페이스
    /// </summary>
    public interface IObservable<T>
    {
        void AddObserver(IObserver<T> observer);
        void RemoveObserver(IObserver<T> observer);
        void NotifyObservers(T value);
    }

    /// <summary>
    /// Action 기반 Observer 래퍼
    /// - 람다식이나 메서드를 Observer로 사용할 수 있게 해줌
    /// </summary>
    public class ActionObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onChanged;

        public ActionObserver(Action<T> onChanged)
        {
            _onChanged = onChanged;
        }

        public void OnChanged(T value)
        {
            _onChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Observable 컬렉션 관리 헬퍼
    /// </summary>
    public class ObserverCollection<T> : IObservable<T>
    {
        private readonly List<IObserver<T>> _observers = new List<IObserver<T>>();

        public void AddObserver(IObserver<T> observer)
        {
            if (observer == null || _observers.Contains(observer))
                return;

            _observers.Add(observer);
        }

        public void RemoveObserver(IObserver<T> observer)
        {
            if (observer == null || !_observers.Contains(observer))
                return;

            _observers.Remove(observer);
        }

        public void NotifyObservers(T value)
        {
            // 역순으로 순회하여 콜백 중 RemoveObserver 호출 시 안전하게 처리
            for (int i = _observers.Count - 1; i >= 0; i--)
            {
                if (i < _observers.Count && _observers[i] != null)
                {
                    _observers[i].OnChanged(value);
                }
            }
        }

        public void Clear()
        {
            _observers.Clear();
        }

        public int Count => _observers.Count;
    }
}
