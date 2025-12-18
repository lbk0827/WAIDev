using UnityEngine;
using UnityEngine.UI; // UI 컴포넌트(Text, Button 등)를 사용하기 위해 필요

/// <summary>
/// 게임의 전체 UI 패널(화면)들을 관리하고, 상태에 따라 적절한 UI를 보여줍니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Game Panels")]
    public GameObject homePanel;
    public GameObject levelIntroPanel;
    public GameObject puzzlePanel; // 퍼즐 조각들이 놓일 컨테이너
    public GameObject resultPanel;

    [Header("Home Panel UI")]
    public Text homeLevelText;
    public Button homeStartButton;

    [Header("Level Intro Panel UI")]
    public Text introLevelText;
    public Image introImagePreview;
    public Text introPiecesText;
    public Button introPlayButton;

    [Header("Result Panel UI")]
    public Text resultLevelText;
    public Button resultNextButton;

    private void Start()
    {
        // 모든 버튼에 대한 이벤트 리스너를 설정합니다.
        homeStartButton.onClick.AddListener(OnHomeStartClicked);
        introPlayButton.onClick.AddListener(OnIntroPlayClicked);
        resultNextButton.onClick.AddListener(OnResultNextClicked);
    }

    /// <summary>
    /// 홈 화면을 보여줍니다.
    /// </summary>
    public void ShowHome()
    {
        homePanel.SetActive(true);
        levelIntroPanel.SetActive(false);
        puzzlePanel.SetActive(false);
        resultPanel.SetActive(false);

        // 현재 레벨 텍스트를 업데이트합니다.
        if (GameManager.Instance != null)
        {
            homeLevelText.text = $"LEVEL {GameManager.Instance.CurrentLevel}";
        }
    }

    /// <summary>
    /// 레벨 인트로 화면을 보여줍니다.
    /// </summary>
    public void ShowLevelIntro(LevelConfig config)
    {
        homePanel.SetActive(false);
        levelIntroPanel.SetActive(true);
        puzzlePanel.SetActive(false);
        resultPanel.SetActive(false);

        // 레벨 정보를 UI에 표시합니다.
        introLevelText.text = $"LEVEL {config.levelNumber}";
        introImagePreview.sprite = Sprite.Create(config.puzzleData.sourceImage, 
            new Rect(0, 0, config.puzzleData.sourceImage.width, config.puzzleData.sourceImage.height), 
            new Vector2(0.5f, 0.5f));
        introPiecesText.text = $"{config.rows * config.cols} Pieces";
    }

    /// <summary>
    /// 퍼즐 플레이 화면을 보여줍니다.
    /// </summary>
    public void ShowPuzzle()
    {
        homePanel.SetActive(false);
        levelIntroPanel.SetActive(false);
        puzzlePanel.SetActive(true);
        resultPanel.SetActive(false);
    }
    
    /// <summary>
    /// 결과 화면을 보여줍니다.
    /// </summary>
    public void ShowResult()
    {
        homePanel.SetActive(false);
        levelIntroPanel.SetActive(false);
        puzzlePanel.SetActive(false);
        resultPanel.SetActive(true);

        resultLevelText.text = $"LEVEL {GameManager.Instance.CurrentLevel - 1} COMPLETE";
    }

    // --- 버튼 이벤트 핸들러 ---

    private void OnHomeStartClicked()
    {
        // GameManager를 통해 Level Intro 화면을 띄우도록 요청
        GameManager.Instance.ShowLevelIntro();
    }

    private void OnIntroPlayClicked()
    {
        // GameManager를 통해 퍼즐 플레이를 시작하도록 요청
        GameManager.Instance.GoToNextLevel();
    }

    private void OnResultNextClicked()
    {
        // GameManager를 통해 홈 화면으로 돌아가도록 요청
        GameManager.Instance.GoToHome();
    }
}
