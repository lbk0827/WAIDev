using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WaiJigsaw.Core;
using WaiJigsaw.Data;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 설정 팝업 UI
    /// - BGM On/Off 토글
    /// - SFX On/Off 토글
    /// - Haptic On/Off 토글
    /// </summary>
    public class SettingsPopup : MonoBehaviour
    {
        [Header("Dim Background")]
        [SerializeField] private Button _dimButton;  // 배경 클릭 시 닫기

        [Header("Close Button")]
        [SerializeField] private Button _closeButton;

        [Header("Toggles")]
        [SerializeField] private Toggle _bgmToggle;
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _hapticToggle;

        [Header("Off Indicator Images")]
        [SerializeField] private GameObject _bgmOffImage;
        [SerializeField] private GameObject _sfxOffImage;
        [SerializeField] private GameObject _hapticOffImage;

        [Header("Animation (Optional)")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _popupPanel;
        [SerializeField] private float _animationDuration = 0.2f;

        [Header("InGame Only Buttons")]
        [SerializeField] private GameObject _inGameButtonsContainer;  // Retry, Home 버튼을 담는 컨테이너
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        [Header("Data Reset")]
        [SerializeField] private Button _resetDataButton;  // 데이터 초기화 버튼

        [Header("Setting Buttons")]
        [SerializeField] private Button _policyButton;     // 개인정보 처리방침
        [SerializeField] private Button _websiteButton;    // 웹사이트
        [SerializeField] private Button _languageButton;   // 언어 설정

        [Header("Debug Level Change (Editor Only)")]
        [SerializeField] private GameObject _debugLevelContainer;  // 디버그 레벨 변경 컨테이너
        [SerializeField] private TMP_InputField _levelInputField;  // 레벨 입력 필드
        [SerializeField] private Button _setLevelButton;           // 레벨 설정 버튼

        // 인게임 모드 여부
        private bool _isInGameMode = false;
        private bool _isInitialized = false;

        private void Awake()
        {
            RegisterButtonEvents();
            _isInitialized = true;
        }

        private void OnEnable()
        {
            // Awake가 호출되지 않은 경우 대비 (Prefab 인스턴스 등)
            if (!_isInitialized)
            {
                RegisterButtonEvents();
                _isInitialized = true;
            }

            SyncToggleStates();
            PlayOpenAnimation();
        }

        private void RegisterButtonEvents()
        {
            if (_dimButton != null)
                _dimButton.onClick.AddListener(Close);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);

            if (_bgmToggle != null)
                _bgmToggle.onValueChanged.AddListener(OnBGMToggleChanged);

            if (_sfxToggle != null)
                _sfxToggle.onValueChanged.AddListener(OnSFXToggleChanged);

            if (_hapticToggle != null)
                _hapticToggle.onValueChanged.AddListener(OnHapticToggleChanged);

            // InGame 전용 버튼
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);

            if (_homeButton != null)
                _homeButton.onClick.AddListener(OnHomeClicked);

            // 데이터 초기화 버튼
            if (_resetDataButton != null)
                _resetDataButton.onClick.AddListener(OnResetDataClicked);

            // 설정 버튼들
            if (_policyButton != null)
                _policyButton.onClick.AddListener(OnPolicyClicked);

            if (_websiteButton != null)
                _websiteButton.onClick.AddListener(OnWebsiteClicked);

            if (_languageButton != null)
                _languageButton.onClick.AddListener(OnLanguageClicked);

            // 디버그 레벨 변경 버튼
            if (_setLevelButton != null)
                _setLevelButton.onClick.AddListener(OnSetLevelClicked);
        }

        #region InGame Button Events

        private void OnRetryClicked()
        {
            Close();
            // 현재 레벨 재시작
            GameManager.Instance?.RetryCurrentLevel();
        }

        private void OnHomeClicked()
        {
            Close();
            // 로비로 이동
            GameManager.Instance?.LoadLobbyScene();
        }

        private void OnResetDataClicked()
        {
            // 모든 진행 데이터 초기화
            GameDataContainer.Instance.ResetAllProgress();

            Close();

            // 로비로 이동 (Level 1부터 다시 시작)
            GameManager.Instance?.LoadLobbyScene();
        }

        /// <summary>
        /// 디버그용 레벨 변경 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnSetLevelClicked()
        {
            if (_levelInputField == null) return;

            string inputText = _levelInputField.text;

            if (int.TryParse(inputText, out int targetLevel))
            {
                if (targetLevel >= 1)
                {
                    // 레벨 변경
                    GameDataContainer.Instance.SetCurrentLevel(targetLevel);
                    GameDataContainer.Instance.Save();

                    Debug.Log($"[SettingsPopup] 레벨을 {targetLevel}로 변경했습니다.");

                    Close();

                    // 로비로 이동하여 변경 사항 반영
                    GameManager.Instance?.LoadLobbyScene();
                }
                else
                {
                    Debug.LogWarning("[SettingsPopup] 레벨은 1 이상이어야 합니다.");
                }
            }
            else
            {
                Debug.LogWarning($"[SettingsPopup] 유효하지 않은 레벨 입력: {inputText}");
            }
        }

        #endregion

        #region Setting Button Events

        /// <summary>
        /// 개인정보 처리방침 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnPolicyClicked()
        {
            // TODO: 개인정보 처리방침 페이지 열기
        }

        /// <summary>
        /// 웹사이트 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnWebsiteClicked()
        {
            // TODO: 웹사이트 열기
        }

        /// <summary>
        /// 언어 설정 버튼 클릭 시 호출됩니다.
        /// </summary>
        private void OnLanguageClicked()
        {
            // TODO: 언어 설정 팝업 열기
        }

        #endregion

        #region Toggle Events

        /// <summary>
        /// BGM 토글 값 변경 시 호출됩니다.
        /// </summary>
        private void OnBGMToggleChanged(bool isOn)
        {
            if (GameOptionManager.Instance == null) return;

            GameOptionManager.Instance.BGMEnabled = isOn;
            UpdateOffIndicator(_bgmOffImage, isOn);
        }

        /// <summary>
        /// SFX 토글 값 변경 시 호출됩니다.
        /// </summary>
        private void OnSFXToggleChanged(bool isOn)
        {
            if (GameOptionManager.Instance == null) return;

            GameOptionManager.Instance.SFXEnabled = isOn;
            UpdateOffIndicator(_sfxOffImage, isOn);
        }

        /// <summary>
        /// Haptic 토글 값 변경 시 호출됩니다.
        /// </summary>
        private void OnHapticToggleChanged(bool isOn)
        {
            if (GameOptionManager.Instance == null) return;

            GameOptionManager.Instance.HapticEnabled = isOn;
            UpdateOffIndicator(_hapticOffImage, isOn);

            // 진동 활성화 시 피드백
            if (isOn)
            {
                GameOptionManager.Instance.PlayHaptic();
            }
        }

        /// <summary>
        /// Off 인디케이터 이미지 표시/숨김
        /// </summary>
        private void UpdateOffIndicator(GameObject offImage, bool isOn)
        {
            if (offImage != null)
            {
                // Off 상태일 때만 Off 이미지 표시
                offImage.SetActive(!isOn);
            }
        }

        #endregion

        #region Toggle Sync

        /// <summary>
        /// 팝업 열릴 때 Toggle 상태를 GameOptionManager 값과 동기화합니다.
        /// </summary>
        private void SyncToggleStates()
        {
            if (GameOptionManager.Instance == null) return;

            bool bgmOn = GameOptionManager.Instance.BGMEnabled;
            bool sfxOn = GameOptionManager.Instance.SFXEnabled;
            bool hapticOn = GameOptionManager.Instance.HapticEnabled;

            // 이벤트 발생 없이 Toggle 값만 변경 (SetIsOnWithoutNotify)
            if (_bgmToggle != null)
                _bgmToggle.SetIsOnWithoutNotify(bgmOn);

            if (_sfxToggle != null)
                _sfxToggle.SetIsOnWithoutNotify(sfxOn);

            if (_hapticToggle != null)
                _hapticToggle.SetIsOnWithoutNotify(hapticOn);

            // Off 이미지 상태 동기화
            UpdateOffIndicator(_bgmOffImage, bgmOn);
            UpdateOffIndicator(_sfxOffImage, sfxOn);
            UpdateOffIndicator(_hapticOffImage, hapticOn);
        }

        #endregion

        #region Open/Close

        /// <summary>
        /// 설정 팝업을 엽니다.
        /// </summary>
        /// <param name="isInGame">인게임 모드 여부 (true면 Retry, Home 버튼 표시)</param>
        public void Open(bool isInGame = false)
        {
            _isInGameMode = isInGame;

            // 인게임 버튼 컨테이너 표시/숨김
            if (_inGameButtonsContainer != null)
            {
                _inGameButtonsContainer.SetActive(isInGame);
            }

            // 팝업 열림 상태 설정 (퍼즐 드래그 차단)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.IsPopupOpen = true;
            }

            gameObject.SetActive(true);
        }

        public void Close()
        {
            // 팝업 닫힘 상태 설정 (퍼즐 드래그 허용)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.IsPopupOpen = false;
            }

            PlayCloseAnimation(() =>
            {
                gameObject.SetActive(false);
            });
        }

        private void PlayOpenAnimation()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                StartCoroutine(FadeIn());
            }

            if (_popupPanel != null)
            {
                _popupPanel.localScale = Vector3.one * 0.8f;
                StartCoroutine(ScaleIn());
            }
        }

        private void PlayCloseAnimation(System.Action onComplete)
        {
            if (_canvasGroup != null)
            {
                StartCoroutine(FadeOut(onComplete));
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        private System.Collections.IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / _animationDuration);
                yield return null;
            }
            _canvasGroup.alpha = 1f;
        }

        private System.Collections.IEnumerator FadeOut(System.Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / _animationDuration);
                yield return null;
            }
            _canvasGroup.alpha = 0f;
            onComplete?.Invoke();
        }

        private System.Collections.IEnumerator ScaleIn()
        {
            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _animationDuration;
                // Ease out back
                float easedT = 1f + 1.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
                _popupPanel.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, easedT);
                yield return null;
            }
            _popupPanel.localScale = Vector3.one;
        }

        #endregion
    }
}
