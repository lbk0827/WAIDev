using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro 네임스페이스 추가

/// <summary>
/// 게임의 전체 UI 패널(화면)들을 관리하고, 상태에 따라 적절한 UI를 보여줍니다.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Game Panels")]
    public GameObject homePanel;
    public GameObject levelIntroPanel; // (사용 안 할 수도 있지만 일단 유지)
    public GameObject puzzlePanel;
    public GameObject resultPanel;

    [Header("Home Panel UI")]
    public TMP_Text homeTitleText;      // 타이틀 텍스트
    public Button homeSettingsButton;   // 설정 버튼
    public Button homePlayButton;       // 플레이 버튼
    public TMP_Text homePlayButtonText; // 플레이 버튼 내부 텍스트 (레벨 표시용)

    [Header("Level Intro Panel UI")]
    public TMP_Text introLevelText;
    public Image introImagePreview;
    public TMP_Text introPiecesText;
    public Button introPlayButton;

    [Header("Result Panel UI")]
    public TMP_Text resultLevelText;
    public Button resultNextButton;

    private void Start()
    {
        // --- Home Panel Events ---
        if (homePlayButton != null)
            homePlayButton.onClick.AddListener(OnHomePlayClicked);
        
        if (homeSettingsButton != null)
            homeSettingsButton.onClick.AddListener(OnHomeSettingsClicked);

        // --- Intro Panel Events ---
        if (introPlayButton != null)
            introPlayButton.onClick.AddListener(OnIntroPlayClicked);

        // --- Result Panel Events ---
        if (resultNextButton != null)
            resultNextButton.onClick.AddListener(OnResultNextClicked);
    }

    /// <summary>
    /// 홈 화면을 보여줍니다.
    /// </summary>
    public void ShowHome()
    {
        ActivatePanel(homePanel);

        // 플레이 버튼에 현재 레벨 표시 (예: "플레이\n레벨 2")
        if (GameManager.Instance != null && homePlayButtonText != null)
        {
            homePlayButtonText.text = $"플레이\n<size=60%>레벨 {GameManager.Instance.CurrentLevel}</size>";
        }
    }

    /// <summary>
    /// 퍼즐 플레이 화면을 보여줍니다.
    /// </summary>
    public void ShowPuzzle()
    {
        ActivatePanel(puzzlePanel);
    }
    
    /// <summary>
    /// 결과 화면을 보여줍니다.
    /// </summary>
    public void ShowResult()
    {
        ActivatePanel(resultPanel);

        if (resultLevelText != null)
        {
            resultLevelText.text = $"LEVEL {GameManager.Instance.CurrentLevel - 1} COMPLETE";
        }
    }

    /// <summary>
    /// 특정 패널만 활성화하고 나머지는 끕니다.
    /// </summary>
    private void ActivatePanel(GameObject targetPanel)
    {
        if (homePanel != null) homePanel.SetActive(targetPanel == homePanel);
        if (levelIntroPanel != null) levelIntroPanel.SetActive(targetPanel == levelIntroPanel);
        if (puzzlePanel != null) puzzlePanel.SetActive(targetPanel == puzzlePanel);
        if (resultPanel != null) resultPanel.SetActive(targetPanel == resultPanel);
    }

    // --- 레벨 인트로 관련 (필요 시 사용) ---
    public void ShowLevelIntro(LevelConfig config)
    {
        ActivatePanel(levelIntroPanel);
        if (introLevelText != null) introLevelText.text = $"LEVEL {config.levelNumber}";
        // 이미지 프리뷰 로직...
    }

    // --- 버튼 이벤트 핸들러 ---

    private void OnHomePlayClicked()
    {
        // 기획 플로우: 로비 플레이 버튼 -> 바로 게임 시작
        GameManager.Instance.StartCurrentLevel();
    }

    private void OnHomeSettingsClicked()
    {
        Debug.Log("설정 버튼 클릭됨 (구현 예정)");
        // 여기에 설정 팝업을 띄우는 로직 추가 가능
    }

    private void OnIntroPlayClicked()
    {
        GameManager.Instance.StartCurrentLevel();
    }

    private void OnResultNextClicked()
    {
        GameManager.Instance.GoToHome();
    }
}
