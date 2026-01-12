using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaiJigsaw.Core;
using WaiJigsaw.Data;
using WaiJigsaw.Ads;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 게임 씬 전용 UI Mediator
    /// - PuzzlePanel 관리
    /// - ResultPanel 관리
    /// - LevelClearSequence 연동
    /// </summary>
    public class GameUIMediator : MonoObject
    {
        [Header("Panels")]
        [SerializeField] private GameObject _puzzlePanel;
        [SerializeField] private GameObject _resultPanel;

        [Header("Puzzle Panel UI")]
        [SerializeField] private TMP_Text _currentLevelText;  // 현재 레벨 표시
        [SerializeField] private Button _settingsButton;      // 설정 버튼
        [SerializeField] private GameObject _inGameButtonsContainer;  // 인게임 버튼 컨테이너

        [Header("Coin Display")]
        [SerializeField] private GameObject _coinDisplayObject;  // 코인 표시 UI (CoinDisplay)

        [Header("Popups")]
        [SerializeField] private SettingsPopup _settingsPopup;

        [Header("Result Panel UI (기존 - ShowResultLegacy에서 사용)")]
        [SerializeField] private TMP_Text _resultLevelText;
        [SerializeField] private Button _resultNextButton;

        [Header("Reward UI (기존)")]
        [SerializeField] private GameObject _rewardContainer;     // 보상 UI 컨테이너
        [SerializeField] private Image _coinIcon;                 // 코인 아이콘
        [SerializeField] private TMP_Text _rewardAmountText;      // 보상량 텍스트 (예: "+10")

        [Header("Level Clear Sequence")]
        [Tooltip("레벨 클리어 시퀀스 컴포넌트 (연출 담당)")]
        [SerializeField] private LevelClearSequence _levelClearSequence;

        [Header("Board")]
        [Tooltip("퍼즐 보드 컨테이너 (클리어 시 이동 대상)")]
        [SerializeField] private Transform _boardContainer;

        [Header("Puzzle Board")]
        [SerializeField] private PuzzleBoardSetup _puzzleBoardSetup;

        [Header("Clear Sequence Settings")]
        [Tooltip("클리어 시퀀스 사용 여부 (false면 기존 방식 사용)")]
        [SerializeField] private bool _useClearSequence = true;

        [Header("Hard Intro Sequence")]
        [Tooltip("Hard 난이도 진입 시퀀스 컴포넌트")]
        [SerializeField] private HardIntroSequence _hardIntroSequence;

        [Header("Banner Ad")]
        [Tooltip("배너 광고 영역만큼 위로 올라갈 콘텐츠 영역")]
        [SerializeField] private RectTransform _contentArea;
        [Tooltip("배너 Placeholder를 생성할 Canvas")]
        [SerializeField] private Transform _canvasTransform;

        #region MonoObject Lifecycle

        protected override void OnInitialize()
        {
            RegisterButtonEvents();
            SetupLevelClearSequence();
            ApplyBannerArea();
        }

        protected override void Start()
        {
            // 부모 클래스의 Start() 호출 → OnInitialize() → RegisterButtonEvents()
            base.Start();

            // GameManager에 참조 등록
            if (GameManager.Instance != null)
            {
                GameManager.Instance.puzzleBoard = _puzzleBoardSetup;
            }

            // 게임 시작
            StartGame();
        }

        #endregion

        #region Setup

        /// <summary>
        /// LevelClearSequence 초기 설정
        /// </summary>
        private void SetupLevelClearSequence()
        {
            if (_levelClearSequence == null)
            {
                Debug.LogWarning("[GameUIMediator] LevelClearSequence가 할당되지 않았습니다. 기존 방식으로 동작합니다.");
                _useClearSequence = false;
            }
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
            // 클리어 시퀀스를 사용하는 경우, 시퀀스의 NEXT 버튼 핸들러가 처리
            // 기존 방식 (ResultPanel)에서만 이 핸들러 사용
            if (!_useClearSequence || _levelClearSequence == null)
            {
                GameManager.Instance.LoadLobbyScene();
            }
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
            // 클리어 시퀀스 리셋 (이전 상태 정리)
            if (_levelClearSequence != null)
            {
                _levelClearSequence.ResetSequence();
            }

            // Hard 인트로 시퀀스 리셋
            if (_hardIntroSequence != null)
            {
                _hardIntroSequence.ResetSequence();
            }

            ShowPuzzle();

            int currentLevel = GameDataContainer.Instance.CurrentLevel;

            // 현재 레벨 텍스트 업데이트
            UpdateCurrentLevelText(currentLevel);

            if (_puzzleBoardSetup != null)
            {
                _puzzleBoardSetup.SetupCurrentLevel(currentLevel);
            }

            // Hard 난이도인 경우 인트로 시퀀스 재생
            CheckAndPlayHardIntro(currentLevel);
        }

        /// <summary>
        /// Hard 난이도인지 확인하고 인트로 시퀀스를 재생합니다.
        /// </summary>
        private void CheckAndPlayHardIntro(int levelNumber)
        {
            // LevelTable에서 난이도 확인
            LevelTableRecord levelRecord = LevelTable.Get(levelNumber);
            if (levelRecord == null)
            {
                Debug.LogWarning($"[GameUIMediator] LevelTable에서 레벨 {levelNumber}를 찾을 수 없습니다.");
                return;
            }

            // Hard 난이도인 경우 인트로 재생
            bool isHard = string.Equals(levelRecord.difficulty, "Hard", System.StringComparison.OrdinalIgnoreCase);

            if (isHard && _hardIntroSequence != null)
            {
                Debug.Log($"[GameUIMediator] 레벨 {levelNumber}은 Hard 난이도 - 인트로 시퀀스 시작");
                _hardIntroSequence.PlayHardIntro(OnHardIntroComplete);
            }
        }

        /// <summary>
        /// Hard 인트로 시퀀스 완료 콜백
        /// </summary>
        private void OnHardIntroComplete()
        {
            Debug.Log("[GameUIMediator] Hard 인트로 시퀀스 완료 - 플레이 시작");
            // 추가 로직이 필요하면 여기에 구현
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
        /// 결과 화면 표시 (레벨 클리어 시 호출)
        /// - 클리어 시퀀스 사용 시: 연출 재생
        /// - 기존 방식: 즉시 ResultPanel 표시
        /// </summary>
        public void ShowResult()
        {
            // 클리어한 레벨 (현재 레벨 - 1, 이미 AdvanceToNextLevel이 호출된 후임)
            int clearedLevel = GameDataContainer.Instance.CurrentLevel - 1;

            if (_useClearSequence && _levelClearSequence != null)
            {
                // 클리어 시퀀스 재생
                Debug.Log($"[GameUIMediator] 레벨 {clearedLevel} 클리어 시퀀스 시작");
                _levelClearSequence.PlayClearSequence(clearedLevel, OnClearSequenceComplete);
            }
            else
            {
                // 기존 방식 사용
                ShowResultLegacy(clearedLevel);
            }
        }

        /// <summary>
        /// 클리어 시퀀스 완료 콜백
        /// </summary>
        private void OnClearSequenceComplete()
        {
            Debug.Log("[GameUIMediator] 클리어 시퀀스 완료 - 로비로 이동");
            GameManager.Instance.LoadLobbyScene();
        }

        /// <summary>
        /// 기존 방식의 결과 화면 표시 (즉시 전환)
        /// </summary>
        private void ShowResultLegacy(int clearedLevel)
        {
            if (_puzzlePanel != null)
                _puzzlePanel.SetActive(false);

            if (_resultPanel != null)
                _resultPanel.SetActive(true);

            UpdateResultLevelText(clearedLevel);

            // 코인 보상 표시 및 지급
            DisplayAndAwardCoinReward(clearedLevel);
        }

        /// <summary>
        /// 코인 보상 표시 및 지급 (기존 방식)
        /// </summary>
        private void DisplayAndAwardCoinReward(int clearedLevel)
        {
            // LevelTable에서 보상량 가져오기
            LevelTableRecord levelRecord = LevelTable.Get(clearedLevel);
            int rewardAmount = levelRecord?.reward ?? 0;

            if (rewardAmount > 0)
            {
                // 코인 지급
                GameDataContainer.Instance.AddCoin(rewardAmount);
                GameDataContainer.Instance.Save();

                // UI 표시
                if (_rewardContainer != null)
                    _rewardContainer.SetActive(true);

                if (_rewardAmountText != null)
                    _rewardAmountText.text = $"+{rewardAmount}";

                // 코인 아이콘 설정 (OutgameResourcePath에서 로드)
                if (_coinIcon != null)
                {
                    Sprite coinSprite = ItemTable.GetCoinIcon();
                    if (coinSprite != null)
                    {
                        _coinIcon.sprite = coinSprite;
                    }
                }

                Debug.Log($"[GameUIMediator] 레벨 {clearedLevel} 클리어 보상: +{rewardAmount} 코인 (총 {GameDataContainer.Instance.Coin})");
            }
            else
            {
                // 보상이 없으면 UI 숨김
                if (_rewardContainer != null)
                    _rewardContainer.SetActive(false);
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

        #region Banner Ad

        /// <summary>
        /// 배너 광고 영역을 적용합니다.
        /// - 콘텐츠 영역 하단 오프셋 설정
        /// - 배너 Placeholder 생성 (디버그용)
        /// </summary>
        private void ApplyBannerArea()
        {
            if (BannerManager.Instance == null) return;

            // 콘텐츠 영역에 배너 높이만큼 하단 오프셋 적용
            if (_contentArea != null)
            {
                BannerManager.Instance.ApplyBannerOffset(_contentArea);
            }

            // 배너 Placeholder 생성 (디버그 모드에서만 표시)
            if (_canvasTransform != null)
            {
                BannerManager.Instance.CreateBannerPlaceholder(_canvasTransform);
            }
        }

        #endregion

        #region Public Accessors (for LevelClearSequence)

        /// <summary>
        /// 현재 레벨 텍스트 (클리어 시퀀스에서 페이드 아웃용)
        /// </summary>
        public TMP_Text CurrentLevelText => _currentLevelText;

        /// <summary>
        /// 설정 버튼 (클리어 시퀀스에서 페이드 아웃용)
        /// </summary>
        public Button SettingsButton => _settingsButton;

        /// <summary>
        /// 인게임 버튼 컨테이너 (클리어 시퀀스에서 페이드 아웃용)
        /// </summary>
        public GameObject InGameButtonsContainer => _inGameButtonsContainer;

        /// <summary>
        /// 코인 표시 오브젝트 (클리어 시퀀스에서 페이드 인용)
        /// </summary>
        public GameObject CoinDisplayObject => _coinDisplayObject;

        /// <summary>
        /// 보드 컨테이너 (클리어 시퀀스에서 이동용)
        /// </summary>
        public Transform BoardContainer => _boardContainer;

        #endregion
    }
}
