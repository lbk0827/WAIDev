using System;
using System.Collections.Generic;
using UnityEngine;
using WaiJigsaw.Data;

namespace WaiJigsaw.Core
{
    /// <summary>
    /// MonoBehaviour 기반 클래스의 공통 기능을 제공하는 추상 클래스
    /// - 표준화된 생명주기 관리 (Initialize / Cleanup)
    /// - Observer 자동 해제
    /// - 초기화 상태 추적
    /// </summary>
    public abstract class MonoObject : MonoBehaviour
    {
        /// <summary>
        /// 초기화 완료 여부
        /// </summary>
        public bool IsInitialized { get; private set; }

        // Observer 참조 리스트 (자동 해제용)
        private readonly List<IObserver> _observers = new List<IObserver>();

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            OnAwake();
        }

        protected virtual void Start()
        {
            Initialize();
        }

        protected virtual void OnEnable()
        {
            OnEnabled();
        }

        protected virtual void OnDisable()
        {
            OnDisabled();
        }

        protected virtual void OnDestroy()
        {
            Cleanup();
            OnFinalize();
        }

        #endregion

        #region Lifecycle Methods

        /// <summary>
        /// 초기화 수행
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
            OnInitialize();

            Debug.Log($"[{GetType().Name}] 초기화 완료");
        }

        /// <summary>
        /// 정리 수행
        /// </summary>
        public void Cleanup()
        {
            if (!IsInitialized)
                return;

            // 등록된 모든 Observer 해제
            ClearAllObservers();

            OnCleanup();
            IsInitialized = false;

            Debug.Log($"[{GetType().Name}] 정리 완료");
        }

        #endregion

        #region Virtual Methods (하위 클래스에서 오버라이드)

        /// <summary>
        /// Awake 시점에 호출 (Unity Awake)
        /// </summary>
        protected virtual void OnAwake() { }

        /// <summary>
        /// 초기화 시점에 호출 (Start 또는 수동 Initialize 호출 시)
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// 활성화 시점에 호출 (Unity OnEnable)
        /// - Observer 등록은 여기서
        /// </summary>
        protected virtual void OnEnabled() { }

        /// <summary>
        /// 비활성화 시점에 호출 (Unity OnDisable)
        /// - Observer 해제는 여기서
        /// </summary>
        protected virtual void OnDisabled() { }

        /// <summary>
        /// 정리 시점에 호출 (Cleanup 호출 시)
        /// </summary>
        protected virtual void OnCleanup() { }

        /// <summary>
        /// 파괴 시점에 호출 (Unity OnDestroy)
        /// </summary>
        protected virtual void OnFinalize() { }

        #endregion

        #region Observer Management

        /// <summary>
        /// LevelChangedEvent Observer 등록 (자동 해제 관리)
        /// </summary>
        protected ActionObserver<LevelChangedEvent> RegisterLevelChangedObserver(Action<LevelChangedEvent> callback)
        {
            var observer = GameDataContainer.Instance.AddLevelChangedObserver(callback);
            _observers.Add(observer);
            return observer;
        }

        /// <summary>
        /// LevelClearedEvent Observer 등록 (자동 해제 관리)
        /// </summary>
        protected ActionObserver<LevelClearedEvent> RegisterLevelClearedObserver(Action<LevelClearedEvent> callback)
        {
            var observer = GameDataContainer.Instance.AddLevelClearedObserver(callback);
            _observers.Add(observer);
            return observer;
        }

        /// <summary>
        /// 특정 Observer 해제
        /// </summary>
        protected void UnregisterObserver<T>(ActionObserver<T> observer)
        {
            if (observer == null)
                return;

            _observers.Remove(observer);

            // 타입에 따라 적절한 해제 메서드 호출
            if (observer is ActionObserver<LevelChangedEvent> levelChangedObserver)
            {
                GameDataContainer.Instance.RemoveLevelChangedObserver(levelChangedObserver);
            }
            else if (observer is ActionObserver<LevelClearedEvent> levelClearedObserver)
            {
                GameDataContainer.Instance.RemoveLevelClearedObserver(levelClearedObserver);
            }
        }

        /// <summary>
        /// 모든 Observer 해제
        /// </summary>
        private void ClearAllObservers()
        {
            foreach (var observer in _observers)
            {
                if (observer is ActionObserver<LevelChangedEvent> levelChangedObserver)
                {
                    GameDataContainer.Instance.RemoveLevelChangedObserver(levelChangedObserver);
                }
                else if (observer is ActionObserver<LevelClearedEvent> levelClearedObserver)
                {
                    GameDataContainer.Instance.RemoveLevelClearedObserver(levelClearedObserver);
                }
            }

            _observers.Clear();
        }

        #endregion
    }
}
