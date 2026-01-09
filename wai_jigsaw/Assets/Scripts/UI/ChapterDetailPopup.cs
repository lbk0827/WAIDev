using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 챕터 상세 팝업 (이미지 확대 뷰)
    /// - 클리어한 챕터 이미지를 크게 보여줌
    /// - 챕터 이름 표시
    /// - Dim 배경 + 닫기 버튼
    /// </summary>
    public class ChapterDetailPopup : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _dimButton;       // Dim 배경 클릭 시 닫기
        [SerializeField] private Image _chapterImage;     // 챕터 이미지
        [SerializeField] private TMP_Text _chapterName;   // 챕터 이름
        [SerializeField] private Image _frameImage;       // 프레임 이미지 (옵션)

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _imageContainer;  // 이미지 컨테이너 (스케일 애니메이션용)
        [SerializeField] private float _animationDuration = 0.2f;

        private bool _isInitialized = false;

        private void Awake()
        {
            RegisterButtonEvents();
            _isInitialized = true;
        }

        private void OnEnable()
        {
            if (!_isInitialized)
            {
                RegisterButtonEvents();
                _isInitialized = true;
            }

            PlayOpenAnimation();
        }

        private void RegisterButtonEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);

            if (_dimButton != null)
                _dimButton.onClick.AddListener(Close);
        }

        #region Public Methods

        /// <summary>
        /// 챕터 상세 팝업을 엽니다.
        /// </summary>
        /// <param name="chapterSprite">표시할 챕터 이미지</param>
        /// <param name="chapterName">챕터 이름</param>
        public void Open(Sprite chapterSprite, string chapterName)
        {
            // 이미지 설정
            if (_chapterImage != null)
            {
                _chapterImage.sprite = chapterSprite;
            }

            // 챕터 이름 설정
            if (_chapterName != null)
            {
                _chapterName.text = chapterName;
            }

            gameObject.SetActive(true);
        }

        /// <summary>
        /// 팝업을 닫습니다.
        /// </summary>
        public void Close()
        {
            PlayCloseAnimation(() =>
            {
                gameObject.SetActive(false);
            });
        }

        #endregion

        #region Animation

        private void PlayOpenAnimation()
        {
            // 페이드 인
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                StartCoroutine(FadeIn());
            }

            // 스케일 인
            if (_imageContainer != null)
            {
                _imageContainer.localScale = Vector3.one * 0.8f;
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
                _imageContainer.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, easedT);
                yield return null;
            }
            _imageContainer.localScale = Vector3.one;
        }

        #endregion
    }
}
