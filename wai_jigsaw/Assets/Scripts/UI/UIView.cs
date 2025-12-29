using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// UI View - UI 컴포넌트 참조 및 표시 메서드만 담당
    /// - 로직 없음, 순수 표시 역할
    /// - Mediator에서 이 클래스의 메서드를 호출하여 UI 업데이트
    /// </summary>
    public class UIView : MonoBehaviour
    {
        [Header("Game Panels")]
        [SerializeField] private GameObject _homePanel;
        [SerializeField] private GameObject _levelIntroPanel;
        [SerializeField] private GameObject _puzzlePanel;
        [SerializeField] private GameObject _resultPanel;

        [Header("Home Panel UI")]
        [SerializeField] private TMP_Text _homeTitleText;
        [SerializeField] private Button _homeSettingsButton;
        [SerializeField] private Button _homePlayButton;
        [SerializeField] private TMP_Text _homePlayButtonText;

        [Header("Lobby Grid")]
        [SerializeField] private LobbyGridManager _lobbyGridManager;

        [Header("Level Intro Panel UI")]
        [SerializeField] private TMP_Text _introLevelText;
        [SerializeField] private Image _introImagePreview;
        [SerializeField] private TMP_Text _introPiecesText;
        [SerializeField] private Button _introPlayButton;

        [Header("Result Panel UI")]
        [SerializeField] private TMP_Text _resultLevelText;
        [SerializeField] private Button _resultNextButton;

        #region Properties (Mediator에서 버튼 이벤트 등록용)

        public Button HomeSettingsButton => _homeSettingsButton;
        public Button HomePlayButton => _homePlayButton;
        public Button IntroPlayButton => _introPlayButton;
        public Button ResultNextButton => _resultNextButton;
        public LobbyGridManager LobbyGridManager => _lobbyGridManager;

        #endregion

        #region Panel 표시 메서드

        /// <summary>
        /// 홈 패널 표시
        /// </summary>
        public void ShowHomePanel()
        {
            ActivatePanel(_homePanel);
        }

        /// <summary>
        /// 레벨 인트로 패널 표시
        /// </summary>
        public void ShowLevelIntroPanel()
        {
            ActivatePanel(_levelIntroPanel);
        }

        /// <summary>
        /// 퍼즐 패널 표시
        /// </summary>
        public void ShowPuzzlePanel()
        {
            ActivatePanel(_puzzlePanel);
        }

        /// <summary>
        /// 결과 패널 표시
        /// </summary>
        public void ShowResultPanel()
        {
            ActivatePanel(_resultPanel);
        }

        /// <summary>
        /// 특정 패널만 활성화하고 나머지는 비활성화
        /// </summary>
        private void ActivatePanel(GameObject targetPanel)
        {
            if (_homePanel != null) _homePanel.SetActive(targetPanel == _homePanel);
            if (_levelIntroPanel != null) _levelIntroPanel.SetActive(targetPanel == _levelIntroPanel);
            if (_puzzlePanel != null) _puzzlePanel.SetActive(targetPanel == _puzzlePanel);
            if (_resultPanel != null) _resultPanel.SetActive(targetPanel == _resultPanel);
        }

        #endregion

        #region UI 업데이트 메서드

        /// <summary>
        /// 홈 플레이 버튼 텍스트 업데이트
        /// </summary>
        public void UpdateHomePlayButtonText(int currentLevel)
        {
            if (_homePlayButtonText != null)
            {
                _homePlayButtonText.text = $"PLAY\n<size=60%>Level {currentLevel}</size>";
            }
        }

        /// <summary>
        /// 레벨 인트로 텍스트 업데이트
        /// </summary>
        public void UpdateIntroLevelText(int levelNumber)
        {
            if (_introLevelText != null)
            {
                _introLevelText.text = $"LEVEL {levelNumber}";
            }
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

        #region 상태 확인

        /// <summary>
        /// 홈 패널이 활성화 상태인지 확인
        /// </summary>
        public bool IsHomePanelActive => _homePanel != null && _homePanel.activeSelf;

        #endregion
    }
}
