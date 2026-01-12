using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaiJigsaw.Core;
using WaiJigsaw.Data;
using WaiJigsaw.Ads;

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
        [SerializeField] private Button _collectionButton;

        [Header("Lobby Grid")]
        [SerializeField] private LobbyGridManager _lobbyGridManager;

        [Header("Popups")]
        [SerializeField] private SettingsPopup _settingsPopup;
        [SerializeField] private CollectionPopup _collectionPopup;

        [Header("Chapter Clear Sequence")]
        [SerializeField] private ChapterClearSequence _chapterClearSequence;

        [Header("UI Blocking")]
        [SerializeField] private GameObject _blockingPanel;  // 연출 중 터치 차단용 (옵션)

        [Header("Banner Ad")]
        [Tooltip("배너 광고 영역만큼 위로 올라갈 콘텐츠 영역")]
        [SerializeField] private RectTransform _contentArea;
        [Tooltip("배너 Placeholder를 생성할 Canvas")]
        [SerializeField] private Transform _canvasTransform;

        // UI 차단 상태
        private bool _isUIBlocked = false;

        #region MonoObject Lifecycle

        protected override void OnEnabled()
        {
            // 배너 영역 적용
            ApplyBannerArea();

            // LevelChangedEvent 구독 (MonoObject가 자동 해제 관리)
            RegisterLevelChangedObserver(OnLevelChanged);

            // LobbyGridManager 항상 새로 찾기 (씬 전환 시 참조가 Missing될 수 있음)
            _lobbyGridManager = FindObjectOfType<LobbyGridManager>(true);
            if (_lobbyGridManager != null)
            {
                Debug.Log($"[LobbyUIMediator] LobbyGridManager를 찾았습니다: {_lobbyGridManager.gameObject.name}");
            }
            else
            {
                Debug.LogError("[LobbyUIMediator] 씬에서 LobbyGridManager를 찾을 수 없습니다!");
            }

            // LobbyGridManager의 클리어 애니메이션 이벤트 구독
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.OnClearAnimationComplete += OnClearAnimationComplete;
                _lobbyGridManager.OnChapterCleared += OnChapterCleared;
            }

            // ChapterClearSequence 이벤트 구독
            if (_chapterClearSequence != null)
            {
                _chapterClearSequence.OnSequenceComplete += OnChapterClearSequenceComplete;
                _chapterClearSequence.OnRequestNextChapterCards += OnRequestNextChapterCards;
            }
        }

        protected override void OnDisabled()
        {
            // 이벤트 구독 해제
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.OnClearAnimationComplete -= OnClearAnimationComplete;
                _lobbyGridManager.OnChapterCleared -= OnChapterCleared;
            }

            if (_chapterClearSequence != null)
            {
                _chapterClearSequence.OnSequenceComplete -= OnChapterClearSequenceComplete;
                _chapterClearSequence.OnRequestNextChapterCards -= OnRequestNextChapterCards;
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

            if (_collectionButton != null)
                _collectionButton.onClick.AddListener(OnCollectionClicked);
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

        private void OnCollectionClicked()
        {
            if (_collectionPopup != null)
            {
                _collectionPopup.Open();
            }
            else
            {
                Debug.LogWarning("[LobbyUIMediator] CollectionPopup이 할당되지 않았습니다.");
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
            // 챕터 클리어 시퀀스가 재생 중이면 UI 차단 해제하지 않음
            if (_chapterClearSequence != null && _chapterClearSequence.IsPlaying)
            {
                return;
            }

            UnblockUI();
            Debug.Log("[LobbyUIMediator] 클리어 애니메이션 완료 - UI 차단 해제");
        }

        /// <summary>
        /// 챕터 클리어 시 호출됩니다 (마지막 레벨 클리어 후).
        /// </summary>
        private void OnChapterCleared(LevelGroupTableRecord clearedGroup, Sprite completedSprite)
        {
            Debug.Log($"[LobbyUIMediator] 챕터 {clearedGroup.GroupID} 클리어 - 시퀀스 시작");

            // UI 차단 유지
            BlockUI();

            // 챕터 클리어 시퀀스 재생 (LobbyGridManager 참조 전달)
            if (_chapterClearSequence != null)
            {
                _chapterClearSequence.Play(clearedGroup, completedSprite, _lobbyGridManager);
            }
            else
            {
                Debug.LogWarning("[LobbyUIMediator] ChapterClearSequence가 할당되지 않았습니다.");
                UnblockUI();
            }
        }

        /// <summary>
        /// 챕터 클리어 시퀀스 완료 시 호출됩니다.
        /// </summary>
        private void OnChapterClearSequenceComplete()
        {
            UnblockUI();
            Debug.Log("[LobbyUIMediator] 챕터 클리어 시퀀스 완료 - UI 차단 해제");
        }

        /// <summary>
        /// 다음 챕터 카드 뿌리기 요청 시 호출됩니다.
        /// </summary>
        private void OnRequestNextChapterCards()
        {
            int currentLevel = GameDataContainer.Instance.CurrentLevel;
            Debug.Log($"[LobbyUIMediator] 다음 챕터 카드 뿌리기 요청 - CurrentLevel: {currentLevel}");

            // _lobbyGridManager가 null이면 다시 찾기
            if (_lobbyGridManager == null)
            {
                Debug.Log("[LobbyUIMediator] _lobbyGridManager가 null - 재검색 시작");

                // 1. LevelGroupManager.Instance에서 찾기 (가장 신뢰할 수 있는 방법)
                if (LevelGroupManager.Instance != null)
                {
                    _lobbyGridManager = LevelGroupManager.Instance.GetComponent<LobbyGridManager>();
                    if (_lobbyGridManager != null)
                    {
                        Debug.Log($"[LobbyUIMediator] LevelGroupManager.Instance에서 LobbyGridManager를 찾았습니다: {_lobbyGridManager.gameObject.name}");
                    }
                }

                // 2. 여전히 못 찾았으면 FindObjectOfType 시도
                if (_lobbyGridManager == null)
                {
                    _lobbyGridManager = FindObjectOfType<LobbyGridManager>(true);
                    if (_lobbyGridManager != null)
                    {
                        Debug.Log($"[LobbyUIMediator] FindObjectOfType으로 LobbyGridManager를 찾았습니다: {_lobbyGridManager.gameObject.name}");
                    }
                }

                if (_lobbyGridManager != null)
                {
                    // 이벤트 재구독
                    _lobbyGridManager.OnClearAnimationComplete += OnClearAnimationComplete;
                    _lobbyGridManager.OnChapterCleared += OnChapterCleared;
                }
            }

            // 다음 레벨로 그리드 갱신 (딜링 애니메이션과 함께)
            if (_lobbyGridManager != null)
            {
                Debug.Log($"[LobbyUIMediator] _lobbyGridManager 호출 시작");
                _lobbyGridManager.SetupGridWithDealingAnimation(currentLevel);
                Debug.Log($"[LobbyUIMediator] _lobbyGridManager 호출 완료");
            }
            else
            {
                Debug.LogError("[LobbyUIMediator] _lobbyGridManager가 null입니다! 찾을 수 없습니다.");
            }

            // 플레이 버튼 텍스트 업데이트
            UpdatePlayButtonText(currentLevel);
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

        #region Banner Ad

        /// <summary>
        /// 배너 광고 영역을 적용합니다.
        /// </summary>
        private void ApplyBannerArea()
        {
            if (BannerManager.Instance == null) return;

            // 콘텐츠 영역에 배너 오프셋 적용
            if (_contentArea != null)
            {
                BannerManager.Instance.ApplyBannerOffset(_contentArea);
            }

            // 배너 Placeholder 생성 (개발용)
            if (_canvasTransform != null)
            {
                BannerManager.Instance.CreateBannerPlaceholder(_canvasTransform);
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

            if (_collectionButton != null)
                _collectionButton.interactable = false;

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

            if (_collectionButton != null)
                _collectionButton.interactable = true;

            // 차단 패널 비활성화
            if (_blockingPanel != null)
                _blockingPanel.SetActive(false);
        }

        #endregion
    }
}
