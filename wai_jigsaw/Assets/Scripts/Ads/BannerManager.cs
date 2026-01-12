using UnityEngine;
using UnityEngine.UI;

namespace WaiJigsaw.Ads
{
    /// <summary>
    /// 배너 광고 영역을 중앙에서 관리합니다.
    /// - 모든 씬에서 동일한 배너 높이 사용
    /// - SafeArea와 연동하여 콘텐츠 영역 조정
    /// - 추후 AppLovin MAX 배너 통합 예정
    /// </summary>
    public class BannerManager : MonoBehaviour
    {
        private static BannerManager _instance;
        public static BannerManager Instance => _instance;

        [Header("Banner Settings")]
        [Tooltip("배너 광고 높이 (픽셀)")]
        [SerializeField] private float _bannerHeight = 150f;

        [Tooltip("배너 표시 여부")]
        [SerializeField] private bool _showBanner = true;

        [Header("Debug")]
        [Tooltip("배너 영역을 시각적으로 표시 (개발용)")]
        [SerializeField] private bool _showPlaceholder = true;
        [SerializeField] private Color _placeholderColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        /// <summary>
        /// 현재 배너 높이 (픽셀)
        /// </summary>
        public float BannerHeight => _showBanner ? _bannerHeight : 0f;

        /// <summary>
        /// 배너 표시 여부
        /// </summary>
        public bool ShowBanner
        {
            get => _showBanner;
            set
            {
                _showBanner = value;
                OnBannerVisibilityChanged?.Invoke(_showBanner);
            }
        }

        /// <summary>
        /// 배너 가시성 변경 이벤트
        /// </summary>
        public event System.Action<bool> OnBannerVisibilityChanged;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 특정 RectTransform의 하단에 배너 영역만큼 여백을 적용합니다.
        /// - Stretch 앵커 (0,0)~(1,1): offsetMin.y 조정
        /// - Center/기타 앵커: anchoredPosition.y 조정
        /// </summary>
        /// <param name="contentArea">콘텐츠 영역 RectTransform</param>
        public void ApplyBannerOffset(RectTransform contentArea)
        {
            if (contentArea == null) return;

            float offset = _showBanner ? _bannerHeight : 0f;

            // Stretch 앵커인지 확인 (anchorMin.y == 0 && anchorMax.y == 1)
            bool isStretchY = Mathf.Approximately(contentArea.anchorMin.y, 0f) &&
                              Mathf.Approximately(contentArea.anchorMax.y, 1f);

            if (isStretchY)
            {
                // Stretch 앵커: 하단 오프셋 조정
                contentArea.offsetMin = new Vector2(contentArea.offsetMin.x, offset);
            }
            else
            {
                // Center/기타 앵커: Y 위치를 배너 높이의 절반만큼 위로 이동
                Vector2 pos = contentArea.anchoredPosition;
                contentArea.anchoredPosition = new Vector2(pos.x, pos.y + offset / 2f);
            }
        }

        /// <summary>
        /// 배너 Placeholder UI를 생성합니다.
        /// </summary>
        /// <param name="parentCanvas">부모 Canvas의 Transform</param>
        /// <returns>생성된 Placeholder GameObject</returns>
        public GameObject CreateBannerPlaceholder(Transform parentCanvas)
        {
            if (!_showPlaceholder || !_showBanner) return null;

            GameObject placeholder = new GameObject("BannerPlaceholder");
            placeholder.transform.SetParent(parentCanvas, false);

            RectTransform rect = placeholder.AddComponent<RectTransform>();
            // 하단에 앵커 고정
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = new Vector2(0f, _bannerHeight);

            // 시각적 표시용 Image
            Image bgImage = placeholder.AddComponent<Image>();
            bgImage.color = _placeholderColor;

            // "BANNER AD" 텍스트 추가 (개발용)
            GameObject textObj = new GameObject("PlaceholderText");
            textObj.transform.SetParent(placeholder.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmpText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmpText.text = "BANNER AD";
            tmpText.alignment = TMPro.TextAlignmentOptions.Center;
            tmpText.fontSize = 24;
            tmpText.color = new Color(1f, 1f, 1f, 0.5f);

            return placeholder;
        }

        /// <summary>
        /// 배너 높이를 설정합니다.
        /// </summary>
        public void SetBannerHeight(float height)
        {
            _bannerHeight = height;
            OnBannerVisibilityChanged?.Invoke(_showBanner);
        }

        #region AppLovin MAX Integration (추후 구현)

        // TODO: AppLovin MAX 배너 광고 로드
        // public void LoadBanner() { }

        // TODO: AppLovin MAX 배너 광고 표시
        // public void ShowBannerAd() { }

        // TODO: AppLovin MAX 배너 광고 숨김
        // public void HideBannerAd() { }

        #endregion
    }
}
