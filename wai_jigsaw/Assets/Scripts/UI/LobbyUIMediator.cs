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

        [Header("Popups")]
        [SerializeField] private SettingsPopup _settingsPopup;

        [Header("UI Blocking")]
        [SerializeField] private GameObject _blockingPanel;  // 연출 중 터치 차단용 (옵션)

        // UI 차단 상태
        private bool _isUIBlocked = false;

        #region MonoObject Lifecycle

        protected override void OnEnabled()
        {
            // LevelChangedEvent 구독 (MonoObject가 자동 해제 관리)
            RegisterLevelChangedObserver(OnLevelChanged);

            // LobbyGridManager의 클리어 애니메이션 이벤트 구독
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.OnClearAnimationComplete += OnClearAnimationComplete;
            }
        }

        protected override void OnDisabled()
        {
            // 이벤트 구독 해제
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.OnClearAnimationComplete -= OnClearAnimationComplete;
            }
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
            if (_settingsPopup != null)
            {
                _settingsPopup.Open();
            }
            else
            {
                Debug.LogWarning("[LobbyUIMediator] SettingsPopup이 할당되지 않았습니다.");
            }
        }

        #endregion

        #region Observer Handlers

        private void OnLevelChanged(LevelChangedEvent evt)
        {
            UpdatePlayButtonText(evt.NewLevel);
            Debug.Log($"[LobbyUIMediator] 레벨 변경: {evt.OldLevel} -> {evt.NewLevel}");
        }

        /// <summary>
        /// 클리어 애니메이션 완료 시 호출됩니다.
        /// </summary>
        private void OnClearAnimationComplete()
        {
            UnblockUI();
            Debug.Log("[LobbyUIMediator] 클리어 애니메이션 완료 - UI 차단 해제");
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

            // 로비 그리드 설정 (내부에서 클리어 연출 체크)
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.SetupGrid(currentLevel);

                // 클리어 연출이 실행 중이면 UI 차단
                if (_lobbyGridManager.IsPlayingClearAnimation)
                {
                    BlockUI();
                    Debug.Log("[LobbyUIMediator] 클리어 애니메이션 시작 - UI 차단");
                }
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

        #region UI Blocking

        /// <summary>
        /// UI 상호작용을 차단합니다 (연출 중 사용).
        /// </summary>
        private void BlockUI()
        {
            if (_isUIBlocked) return;

            _isUIBlocked = true;

            // 버튼 비활성화
            if (_playButton != null)
                _playButton.interactable = false;

            if (_settingsButton != null)
                _settingsButton.interactable = false;

            // 차단 패널 활성화 (설정된 경우)
            if (_blockingPanel != null)
                _blockingPanel.SetActive(true);
        }

        /// <summary>
        /// UI 상호작용 차단을 해제합니다.
        /// </summary>
        private void UnblockUI()
        {
            if (!_isUIBlocked) return;

            _isUIBlocked = false;

            // 버튼 활성화
            if (_playButton != null)
                _playButton.interactable = true;

            if (_settingsButton != null)
                _settingsButton.interactable = true;

            // 차단 패널 비활성화
            if (_blockingPanel != null)
                _blockingPanel.SetActive(false);
        }

        #endregion
    }
}
