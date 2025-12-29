using UnityEngine;
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

    [Header("Component References")]
    public UIMediator uiMediator;
    public PuzzleBoardSetup puzzleBoard;
    public LevelManager levelManager;
    public LevelGroupManager levelGroupManager;

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

        // GameDataContainer 초기화 및 데이터 로드
        GameDataContainer.Instance.Load();
    }

    private void Start()
    {
        // 게임 시작 시 홈 화면으로 이동
        GoToHome();
    }

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

        if (uiMediator != null)
        {
            uiMediator.ShowResult();
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
