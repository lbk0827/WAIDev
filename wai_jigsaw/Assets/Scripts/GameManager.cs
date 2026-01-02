using UnityEngine;
using WaiJigsaw.Core;
using WaiJigsaw.Data;
using WaiJigsaw.UI;

/// <summary>
/// 게임의 전반적인 상태와 흐름을 관리하는 중앙 관리자입니다.
/// (싱글턴 패턴 사용)
/// - 데이터 관리는 GameDataContainer에 위임
/// - UI 로직은 UIMediator에 위임
/// - 게임 흐름 제어에 집중
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Scene-Specific References (씬별로 자동/수동 설정)")]
    public UIMediator uiMediator;           // 단일 씬 모드용 (SampleScene)
    public PuzzleBoardSetup puzzleBoard;    // GameScene에서 설정됨

    [Header("Multi-Scene Mediators (자동 탐색)")]
    private GameUIMediator _gameUIMediator; // GameScene용

    [Header("Persistent Managers (자동 탐색)")]
    [SerializeField] private LevelManager _levelManagerPrefab;
    [SerializeField] private LevelGroupManager _levelGroupManagerPrefab;

    // 싱글턴 인스턴스 접근용 프로퍼티
    public LevelManager levelManager => LevelManager.Instance;
    public LevelGroupManager levelGroupManager => LevelGroupManager.Instance;

    /// <summary>
    /// 현재 레벨 (GameDataContainer에서 가져옴)
    /// </summary>
    public int CurrentLevel => GameDataContainer.Instance.CurrentLevel;

    private void Awake()
    {
        // 싱글턴 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록 설정
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 영속 매니저들 초기화
        EnsurePersistentManagers();

        // GameDataContainer 초기화 및 데이터 로드
        GameDataContainer.Instance.Load();
    }

    /// <summary>
    /// 영속 매니저들이 존재하는지 확인하고, 없으면 생성합니다.
    /// </summary>
    private void EnsurePersistentManagers()
    {
        // SceneTransitionManager 확인 및 생성
        if (SceneTransitionManager.Instance == null)
        {
            GameObject go = new GameObject("SceneTransitionManager");
            go.AddComponent<SceneTransitionManager>();
        }

        // LevelManager 확인 및 생성
        if (LevelManager.Instance == null)
        {
            if (_levelManagerPrefab != null)
            {
                Instantiate(_levelManagerPrefab);
            }
            else
            {
                // 프리팹이 없으면 새 GameObject 생성
                GameObject go = new GameObject("LevelManager");
                go.AddComponent<LevelManager>();
            }
        }

        // LevelGroupManager 확인 및 생성
        if (LevelGroupManager.Instance == null)
        {
            if (_levelGroupManagerPrefab != null)
            {
                Instantiate(_levelGroupManagerPrefab);
            }
            else
            {
                // 프리팹이 없으면 새 GameObject 생성
                GameObject go = new GameObject("LevelGroupManager");
                go.AddComponent<LevelGroupManager>();
            }
        }

        // GameOptionManager 확인 및 생성
        if (GameOptionManager.Instance == null)
        {
            GameObject go = new GameObject("GameOptionManager");
            go.AddComponent<GameOptionManager>();
        }
    }

    #region Scene Transition Methods

    /// <summary>
    /// 로비 씬으로 전환합니다.
    /// </summary>
    public void LoadLobbyScene()
    {
        // 퍼즐 보드 정리
        if (puzzleBoard != null)
        {
            puzzleBoard.ClearBoard();
        }

        SceneTransitionManager.Instance.LoadLobbyScene();
    }

    /// <summary>
    /// 게임 씬으로 전환합니다.
    /// </summary>
    public void LoadGameScene()
    {
        SceneTransitionManager.Instance.LoadGameScene();
    }

    /// <summary>
    /// 현재 레벨을 재시작합니다. (인게임에서 Retry 버튼용)
    /// </summary>
    public void RetryCurrentLevel()
    {
        // 퍼즐 보드 정리
        if (puzzleBoard != null)
        {
            puzzleBoard.ClearBoard();
        }

        // GameUIMediator를 통해 퍼즐 다시 시작
        if (_gameUIMediator == null)
        {
            _gameUIMediator = FindObjectOfType<GameUIMediator>();
        }

        if (_gameUIMediator != null)
        {
            _gameUIMediator.StartGame();
        }
        else if (puzzleBoard != null)
        {
            // Mediator가 없으면 직접 퍼즐 보드 설정
            puzzleBoard.SetupCurrentLevel(CurrentLevel);
        }

        Debug.Log($"[GameManager] 레벨 {CurrentLevel} 재시작");
    }

    #endregion

    private void Start()
    {
        // SampleScene (단일 씬 모드) 호환성 유지
        // LobbyScene/GameScene 분리 시에는 각 씬의 Mediator가 초기화 담당
        if (uiMediator != null)
        {
            GoToHome();
        }
    }

    /// <summary>
    /// 홈 화면으로 이동합니다. (단일 씬 모드용)
    /// </summary>
    public void GoToHome()
    {
        if (puzzleBoard != null)
        {
            puzzleBoard.ClearBoard();
        }
        if (uiMediator != null)
        {
            uiMediator.ShowHome();
        }
    }

    /// <summary>
    /// 게임 데이터 저장 (GameDataContainer에 위임)
    /// </summary>
    public void SaveGameData()
    {
        GameDataContainer.Instance.Save();
    }

    /// <summary>
    /// 특정 레벨이 클리어되었는지 확인합니다. (GameDataContainer에 위임)
    /// </summary>
    public bool IsLevelCleared(int levelNumber)
    {
        return GameDataContainer.Instance.IsLevelCleared(levelNumber);
    }

    /// <summary>
    /// 현재 레벨의 게임을 시작합니다. (로비 -> 인게임)
    /// </summary>
    public void StartCurrentLevel()
    {
        StartLevel(CurrentLevel);
    }

    /// <summary>
    /// 특정 레벨의 게임을 시작합니다.
    /// </summary>
    /// <param name="levelNumber">시작할 레벨 번호</param>
    public void StartLevel(int levelNumber)
    {
        if (uiMediator != null && puzzleBoard != null)
        {
            uiMediator.ShowPuzzle();
            puzzleBoard.SetupCurrentLevel(levelNumber);
        }
    }

    public void ShowLevelIntro()
    {
        if (uiMediator != null && levelManager != null)
        {
            LevelConfig config = levelManager.GetLevelInfo(CurrentLevel);
            uiMediator.ShowLevelIntro(config);
        }
    }

    public void OnLevelComplete()
    {
        int clearedLevel = CurrentLevel;

        // 현재 레벨을 클리어 처리 (GameDataContainer에 위임)
        // -> LevelClearedEvent 발생 -> LobbyGridManager가 자동으로 카드 플립
        GameDataContainer.Instance.MarkLevelCleared(clearedLevel);

        // 다음 레벨로 이동 (GameDataContainer에 위임)
        // -> LevelChangedEvent 발생 -> UIManager가 자동으로 UI 업데이트
        GameDataContainer.Instance.AdvanceToNextLevel();
        SaveGameData();

        if (puzzleBoard != null)
        {
            puzzleBoard.ClearBoard();
        }

        // 결과 화면 표시 (단일 씬 모드 또는 멀티 씬 모드)
        if (uiMediator != null)
        {
            // 단일 씬 모드 (SampleScene)
            uiMediator.ShowResult();
        }
        else if (_gameUIMediator != null)
        {
            // 멀티 씬 모드 (GameScene)
            _gameUIMediator.ShowResult();
        }
        else
        {
            // GameUIMediator 자동 탐색
            _gameUIMediator = FindObjectOfType<GameUIMediator>();
            if (_gameUIMediator != null)
            {
                _gameUIMediator.ShowResult();
            }
        }
    }

    public void GoToNextLevel()
    {
        // Result 화면에서 다음 레벨로 넘어갈 때도 동일하게 StartCurrentLevel 사용
        StartCurrentLevel();
    }

    /// <summary>
    /// 디버그용: 모든 진행 데이터를 초기화합니다. (GameDataContainer에 위임)
    /// </summary>
    public void ResetAllProgress()
    {
        GameDataContainer.Instance.ResetAllProgress();
    }
}
