using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using WaiJigsaw.Data;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 컬렉션 팝업 UI
    /// - 클리어한 챕터 이미지들을 보여주는 갤러리
    /// - 2열 그리드 형태로 챕터 카드 배치
    /// - 클리어된 챕터: 이미지 + 이름 + 레벨 구간
    /// - 미클리어 챕터: 자물쇠 아이콘
    /// </summary>
    public class CollectionPopup : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _titleText;

        [Header("Scroll View")]
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private Transform _gridContainer;  // Grid Layout Group이 붙은 Content
        [SerializeField] private GameObject _chapterCardPrefab;  // 챕터 카드 프리팹

        [Header("Banner Ad Area")]
        [SerializeField] private RectTransform _bannerAdArea;  // 배너 광고 영역 (높이 약 100px)

        [Header("Animation")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _animationDuration = 0.2f;

        [Header("References")]
        [SerializeField] private LevelGroupManager _levelGroupManager;
        [SerializeField] private ChapterDetailPopup _chapterDetailPopup;

        [Header("Lock Icon")]
        [SerializeField] private Sprite _lockIconSprite;  // 자물쇠 아이콘

        [Header("Card Layout Settings")]
        [Tooltip("챕터 카드 크기")]
        [SerializeField] private Vector2 _cardSize = new Vector2(200, 300);

        [Header("Chapter Image Settings")]
        [Tooltip("챕터 이미지 영역 (앵커 Min)")]
        [SerializeField] private Vector2 _chapterImageAnchorMin = new Vector2(0.05f, 0.15f);
        [Tooltip("챕터 이미지 영역 (앵커 Max)")]
        [SerializeField] private Vector2 _chapterImageAnchorMax = new Vector2(0.95f, 0.95f);

        [Header("Lock Icon Settings")]
        [Tooltip("자물쇠 아이콘 영역 (앵커 Min)")]
        [SerializeField] private Vector2 _lockIconAnchorMin = new Vector2(0.3f, 0.4f);
        [Tooltip("자물쇠 아이콘 영역 (앵커 Max)")]
        [SerializeField] private Vector2 _lockIconAnchorMax = new Vector2(0.7f, 0.7f);

        [Header("Chapter Name Settings")]
        [Tooltip("챕터 이름 영역 (앵커 Min)")]
        [SerializeField] private Vector2 _chapterNameAnchorMin = new Vector2(0f, 0.85f);
        [Tooltip("챕터 이름 영역 (앵커 Max)")]
        [SerializeField] private Vector2 _chapterNameAnchorMax = new Vector2(1f, 1f);
        [Tooltip("챕터 이름 좌우 패딩")]
        [SerializeField] private float _chapterNamePadding = 10f;
        [Tooltip("챕터 이름 상하 마진")]
        [SerializeField] private float _chapterNameMargin = 10f;
        [Tooltip("챕터 이름 Auto Size 최소 폰트")]
        [SerializeField] private float _chapterNameFontSizeMin = 10f;
        [Tooltip("챕터 이름 Auto Size 최대 폰트")]
        [SerializeField] private float _chapterNameFontSizeMax = 24f;

        [Header("Level Range Settings")]
        [Tooltip("레벨 구간 영역 (앵커 Min)")]
        [SerializeField] private Vector2 _levelRangeAnchorMin = new Vector2(0.1f, 0f);
        [Tooltip("레벨 구간 영역 (앵커 Max)")]
        [SerializeField] private Vector2 _levelRangeAnchorMax = new Vector2(0.9f, 0.12f);
        [Tooltip("레벨 구간 배경 색상")]
        [SerializeField] private Color _levelRangeBgColor = new Color(0, 0, 0, 0.5f);
        [Tooltip("레벨 구간 상하 마진")]
        [SerializeField] private float _levelRangeMargin = 10f;
        [Tooltip("레벨 구간 Auto Size 최소 폰트")]
        [SerializeField] private float _levelRangeFontSizeMin = 10f;
        [Tooltip("레벨 구간 Auto Size 최대 폰트")]
        [SerializeField] private float _levelRangeFontSizeMax = 20f;

        [Header("Card Background")]
        [Tooltip("카드 배경 색상")]
        [SerializeField] private Color _cardBackgroundColor = new Color(0.2f, 0.3f, 0.25f, 1f);

        // 생성된 챕터 카드들
        private List<CollectionChapterCard> _chapterCards = new List<CollectionChapterCard>();
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

            RefreshChapterCards();
            PlayOpenAnimation();
        }

        private void RegisterButtonEvents()
        {
            if (_closeButton != null)
                _closeButton.onClick.AddListener(Close);
        }

        #region Chapter Cards

        /// <summary>
        /// 챕터 카드들을 갱신합니다.
        /// </summary>
        private void RefreshChapterCards()
        {
            ClearChapterCards();

            // Inspector에서 할당되지 않았으면 싱글턴에서 가져오기
            if (_levelGroupManager == null)
            {
                _levelGroupManager = LevelGroupManager.Instance;
            }

            if (_levelGroupManager == null)
            {
                Debug.LogWarning("[CollectionPopup] LevelGroupManager를 찾을 수 없습니다.");
                return;
            }

            // 모든 레벨 그룹 가져오기
            var allGroups = LevelGroupTable.GetAll();

            foreach (var group in allGroups)
            {
                CreateChapterCard(group);
            }
        }

        /// <summary>
        /// 챕터 카드를 생성합니다.
        /// </summary>
        private void CreateChapterCard(LevelGroupTableRecord group)
        {
            if (_gridContainer == null) return;

            GameObject cardObj;

            if (_chapterCardPrefab != null)
            {
                cardObj = Instantiate(_chapterCardPrefab, _gridContainer);
            }
            else
            {
                cardObj = CreateDefaultChapterCard();
                cardObj.transform.SetParent(_gridContainer, false);
            }

            cardObj.name = $"ChapterCard_Group{group.GroupID}";

            // CollectionChapterCard 컴포넌트 가져오기 또는 추가
            CollectionChapterCard chapterCard = cardObj.GetComponent<CollectionChapterCard>();
            if (chapterCard == null)
            {
                chapterCard = cardObj.AddComponent<CollectionChapterCard>();
            }

            // 챕터 클리어 여부 확인 (마지막 레벨까지 클리어했는지)
            bool isCleared = IsChapterCleared(group);

            // 챕터 이미지 로드
            Sprite chapterSprite = null;
            if (isCleared)
            {
                chapterSprite = _levelGroupManager.LoadGroupImage(group);
            }

            // 챕터 카드 초기화
            chapterCard.Initialize(
                group,
                isCleared,
                chapterSprite,
                _lockIconSprite,
                OnChapterCardClicked
            );

            _chapterCards.Add(chapterCard);
        }

        /// <summary>
        /// 기본 챕터 카드를 생성합니다 (프리팹이 없을 때).
        /// </summary>
        private GameObject CreateDefaultChapterCard()
        {
            GameObject cardObj = new GameObject("ChapterCard");

            // RectTransform
            RectTransform rectTransform = cardObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = _cardSize;

            // Image (배경)
            Image bgImage = cardObj.AddComponent<Image>();
            bgImage.color = _cardBackgroundColor;

            // Button
            Button button = cardObj.AddComponent<Button>();

            // === 챕터 이미지 ===
            GameObject imageObj = new GameObject("ChapterImage");
            imageObj.transform.SetParent(cardObj.transform, false);
            RectTransform imageRect = imageObj.AddComponent<RectTransform>();
            imageRect.anchorMin = _chapterImageAnchorMin;
            imageRect.anchorMax = _chapterImageAnchorMax;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;

            Image chapterImage = imageObj.AddComponent<Image>();
            chapterImage.preserveAspect = true;

            // === 자물쇠 아이콘 ===
            GameObject lockObj = new GameObject("LockIcon");
            lockObj.transform.SetParent(cardObj.transform, false);
            RectTransform lockRect = lockObj.AddComponent<RectTransform>();
            lockRect.anchorMin = _lockIconAnchorMin;
            lockRect.anchorMax = _lockIconAnchorMax;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;

            Image lockImage = lockObj.AddComponent<Image>();
            lockImage.color = Color.white;
            lockImage.preserveAspect = true;

            // === 챕터 이름 ===
            GameObject nameObj = new GameObject("ChapterName");
            nameObj.transform.SetParent(cardObj.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = _chapterNameAnchorMin;
            nameRect.anchorMax = _chapterNameAnchorMax;
            nameRect.offsetMin = new Vector2(_chapterNamePadding, _chapterNameMargin);
            nameRect.offsetMax = new Vector2(-_chapterNamePadding, -_chapterNameMargin);

            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableAutoSizing = true;
            nameText.fontSizeMin = _chapterNameFontSizeMin;
            nameText.fontSizeMax = _chapterNameFontSizeMax;
            nameText.color = Color.white;

            // === 레벨 구간 ===
            GameObject levelRangeObj = new GameObject("LevelRange");
            levelRangeObj.transform.SetParent(cardObj.transform, false);
            RectTransform levelRect = levelRangeObj.AddComponent<RectTransform>();
            levelRect.anchorMin = _levelRangeAnchorMin;
            levelRect.anchorMax = _levelRangeAnchorMax;
            levelRect.offsetMin = Vector2.zero;
            levelRect.offsetMax = Vector2.zero;

            // 레벨 구간 배경
            Image levelBgImage = levelRangeObj.AddComponent<Image>();
            levelBgImage.color = _levelRangeBgColor;

            // 레벨 구간 텍스트
            GameObject levelTextObj = new GameObject("LevelText");
            levelTextObj.transform.SetParent(levelRangeObj.transform, false);
            RectTransform levelTextRect = levelTextObj.AddComponent<RectTransform>();
            levelTextRect.anchorMin = Vector2.zero;
            levelTextRect.anchorMax = Vector2.one;
            levelTextRect.offsetMin = new Vector2(0, _levelRangeMargin);
            levelTextRect.offsetMax = new Vector2(0, -_levelRangeMargin);

            TextMeshProUGUI levelText = levelTextObj.AddComponent<TextMeshProUGUI>();
            levelText.alignment = TextAlignmentOptions.Center;
            levelText.enableAutoSizing = true;
            levelText.fontSizeMin = _levelRangeFontSizeMin;
            levelText.fontSizeMax = _levelRangeFontSizeMax;
            levelText.color = Color.white;

            // CollectionChapterCard에 참조 연결
            CollectionChapterCard card = cardObj.AddComponent<CollectionChapterCard>();
            card.SetReferences(chapterImage, lockImage, nameText, levelText, button);

            return cardObj;
        }

        /// <summary>
        /// 챕터 클리어 여부를 확인합니다.
        /// </summary>
        private bool IsChapterCleared(LevelGroupTableRecord group)
        {
            // 챕터의 마지막 레벨까지 클리어했는지 확인
            int currentLevel = GameDataContainer.Instance.CurrentLevel;
            return currentLevel > group.EndLevel;
        }

        /// <summary>
        /// 챕터 카드 클릭 시 호출됩니다.
        /// </summary>
        private void OnChapterCardClicked(LevelGroupTableRecord group, bool isCleared)
        {
            if (!isCleared)
            {
                // 미클리어 챕터는 클릭 불가
                Debug.Log($"[CollectionPopup] 챕터 {group.GroupID}는 아직 클리어되지 않았습니다.");
                return;
            }

            // ChapterDetailPopup 열기
            if (_chapterDetailPopup != null)
            {
                Sprite chapterSprite = _levelGroupManager.LoadGroupImage(group);
                string chapterName = group.GroupName ?? $"Chapter {group.GroupID}";
                _chapterDetailPopup.Open(chapterSprite, chapterName);
            }
            else
            {
                Debug.LogWarning("[CollectionPopup] ChapterDetailPopup이 할당되지 않았습니다.");
            }
        }

        /// <summary>
        /// 챕터 카드들을 정리합니다.
        /// </summary>
        private void ClearChapterCards()
        {
            foreach (var card in _chapterCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            _chapterCards.Clear();
        }

        #endregion

        #region Open/Close

        public void Open()
        {
            // 팝업 열림 상태 설정
            if (GameManager.Instance != null)
            {
                GameManager.Instance.IsPopupOpen = true;
            }

            gameObject.SetActive(true);
        }

        public void Close()
        {
            // 팝업 닫힘 상태 설정
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

        #endregion
    }
}
