using UnityEngine;
using WaiJigsaw.Core;
using WaiJigsaw.Data;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// UI Mediator - 비즈니스 로직 담당
    /// - MonoObject 상속으로 표준화된 생명주기 관리
    /// - 버튼 클릭 핸들러
    /// - Observer 자동 해제
    /// - GameManager 호출
    /// - View를 통해 UI 업데이트
    /// </summary>
    public class UIMediator : MonoObject
    {
        [Header("References")]
        [SerializeField] private UIView _view;

        #region MonoObject Lifecycle

        protected override void OnEnabled()
        {
            // LevelChangedEvent 구독 (MonoObject가 자동 해제 관리)
            RegisterLevelChangedObserver(OnLevelChanged);
        }

        protected override void OnInitialize()
        {
            RegisterButtonEvents();
        }

        #endregion

        #region Button Event Registration

        private void RegisterButtonEvents()
        {
            // Home Panel
            if (_view.HomePlayButton != null)
                _view.HomePlayButton.onClick.AddListener(OnHomePlayClicked);

            if (_view.HomeSettingsButton != null)
                _view.HomeSettingsButton.onClick.AddListener(OnHomeSettingsClicked);

            // Intro Panel
            if (_view.IntroPlayButton != null)
                _view.IntroPlayButton.onClick.AddListener(OnIntroPlayClicked);

            // Result Panel
            if (_view.ResultNextButton != null)
                _view.ResultNextButton.onClick.AddListener(OnResultNextClicked);
        }

        #endregion

        #region Observer Handlers

        private void OnLevelChanged(LevelChangedEvent evt)
        {
            // 홈 패널이 활성화된 상태라면 UI 업데이트
            if (_view.IsHomePanelActive)
            {
                _view.UpdateHomePlayButtonText(evt.NewLevel);
            }

            Debug.Log($"[UIMediator] 레벨 변경: {evt.OldLevel} -> {evt.NewLevel}");
        }

        #endregion

        #region Public Methods (GameManager에서 호출)

        /// <summary>
        /// 홈 화면 표시
        /// </summary>
        public void ShowHome()
        {
            _view.ShowHomePanel();

            int currentLevel = GameDataContainer.Instance.CurrentLevel;

            // 로비 그리드 설정
            if (_view.LobbyGridManager != null)
            {
                _view.LobbyGridManager.SetupGrid(currentLevel);
            }

            // 플레이 버튼 텍스트 업데이트
            _view.UpdateHomePlayButtonText(currentLevel);
        }

        /// <summary>
        /// 퍼즐 화면 표시
        /// </summary>
        public void ShowPuzzle()
        {
            _view.ShowPuzzlePanel();
        }

        /// <summary>
        /// 결과 화면 표시
        /// </summary>
        public void ShowResult()
        {
            _view.ShowResultPanel();

            // 클리어한 레벨 (현재 레벨 - 1)
            int clearedLevel = GameDataContainer.Instance.CurrentLevel - 1;
            _view.UpdateResultLevelText(clearedLevel);
        }

        /// <summary>
        /// 레벨 인트로 화면 표시
        /// </summary>
        public void ShowLevelIntro(LevelConfig config)
        {
            _view.ShowLevelIntroPanel();
            _view.UpdateIntroLevelText(config.levelNumber);
        }

        #endregion

        #region Button Click Handlers

        private void OnHomePlayClicked()
        {
            GameManager.Instance.StartCurrentLevel();
        }

        private void OnHomeSettingsClicked()
        {
            Debug.Log("[UIMediator] 설정 버튼 클릭됨 (구현 예정)");
            // 설정 팝업 로직 추가 가능
        }

        private void OnIntroPlayClicked()
        {
            GameManager.Instance.StartCurrentLevel();
        }

        private void OnResultNextClicked()
        {
            GameManager.Instance.GoToHome();
        }

        #endregion
    }
}
