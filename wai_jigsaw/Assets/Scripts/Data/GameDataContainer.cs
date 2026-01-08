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
    /// 코인 변경 이벤트 데이터
    /// </summary>
    public struct CoinChangedEvent
    {
        public int OldAmount;
        public int NewAmount;
        public int Delta;  // 변화량 (양수 = 획득, 음수 = 소비)

        public CoinChangedEvent(int oldAmount, int newAmount)
        {
            OldAmount = oldAmount;
            NewAmount = newAmount;
            Delta = newAmount - oldAmount;
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
        private const string COIN_KEY = "Coin";

        // 기본값
        private const int DEFAULT_INIT_COIN = 100;  // ConfigTable에서 로드 실패 시 기본값

        // 데이터
        private int _currentLevel = 1;
        private HashSet<int> _clearedLevels = new HashSet<int>();
        private int _coin = 0;

        // 방금 클리어한 레벨 (로비 복귀 시 카드 플립 연출용, 메모리에만 저장)
        private int _justClearedLevel = -1;

        // Observer 컬렉션
        private readonly ObserverCollection<LevelChangedEvent> _levelChangedObservers = new ObserverCollection<LevelChangedEvent>();
        private readonly ObserverCollection<LevelClearedEvent> _levelClearedObservers = new ObserverCollection<LevelClearedEvent>();
        private readonly ObserverCollection<CoinChangedEvent> _coinChangedObservers = new ObserverCollection<CoinChangedEvent>();

        /// <summary>
        /// 현재 레벨 (읽기 전용)
        /// </summary>
        public int CurrentLevel => _currentLevel;

        /// <summary>
        /// 클리어한 레벨 수
        /// </summary>
        public int ClearedLevelCount => _clearedLevels.Count;

        /// <summary>
        /// 현재 보유 코인
        /// </summary>
        public int Coin => _coin;

        /// <summary>
        /// 방금 클리어한 레벨 (로비에서 카드 플립 연출용)
        /// -1이면 연출 불필요
        /// </summary>
        public int JustClearedLevel => _justClearedLevel;

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

            // 코인 로드 (첫 실행 시 초기 코인 지급)
            if (PlayerPrefs.HasKey(COIN_KEY))
            {
                _coin = PlayerPrefs.GetInt(COIN_KEY, 0);
            }
            else
            {
                // 첫 실행: ConfigTable에서 InitCoin 로드 (추후 구현)
                // 현재는 기본값 사용
                _coin = DEFAULT_INIT_COIN;
                PlayerPrefs.SetInt(COIN_KEY, _coin);
                PlayerPrefs.Save();
            }

            Debug.Log($"[GameDataContainer] 로드 완료 - 현재 레벨: {_currentLevel}, 클리어 수: {_clearedLevels.Count}, 코인: {_coin}");
        }

        /// <summary>
        /// PlayerPrefs에 게임 데이터 저장
        /// </summary>
        public void Save()
        {
            PlayerPrefs.SetInt(CURRENT_LEVEL_KEY, _currentLevel);

            string clearedData = string.Join(",", _clearedLevels);
            PlayerPrefs.SetString(CLEARED_LEVELS_KEY, clearedData);

            PlayerPrefs.SetInt(COIN_KEY, _coin);

            PlayerPrefs.Save();

            Debug.Log($"[GameDataContainer] 저장 완료 - 현재 레벨: {_currentLevel}, 코인: {_coin}");
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

            // 방금 클리어한 레벨 저장 (로비 복귀 시 카드 플립 연출용)
            _justClearedLevel = levelNumber;

            // Observer들에게 알림
            _levelClearedObservers.NotifyObservers(new LevelClearedEvent(levelNumber, _clearedLevels.Count));
        }

        /// <summary>
        /// 방금 클리어한 레벨 정보를 소비합니다.
        /// 로비에서 카드 플립 연출 후 호출하여 중복 연출 방지.
        /// </summary>
        /// <returns>방금 클리어한 레벨 번호 (-1이면 없음)</returns>
        public int ConsumeJustClearedLevel()
        {
            int level = _justClearedLevel;
            _justClearedLevel = -1;
            return level;
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

        #region 코인 관리

        /// <summary>
        /// 코인 추가 (획득)
        /// </summary>
        /// <param name="amount">추가할 코인량 (양수)</param>
        public void AddCoin(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[GameDataContainer] AddCoin: 양수만 허용됩니다. amount={amount}");
                return;
            }

            int oldCoin = _coin;
            _coin += amount;

            // Observer들에게 알림
            _coinChangedObservers.NotifyObservers(new CoinChangedEvent(oldCoin, _coin));

            Debug.Log($"[GameDataContainer] 코인 획득: +{amount} (총 {_coin})");
        }

        /// <summary>
        /// 코인 소비
        /// </summary>
        /// <param name="amount">소비할 코인량 (양수)</param>
        /// <returns>소비 성공 여부</returns>
        public bool SpendCoin(int amount)
        {
            if (amount <= 0)
            {
                Debug.LogWarning($"[GameDataContainer] SpendCoin: 양수만 허용됩니다. amount={amount}");
                return false;
            }

            if (_coin < amount)
            {
                Debug.LogWarning($"[GameDataContainer] SpendCoin: 코인 부족. 필요={amount}, 보유={_coin}");
                return false;
            }

            int oldCoin = _coin;
            _coin -= amount;

            // Observer들에게 알림
            _coinChangedObservers.NotifyObservers(new CoinChangedEvent(oldCoin, _coin));

            Debug.Log($"[GameDataContainer] 코인 소비: -{amount} (남은 {_coin})");
            return true;
        }

        /// <summary>
        /// 코인이 충분한지 확인
        /// </summary>
        public bool HasEnoughCoin(int amount)
        {
            return _coin >= amount;
        }

        /// <summary>
        /// 코인 변경 Observer 등록
        /// </summary>
        public void AddCoinChangedObserver(IObserver<CoinChangedEvent> observer)
        {
            _coinChangedObservers.AddObserver(observer);
        }

        /// <summary>
        /// 코인 변경 Observer 해제
        /// </summary>
        public void RemoveCoinChangedObserver(IObserver<CoinChangedEvent> observer)
        {
            _coinChangedObservers.RemoveObserver(observer);
        }

        /// <summary>
        /// 코인 변경 Observer 등록 (Action 버전)
        /// </summary>
        public ActionObserver<CoinChangedEvent> AddCoinChangedObserver(System.Action<CoinChangedEvent> callback)
        {
            var observer = new ActionObserver<CoinChangedEvent>(callback);
            _coinChangedObservers.AddObserver(observer);
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
            int oldCoin = _coin;

            _currentLevel = 1;
            _clearedLevels.Clear();
            _coin = DEFAULT_INIT_COIN;  // 초기 코인으로 리셋

            PlayerPrefs.DeleteKey(CURRENT_LEVEL_KEY);
            PlayerPrefs.DeleteKey(CLEARED_LEVELS_KEY);
            PlayerPrefs.DeleteKey(COIN_KEY);
            PlayerPrefs.Save();

            // Observer 알림
            if (oldLevel != 1)
            {
                _levelChangedObservers.NotifyObservers(new LevelChangedEvent(oldLevel, 1));
            }

            if (oldCoin != _coin)
            {
                _coinChangedObservers.NotifyObservers(new CoinChangedEvent(oldCoin, _coin));
            }

            Debug.Log($"[GameDataContainer] 모든 진행 데이터 초기화 완료 (코인: {_coin})");
        }

        /// <summary>
        /// Observer 전체 해제 (씬 전환 시 호출)
        /// </summary>
        public void ClearAllObservers()
        {
            _levelChangedObservers.Clear();
            _levelClearedObservers.Clear();
            _coinChangedObservers.Clear();
        }

        #endregion
    }
}
