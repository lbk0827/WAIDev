using UnityEngine;
using UnityEngine.UI;
using WaiJigsaw.Core;

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

        [Header("Toggle Buttons")]
        [SerializeField] private Button _bgmToggleButton;
        [SerializeField] private Button _sfxToggleButton;
        [SerializeField] private Button _hapticToggleButton;

        [Header("Toggle Visual Elements")]
        [SerializeField] private GameObject _bgmOnIndicator;
        [SerializeField] private GameObject _bgmOffIndicator;
        [SerializeField] private GameObject _sfxOnIndicator;
        [SerializeField] private GameObject _sfxOffIndicator;
        [SerializeField] private GameObject _hapticOnIndicator;
        [SerializeField] private GameObject _hapticOffIndicator;

        [Header("Animation (Optional)")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _popupPanel;
        [SerializeField] private float _animationDuration = 0.2f;

        [Header("InGame Only Buttons")]
        [SerializeField] private GameObject _inGameButtonsContainer;  // Retry, Home 버튼을 담는 컨테이너
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _homeButton;

        // 인게임 모드 여부
        private bool _isInGameMode = false;
        private bool _isInitialized = false;

        private void Awake()
        {
            // Awake는 비활성화 상태에서도 호출됨
            Debug.Log("[SettingsPopup] Awake 호출됨");
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

            RefreshToggleVisuals();
            PlayOpenAnimation();
        }

        private void RegisterButtonEvents()
        {
            Debug.Log($"[SettingsPopup] RegisterButtonEvents - dimButton: {_dimButton != null}, closeButton: {_closeButton != null}");

            if (_dimButton != null)
                _dimButton.onClick.AddListener(Close);

            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);

            if (_bgmToggleButton != null)
                _bgmToggleButton.onClick.AddListener(OnBGMToggleClicked);

            if (_sfxToggleButton != null)
                _sfxToggleButton.onClick.AddListener(OnSFXToggleClicked);

            if (_hapticToggleButton != null)
                _hapticToggleButton.onClick.AddListener(OnHapticToggleClicked);

            // InGame 전용 버튼
            if (_retryButton != null)
                _retryButton.onClick.AddListener(OnRetryClicked);

            if (_homeButton != null)
                _homeButton.onClick.AddListener(OnHomeClicked);
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

        #endregion

        #region Toggle Events

        private void OnBGMToggleClicked()
        {
            if (GameOptionManager.Instance == null) return;

            GameOptionManager.Instance.BGMEnabled = !GameOptionManager.Instance.BGMEnabled;
            UpdateBGMVisual();
        }

        private void OnSFXToggleClicked()
        {
            if (GameOptionManager.Instance == null) return;

            GameOptionManager.Instance.SFXEnabled = !GameOptionManager.Instance.SFXEnabled;
            UpdateSFXVisual();
        }

        private void OnHapticToggleClicked()
        {
            if (GameOptionManager.Instance == null) return;

            GameOptionManager.Instance.HapticEnabled = !GameOptionManager.Instance.HapticEnabled;
            UpdateHapticVisual();

            // 진동 활성화 시 피드백
            if (GameOptionManager.Instance.HapticEnabled)
            {
                GameOptionManager.Instance.PlayHaptic();
            }
        }

        #endregion

        #region Visual Updates

        private void RefreshToggleVisuals()
        {
            UpdateBGMVisual();
            UpdateSFXVisual();
            UpdateHapticVisual();
        }

        private void UpdateBGMVisual()
        {
            if (GameOptionManager.Instance == null) return;

            bool isOn = GameOptionManager.Instance.BGMEnabled;

            if (_bgmOnIndicator != null)
                _bgmOnIndicator.SetActive(isOn);

            if (_bgmOffIndicator != null)
                _bgmOffIndicator.SetActive(!isOn);
        }

        private void UpdateSFXVisual()
        {
            if (GameOptionManager.Instance == null) return;

            bool isOn = GameOptionManager.Instance.SFXEnabled;

            if (_sfxOnIndicator != null)
                _sfxOnIndicator.SetActive(isOn);

            if (_sfxOffIndicator != null)
                _sfxOffIndicator.SetActive(!isOn);
        }

        private void UpdateHapticVisual()
        {
            if (GameOptionManager.Instance == null) return;

            bool isOn = GameOptionManager.Instance.HapticEnabled;

            if (_hapticOnIndicator != null)
                _hapticOnIndicator.SetActive(isOn);

            if (_hapticOffIndicator != null)
                _hapticOffIndicator.SetActive(!isOn);
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

            gameObject.SetActive(true);
        }

        public void Close()
        {
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
