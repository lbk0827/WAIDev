using UnityEngine;
using System.Collections.Generic;

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
    public LevelManager levelManager;
    public LevelGroupManager levelGroupManager;
    public LobbyGridManager lobbyGridManager;

    public int CurrentLevel { get; private set; } = 1;

    // 클리어한 레벨들을 저장 (PlayerPrefs에 저장/로드)
    private HashSet<int> _clearedLevels = new HashSet<int>();
    private const string CLEARED_LEVELS_KEY = "ClearedLevels";

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
        // 현재 레벨 불러오기
        CurrentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

        // 클리어한 레벨 목록 불러오기
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

        Debug.Log($"불러온 현재 레벨: {CurrentLevel}, 클리어한 레벨 수: {_clearedLevels.Count}");
    }

    public void SaveGameData()
    {
        // 현재 레벨 저장
        PlayerPrefs.SetInt("CurrentLevel", CurrentLevel);

        // 클리어한 레벨 목록 저장
        string clearedData = string.Join(",", _clearedLevels);
        PlayerPrefs.SetString(CLEARED_LEVELS_KEY, clearedData);

        PlayerPrefs.Save();
        Debug.Log($"레벨 저장: {CurrentLevel}");
    }

    /// <summary>
    /// 특정 레벨이 클리어되었는지 확인합니다.
    /// </summary>
    public bool IsLevelCleared(int levelNumber)
    {
        return _clearedLevels.Contains(levelNumber);
    }

    /// <summary>
    /// 레벨을 클리어 처리합니다.
    /// </summary>
    private void MarkLevelCleared(int levelNumber)
    {
        if (!_clearedLevels.Contains(levelNumber))
        {
            _clearedLevels.Add(levelNumber);
        }
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
        if (uiManager != null && puzzleBoard != null)
        {
            uiManager.ShowPuzzle();
            puzzleBoard.SetupCurrentLevel(levelNumber);
        }
    }

    public void ShowLevelIntro()
    {
        if (uiManager != null && levelManager != null)
        {
            LevelConfig config = levelManager.GetLevelInfo(CurrentLevel);
            uiManager.ShowLevelIntro(config);
        }
    }

    public void OnLevelComplete()
    {
        // 현재 레벨을 클리어 처리
        MarkLevelCleared(CurrentLevel);

        // 다음 레벨로 이동
        CurrentLevel++;
        SaveGameData();

        if (puzzleBoard != null)
        {
            puzzleBoard.ClearBoard();
        }

        // 로비 그리드 업데이트 (클리어 애니메이션)
        if (lobbyGridManager != null)
        {
            lobbyGridManager.OnLevelCleared(CurrentLevel - 1);
        }

        if (uiManager != null)
        {
            uiManager.ShowResult();
        }
    }

    public void GoToNextLevel()
    {
        // Result 화면에서 다음 레벨로 넘어갈 때도 동일하게 StartCurrentLevel 사용
        StartCurrentLevel();
    }

    /// <summary>
    /// 디버그용: 모든 진행 데이터를 초기화합니다.
    /// </summary>
    public void ResetAllProgress()
    {
        CurrentLevel = 1;
        _clearedLevels.Clear();
        PlayerPrefs.DeleteKey("CurrentLevel");
        PlayerPrefs.DeleteKey(CLEARED_LEVELS_KEY);
        PlayerPrefs.Save();
        Debug.Log("모든 진행 데이터가 초기화되었습니다.");
    }
}
