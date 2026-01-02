using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaiJigsaw.Core;
using WaiJigsaw.Data;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 로비 씬 전용 UI Mediator
    /// - HomePanel 관리
    /// - LobbyGridManager 연동
    /// - LevelChangedEvent 구독
    /// </summary>
    public class LobbyUIMediator : MonoObject
    {
        [Header("Panels")]
        [SerializeField] private GameObject _homePanel;

        [Header("Home Panel UI")]
        [SerializeField] private Button _playButton;
        [SerializeField] private TMP_Text _playButtonText;
        [SerializeField] private Button _settingsButton;

        [Header("Lobby Grid")]
        [SerializeField] private LobbyGridManager _lobbyGridManager;

        #region MonoObject Lifecycle

        protected override void OnEnabled()
        {
            // LevelChangedEvent 구독 (MonoObject가 자동 해제 관리)
            RegisterLevelChangedObserver(OnLevelChanged);
        }

        protected override void OnInitialize()
        {
            RegisterButtonEvents();

            // 초기 UI 설정
            ShowHome();
        }

        #endregion

        #region Button Events

        private void RegisterButtonEvents()
        {
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlayClicked);

            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
        }

        private void OnPlayClicked()
        {
            // GameScene으로 전환
            GameManager.Instance.LoadGameScene();
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[LobbyUIMediator] 설정 버튼 클릭됨 (구현 예정)");
        }

        #endregion

        #region Observer Handlers

        private void OnLevelChanged(LevelChangedEvent evt)
        {
            UpdatePlayButtonText(evt.NewLevel);
            Debug.Log($"[LobbyUIMediator] 레벨 변경: {evt.OldLevel} -> {evt.NewLevel}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 홈 화면 표시
        /// </summary>
        public void ShowHome()
        {
            if (_homePanel != null)
                _homePanel.SetActive(true);

            int currentLevel = GameDataContainer.Instance.CurrentLevel;

            // 로비 그리드 설정
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.SetupGrid(currentLevel);
            }

            // 플레이 버튼 텍스트 업데이트
            UpdatePlayButtonText(currentLevel);
        }

        /// <summary>
        /// 플레이 버튼 텍스트 업데이트
        /// </summary>
        public void UpdatePlayButtonText(int currentLevel)
        {
            if (_playButtonText != null)
            {
                _playButtonText.text = $"PLAY\n<size=60%>Level {currentLevel}</size>";
            }
        }

        #endregion
    }
}
