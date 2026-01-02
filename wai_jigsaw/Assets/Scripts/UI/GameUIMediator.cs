using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaiJigsaw.Core;
using WaiJigsaw.Data;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 게임 씬 전용 UI Mediator
    /// - PuzzlePanel 관리
    /// - ResultPanel 관리
    /// </summary>
    public class GameUIMediator : MonoObject
    {
        [Header("Panels")]
        [SerializeField] private GameObject _puzzlePanel;
        [SerializeField] private GameObject _resultPanel;

        [Header("Puzzle Panel UI")]
        [SerializeField] private TMP_Text _currentLevelText;  // 현재 레벨 표시
        [SerializeField] private Button _settingsButton;      // 설정 버튼

        [Header("Popups")]
        [SerializeField] private SettingsPopup _settingsPopup;

        [Header("Result Panel UI")]
        [SerializeField] private TMP_Text _resultLevelText;
        [SerializeField] private Button _resultNextButton;

        [Header("Puzzle Board")]
        [SerializeField] private PuzzleBoardSetup _puzzleBoardSetup;

        #region MonoObject Lifecycle

        protected override void OnInitialize()
        {
            RegisterButtonEvents();
        }

        private void Start()
        {
            // GameManager에 참조 등록
            if (GameManager.Instance != null)
            {
                GameManager.Instance.puzzleBoard = _puzzleBoardSetup;
            }

            // 게임 시작
            StartGame();
        }

        #endregion

        #region Button Events

        private void RegisterButtonEvents()
        {
            if (_resultNextButton != null)
                _resultNextButton.onClick.AddListener(OnResultNextClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnResultNextClicked()
        {
            // LobbyScene으로 전환
            GameManager.Instance.LoadLobbyScene();
        }

        private void OnSettingsClicked()
        {
            if (_settingsPopup != null)
            {
                // 인게임 모드로 팝업 열기 (Retry, Home 버튼 표시)
                _settingsPopup.Open(isInGame: true);
            }
            else
            {
                Debug.LogWarning("[GameUIMediator] SettingsPopup이 할당되지 않았습니다.");
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 게임 시작 (씬 로드 시 자동 호출)
        /// </summary>
        public void StartGame()
        {
            ShowPuzzle();

            int currentLevel = GameDataContainer.Instance.CurrentLevel;

            // 현재 레벨 텍스트 업데이트
            UpdateCurrentLevelText(currentLevel);

            if (_puzzleBoardSetup != null)
            {
                _puzzleBoardSetup.SetupCurrentLevel(currentLevel);
            }
        }

        /// <summary>
        /// 현재 레벨 텍스트 업데이트
        /// </summary>
        private void UpdateCurrentLevelText(int level)
        {
            if (_currentLevelText != null)
            {
                _currentLevelText.text = $"LEVEL {level}";
            }
        }

        /// <summary>
        /// 퍼즐 화면 표시
        /// </summary>
        public void ShowPuzzle()
        {
            if (_puzzlePanel != null)
                _puzzlePanel.SetActive(true);

            if (_resultPanel != null)
                _resultPanel.SetActive(false);
        }

        /// <summary>
        /// 결과 화면 표시
        /// </summary>
        public void ShowResult()
        {
            if (_puzzlePanel != null)
                _puzzlePanel.SetActive(false);

            if (_resultPanel != null)
                _resultPanel.SetActive(true);

            // 클리어한 레벨 (현재 레벨 - 1)
            int clearedLevel = GameDataContainer.Instance.CurrentLevel - 1;
            UpdateResultLevelText(clearedLevel);
        }

        /// <summary>
        /// 결과 레벨 텍스트 업데이트
        /// </summary>
        public void UpdateResultLevelText(int clearedLevel)
        {
            if (_resultLevelText != null)
            {
                _resultLevelText.text = $"LEVEL {clearedLevel} COMPLETE";
            }
        }

        #endregion
    }
}
