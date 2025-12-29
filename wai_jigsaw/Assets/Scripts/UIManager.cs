using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaiJigsaw.Data;
using System;

/// <summary>
/// [DEPRECATED] 이 클래스는 UIView + UIMediator로 분리되었습니다.
/// - UIView: UI 컴포넌트 참조 및 표시 메서드
/// - UIMediator: 비즈니스 로직 (버튼 핸들러, Observer 구독)
///
/// 마이그레이션:
/// 1. 씬에서 UIManager 컴포넌트 제거
/// 2. UIView와 UIMediator 컴포넌트 추가
/// 3. UIView의 UI 컴포넌트 참조 재설정
/// 4. UIMediator의 _view 참조에 UIView 연결
/// 5. GameManager의 uiMediator 참조에 UIMediator 연결
/// </summary>
[Obsolete("UIManager는 deprecated 되었습니다. UIView + UIMediator를 사용하세요.")]
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
    public Button homePlayButton;       // 플레이 버튼 (기존 방식용, 없어도 됨)
    public TMP_Text homePlayButtonText; // 플레이 버튼 내부 텍스트

    [Header("Lobby Grid")]
    public LobbyGridManager lobbyGridManager; // 5x5 레벨 그리드

    [Header("Level Intro Panel UI")]
    public TMP_Text introLevelText;
    public Image introImagePreview;
    public TMP_Text introPiecesText;
    public Button introPlayButton;

    [Header("Result Panel UI")]
    public TMP_Text resultLevelText;
    public Button resultNextButton;

    // Observer 참조 (해제용)
    private ActionObserver<LevelChangedEvent> _levelChangedObserver;

    private void OnEnable()
    {
        // LevelChangedEvent 구독
        _levelChangedObserver = GameDataContainer.Instance.AddLevelChangedObserver(OnLevelChangedEvent);
    }

    private void OnDisable()
    {
        // 구독 해제
        if (_levelChangedObserver != null)
        {
            GameDataContainer.Instance.RemoveLevelChangedObserver(_levelChangedObserver);
            _levelChangedObserver = null;
        }
    }

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
    /// LevelChangedEvent 핸들러 (Observer 패턴)
    /// </summary>
    private void OnLevelChangedEvent(LevelChangedEvent evt)
    {
        // 홈 화면이 활성화된 상태라면 UI 업데이트
        if (homePanel != null && homePanel.activeSelf)
        {
            UpdateHomeUI(evt.NewLevel);
        }

        Debug.Log($"[UIManager] 레벨 변경 이벤트 수신: {evt.OldLevel} -> {evt.NewLevel}");
    }

    /// <summary>
    /// 홈 화면 UI 업데이트
    /// </summary>
    private void UpdateHomeUI(int currentLevel)
    {
        // 플레이 버튼 텍스트 업데이트
        if (homePlayButtonText != null)
        {
            homePlayButtonText.text = $"PLAY\n<size=60%>Level {currentLevel}</size>";
        }
    }

    /// <summary>
    /// 홈 화면을 보여줍니다.
    /// </summary>
    public void ShowHome()
    {
        ActivatePanel(homePanel);

        int currentLevel = GameDataContainer.Instance.CurrentLevel;

        // 로비 그리드 설정 (5x5 레벨 카드)
        if (lobbyGridManager != null)
        {
            lobbyGridManager.SetupGrid(currentLevel);
        }

        // 플레이 버튼 텍스트 업데이트
        UpdateHomeUI(currentLevel);
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
