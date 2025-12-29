using System.Collections.Generic;
using UnityEngine;

namespace WaiJigsaw.Data
{
    /// <summary>
    /// 레벨 변경 이벤트 데이터
    /// </summary>
    public struct LevelChangedEvent
    {
        public int OldLevel;
        public int NewLevel;

        public LevelChangedEvent(int oldLevel, int newLevel)
        {
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }

    /// <summary>
    /// 레벨 클리어 이벤트 데이터
    /// </summary>
    public struct LevelClearedEvent
    {
        public int ClearedLevel;
        public int TotalCleared;

        public LevelClearedEvent(int clearedLevel, int totalCleared)
        {
            ClearedLevel = clearedLevel;
            TotalCleared = totalCleared;
        }
    }

    /// <summary>
    /// 게임 데이터 컨테이너 (싱글턴)
    /// - 게임 진행 데이터를 중앙에서 관리
    /// - Observer 패턴으로 데이터 변경 알림
    /// </summary>
    public class GameDataContainer
    {
        private static GameDataContainer _instance;
        public static GameDataContainer Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameDataContainer();
                }
                return _instance;
            }
        }

        // PlayerPrefs 키
        private const string CURRENT_LEVEL_KEY = "CurrentLevel";
        private const string CLEARED_LEVELS_KEY = "ClearedLevels";

        // 데이터
        private int _currentLevel = 1;
        private HashSet<int> _clearedLevels = new HashSet<int>();

        // Observer 컬렉션
        private readonly ObserverCollection<LevelChangedEvent> _levelChangedObservers = new ObserverCollection<LevelChangedEvent>();
        private readonly ObserverCollection<LevelClearedEvent> _levelClearedObservers = new ObserverCollection<LevelClearedEvent>();

        /// <summary>
        /// 현재 레벨 (읽기 전용)
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// 클리어한 레벨 수
        /// </summary>
        public int ClearedLevelCount => _clearedLevels.Count;

        private GameDataContainer()
        {
            // 싱글턴 생성자
        }

        #region 데이터 로드/저장

        /// <summary>
        /// PlayerPrefs에서 게임 데이터 로드
        /// </summary>
        public void Load()
        {
            // 현재 레벨 로드
            _currentLevel = PlayerPrefs.GetInt(CURRENT_LEVEL_KEY, 1);

            // 클리어 레벨 목록 로드
            _clearedLevels.Clear();
            string clearedData = PlayerPrefs.GetString(CLEARED_LEVELS_KEY, "");
            if (!string.IsNullOrEmpty(clearedData))
            {
                string[] levels = clearedData.Split(',');
                foreach (string levelStr in levels)
                {
                    if (int.TryParse(levelStr, out int level))
                    {
                        _clearedLevels.Add(level);
                    }
                }
            }

            Debug.Log($"[GameDataContainer] 로드 완료 - 현재 레벨: {_currentLevel}, 클리어 수: {_clearedLevels.Count}");
        }

        /// <summary>
        /// PlayerPrefs에 게임 데이터 저장
        /// </summary>
        public void Save()
        {
            PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, _currentLevel);

            string clearedData = string.Join(",", _clearedLevels);
            PlayerPrefs.SetString(CLEARED_LEVELS_KEY, clearedData);

            PlayerPrefs.Save();

            Debug.Log($"[GameDataContainer] 저장 완료 - 현재 레벨: {_currentLevel}");
        }

        #endregion

        #region 현재 레벨 관리

        /// <summary>
        /// 현재 레벨 설정 (Observer 알림 포함)
        /// </summary>
        public void SetCurrentLevel(int level)
        {
            if (_currentLevel == level)
                return;

            int oldLevel = _currentLevel;
            _currentLevel = level;

            // Observer들에게 알림
            _levelChangedObservers.NotifyObservers(new LevelChangedEvent(oldLevel, _currentLevel));
        }

        /// <summary>
        /// 다음 레벨로 진행
        /// </summary>
        public void AdvanceToNextLevel()
        {
            SetCurrentLevel(_currentLevel + 1);
        }

        /// <summary>
        /// 레벨 변경 Observer 등록
        /// </summary>
        public void AddLevelChangedObserver(IObserver<LevelChangedEvent> observer)
        {
            _levelChangedObservers.AddObserver(observer);
        }

        /// <summary>
        /// 레벨 변경 Observer 해제
        /// </summary>
        public void RemoveLevelChangedObserver(IObserver<LevelChangedEvent> observer)
        {
            _levelChangedObservers.RemoveObserver(observer);
        }

        /// <summary>
        /// 레벨 변경 Observer 등록 (Action 버전)
        /// </summary>
        public ActionObserver<LevelChangedEvent> AddLevelChangedObserver(System.Action<LevelChangedEvent> callback)
        {
            var observer = new ActionObserver<LevelChangedEvent>(callback);
            _levelChangedObservers.AddObserver(observer);
            return observer;
        }

        #endregion

        #region 클리어 레벨 관리

        /// <summary>
        /// 특정 레벨이 클리어되었는지 확인
        /// </summary>
        public bool IsLevelCleared(int levelNumber)
        {
            return _clearedLevels.Contains(levelNumber);
        }

        /// <summary>
        /// 레벨 클리어 처리 (Observer 알림 포함)
        /// </summary>
        public void MarkLevelCleared(int levelNumber)
        {
            if (_clearedLevels.Contains(levelNumber))
                return;

            _clearedLevels.Add(levelNumber);

            // Observer들에게 알림
            _levelClearedObservers.NotifyObservers(new LevelClearedEvent(levelNumber, _clearedLevels.Count));
        }

        /// <summary>
        /// 레벨 클리어 Observer 등록
        /// </summary>
        public void AddLevelClearedObserver(IObserver<LevelClearedEvent> observer)
        {
            _levelClearedObservers.AddObserver(observer);
        }

        /// <summary>
        /// 레벨 클리어 Observer 해제
        /// </summary>
        public void RemoveLevelClearedObserver(IObserver<LevelClearedEvent> observer)
        {
            _levelClearedObservers.RemoveObserver(observer);
        }

        /// <summary>
        /// 레벨 클리어 Observer 등록 (Action 버전)
        /// </summary>
        public ActionObserver<LevelClearedEvent> AddLevelClearedObserver(System.Action<LevelClearedEvent> callback)
        {
            var observer = new ActionObserver<LevelClearedEvent>(callback);
            _levelClearedObservers.AddObserver(observer);
            return observer;
        }

        #endregion

        #region 초기화

        /// <summary>
        /// 모든 진행 데이터 초기화
        /// </summary>
        public void ResetAllProgress()
        {
            int oldLevel = _currentLevel;
            _currentLevel = 1;
            _clearedLevels.Clear();

            PlayerPrefs.DeleteKey(CURRENT_LEVEL_KEY);
            PlayerPrefs.DeleteKey(CLEARED_LEVELS_KEY);
            PlayerPrefs.Save();

            // Observer 알림
            if (oldLevel != 1)
            {
                _levelChangedObservers.NotifyObservers(new LevelChangedEvent(oldLevel, 1));
            }

            Debug.Log("[GameDataContainer] 모든 진행 데이터 초기화 완료");
        }

        /// <summary>
        /// Observer 전체 해제 (씬 전환 시 호출)
        /// </summary>
        public void ClearAllObservers()
        {
            _levelChangedObservers.Clear();
            _levelClearedObservers.Clear();
        }

        #endregion
    }
}
