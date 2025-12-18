using UnityEngine;

/// <summary>
/// 게임의 전반적인 상태와 흐름을 관리하는 중앙 관리자입니다.
/// (싱글턴 패턴 사용)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Component References")]
    public UIManager uiManager;
    public PuzzleBoardSetup puzzleBoard;
    public LevelDatabase levelDatabase;

    public int CurrentLevel { get; private set; } = 1;

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

        LoadGameData();
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
        if (uiManager != null)
        {
            uiManager.ShowHome();
        }
    }

    public void LoadGameData()
    {
        // 간단한 PlayerPrefs를 사용해 현재 레벨을 불러옵니다.
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
        Debug.Log($"불러온 현재 레벨: {CurrentLevel}");
    }

    public void SaveGameData()
    {
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);
        PlayerPrefs.Save();
        Debug.Log($"레벨 저장: {CurrentLevel}");
    }

    public void ShowLevelIntro()
    {
        if (uiManager != null && levelDatabase != null)
        {
            LevelConfig config = levelDatabase.GetLevelInfo(CurrentLevel);
            uiManager.ShowLevelIntro(config);
        }
    }

    public void OnLevelComplete()
    {
        // 레벨 클리어 시
        CurrentLevel++;
        SaveGameData();
        
        if (puzzleBoard != null)
        {
            puzzleBoard.ClearBoard();
        }
        if (uiManager != null)
        {
            uiManager.ShowResult();
        }
    }
    
    public void GoToNextLevel()
    {
        // Result 화면에서 다음 레벨로 넘어갈 때
        if (uiManager != null && puzzleBoard != null)
        {
            uiManager.ShowPuzzle();
            puzzleBoard.SetupCurrentLevel(CurrentLevel);
        }
    }
}
