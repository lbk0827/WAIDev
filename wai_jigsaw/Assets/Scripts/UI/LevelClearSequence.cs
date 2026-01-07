using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using WaiJigsaw.Data;
using System;
using System.Collections.Generic;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 레벨 클리어 시퀀스를 관리하는 컴포넌트
    ///
    /// 연출 순서:
    /// 1. 퍼즐 완성 감지 → 드래그 비활성화
    /// 2. 상단 UI 페이드 아웃 (레벨 텍스트, 설정 버튼)
    /// 3. 코인 표시 UI 페이드 인 (이미 보이면 유지)
    /// 4. BoardContainer 위로 이동
    /// 5. 축하 연출 재생 (파티클 - 추후)
    /// 6. 클리어 타이틀 + 획득 코인 + NEXT 버튼 등장
    /// 7. NEXT 클릭 → 코인 날아가기 → 로비 이동
    /// </summary>
    public class LevelClearSequence : MonoBehaviour
    {
        [Header("====== Board ======")]
        [Tooltip("퍼즐 보드 컨테이너 (위로 이동할 대상)")]
        [SerializeField] private Transform _boardContainer;
        [Tooltip("퍼즐 보드 셋업 (그룹 테두리 접근용)")]
        [SerializeField] private PuzzleBoardSetup _puzzleBoardSetup;
        [Tooltip("보드 이동 거리 (Y축)")]
        [SerializeField] private float _boardMoveDistance = 1.5f;
        [Tooltip("보드 이동 시간 (초)")]
        [SerializeField] private float _boardMoveDuration = 0.6f;

        [Header("====== Puzzle Panel UI (페이드 아웃 대상) ======")]
        [Tooltip("현재 레벨 텍스트")]
        [SerializeField] private TMP_Text _currentLevelText;
        [Tooltip("설정 버튼")]
        [SerializeField] private Button _settingsButton;
        [Tooltip("인게임 버튼 컨테이너 (있으면)")]
        [SerializeField] private GameObject _inGameButtonsContainer;

        [Header("====== Coin Display (페이드 인 대상) ======")]
        [Tooltip("코인 표시 UI (CoinDisplay가 붙은 오브젝트)")]
        [SerializeField] private GameObject _coinDisplayObject;

        [Header("====== Clear Result UI ======")]
        [Tooltip("클리어 타이틀 텍스트")]
        [SerializeField] private TMP_Text _clearTitleText;
        [Tooltip("획득 코인 컨테이너")]
        [SerializeField] private GameObject _rewardContainer;
        [Tooltip("획득 코인 아이콘")]
        [SerializeField] private Image _rewardCoinIcon;
        [Tooltip("획득 코인량 텍스트")]
        [SerializeField] private TMP_Text _rewardAmountText;
        [Tooltip("NEXT 버튼")]
        [SerializeField] private Button _nextButton;

        [Header("====== Coin Fly Animation ======")]
        [Tooltip("코인 프리팹 (날아가는 코인용) - 없으면 자동 생성")]
        [SerializeField] private GameObject _coinPrefab;
        [Tooltip("코인이 날아갈 목적지 (보유 코인 UI) - 없으면 CoinDisplay 자동 검색")]
        [SerializeField] private Transform _coinDestination;
        [Tooltip("날아가는 코인 개수")]
        [SerializeField] private int _flyingCoinCount = 8;
        [Tooltip("코인 날아가는 시간 (초)")]
        [SerializeField] private float _coinFlyDuration = 0.5f;
        [Tooltip("코인 생성 간격 (초)")]
        [SerializeField] private float _coinSpawnInterval = 0.05f;
        [Tooltip("코인 크기 (UI Canvas 기준)")]
        [SerializeField] private float _coinSize = 100f;
        [Tooltip("시작 위치 분산 범위 (픽셀)")]
        [SerializeField] private float _startSpreadRange = 30f;

        [Header("====== Celebration Effect ======")]
        [Tooltip("축하 연출 컨트롤러 (파티클 시스템)")]
        [SerializeField] private CelebrationController _celebrationController;

        [Header("====== Animation Settings ======")]
        [Tooltip("UI 페이드 시간 (초)")]
        [SerializeField] private float _uiFadeDuration = 0.3f;
        [Tooltip("UI 등장 시간 (초)")]
        [SerializeField] private float _uiAppearDuration = 0.4f;
        [Tooltip("각 단계 사이 대기 시간 (초)")]
        [SerializeField] private float _stepDelay = 0.2f;

        // 시퀀스 상태
        private bool _isPlaying = false;
        private int _rewardAmount = 0;
        private int _clearedLevel = 0;
        private Sequence _mainSequence;

        // 원래 위치 저장 (복원용)
        private Vector3 _boardOriginalPosition;

        // 콜백
        private Action _onSequenceComplete;

        #region Public Properties

        /// <summary>
        /// 시퀀스가 재생 중인지 여부
        /// </summary>
        public bool IsPlaying => _isPlaying;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // 결과 UI 초기 상태: 숨김
            HideResultUI();
        }

        private void OnDestroy()
        {
            // 시퀀스 정리
            _mainSequence?.Kill();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 레벨 클리어 시퀀스를 시작합니다.
        /// </summary>
        /// <param name="clearedLevel">클리어한 레벨 번호</param>
        /// <param name="onComplete">시퀀스 완료 후 콜백</param>
        public void PlayClearSequence(int clearedLevel, Action onComplete = null)
        {
            if (_isPlaying)
            {
                Debug.LogWarning("[LevelClearSequence] 이미 시퀀스가 재생 중입니다.");
                return;
            }

            _isPlaying = true;
            _clearedLevel = clearedLevel;
            _onSequenceComplete = onComplete;

            // LevelTable에서 보상량 가져오기
            LevelTableRecord levelRecord = LevelTable.Get(clearedLevel);
            _rewardAmount = levelRecord?.reward ?? 0;

            // 보드 원래 위치 저장
            if (_boardContainer != null)
            {
                _boardOriginalPosition = _boardContainer.position;
            }

            // DOTween 시퀀스 생성 및 실행
            CreateAndPlaySequence();
        }

        /// <summary>
        /// NEXT 버튼 클릭 시 호출 - 코인 획득 연출 후 로비로 이동
        /// </summary>
        public void OnNextButtonClicked()
        {
            if (_nextButton != null)
            {
                _nextButton.interactable = false;
            }

            // 축하 연출 중지
            StopCelebrationEffect();

            // 코인 획득 연출 재생
            PlayCoinFlyAnimation(() =>
            {
                // 코인 지급
                if (_rewardAmount > 0)
                {
                    GameDataContainer.Instance.AddCoin(_rewardAmount);
                    GameDataContainer.Instance.Save();
                }

                // 시퀀스 완료
                _isPlaying = false;
                _onSequenceComplete?.Invoke();
            });
        }

        /// <summary>
        /// 시퀀스를 리셋합니다 (다음 레벨을 위해)
        /// </summary>
        public void ResetSequence()
        {
            _mainSequence?.Kill();
            _isPlaying = false;

            // 축하 연출 중지
            StopCelebrationEffect();

            // 보드 위치 복원
            if (_boardContainer != null && _boardOriginalPosition != Vector3.zero)
            {
                _boardContainer.position = _boardOriginalPosition;
            }

            // UI 상태 복원
            ResetUIStates();
            HideResultUI();
        }

        #endregion

        #region Sequence Creation

        /// <summary>
        /// DOTween 시퀀스를 생성하고 실행합니다.
        /// </summary>
        private void CreateAndPlaySequence()
        {
            _mainSequence?.Kill();
            _mainSequence = DOTween.Sequence();

            // ====== Step 1: 상단 UI 페이드 아웃 ======
            // 레벨 텍스트 페이드 아웃
            if (_currentLevelText != null)
            {
                _mainSequence.Join(_currentLevelText.DOFade(0f, _uiFadeDuration));
            }

            // 설정 버튼 페이드 아웃
            if (_settingsButton != null)
            {
                Image btnImage = _settingsButton.GetComponent<Image>();
                if (btnImage != null)
                {
                    _mainSequence.Join(btnImage.DOFade(0f, _uiFadeDuration));
                }
                // 자식 텍스트/이미지도 페이드
                foreach (var graphic in _settingsButton.GetComponentsInChildren<Graphic>())
                {
                    _mainSequence.Join(graphic.DOFade(0f, _uiFadeDuration));
                }
            }

            // 인게임 버튼 컨테이너 페이드 아웃
            if (_inGameButtonsContainer != null)
            {
                CanvasGroup cg = _inGameButtonsContainer.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = _inGameButtonsContainer.AddComponent<CanvasGroup>();
                }
                _mainSequence.Join(cg.DOFade(0f, _uiFadeDuration));
            }

            // ====== Step 2: 코인 표시 UI 페이드 인 ======
            _mainSequence.AppendInterval(_stepDelay);

            if (_coinDisplayObject != null)
            {
                CanvasGroup coinCG = _coinDisplayObject.GetComponent<CanvasGroup>();
                if (coinCG == null)
                {
                    coinCG = _coinDisplayObject.AddComponent<CanvasGroup>();
                }
                _coinDisplayObject.SetActive(true);
                coinCG.alpha = 0f;
                _mainSequence.Append(coinCG.DOFade(1f, _uiFadeDuration));
            }

            // ====== Step 3: 보드 위로 이동 ======
            _mainSequence.AppendInterval(_stepDelay);

            // 퍼즐 조각들은 월드 좌표를 직접 설정하므로 BoardContainer 이동이 전파되지 않음
            // 따라서 퍼즐 조각과 테두리를 직접 이동해야 함
            if (_puzzleBoardSetup != null)
            {
                PuzzleBoardSetup boardSetupRef = _puzzleBoardSetup;

                // 이동 시작 전 테두리 재계산
                _mainSequence.AppendCallback(() =>
                {
                    boardSetupRef.RecalculateCompletedGroupBorder();
                });

                // DOTween을 사용하여 부드럽게 이동
                float elapsedTime = 0f;
                float totalMoved = 0f;

                _mainSequence.Append(
                    DOTween.To(
                        () => elapsedTime,
                        x =>
                        {
                            float newElapsed = x;
                            float deltaTime = newElapsed - elapsedTime;
                            elapsedTime = newElapsed;

                            // 이번 프레임에 이동할 거리 계산 (Ease 적용)
                            float targetProgress = newElapsed / _boardMoveDuration;
                            float easedProgress = DOVirtual.EasedValue(0f, 1f, targetProgress, Ease.OutCubic);
                            float targetMoved = _boardMoveDistance * easedProgress;
                            float frameDelta = targetMoved - totalMoved;
                            totalMoved = targetMoved;

                            // 퍼즐 조각과 테두리를 함께 이동
                            if (frameDelta > 0.0001f)
                            {
                                boardSetupRef.MoveAllPiecesAndBorder(new Vector3(0, frameDelta, 0));
                            }
                        },
                        _boardMoveDuration,
                        _boardMoveDuration
                    ).SetEase(Ease.Linear) // 실제 Easing은 위에서 수동으로 적용
                );
            }
            else
            {
                Debug.LogWarning("[LevelClearSequence] _puzzleBoardSetup이 할당되지 않았습니다! Inspector에서 설정해주세요.");
            }

            // ====== Step 4: 축하 연출 (파티클) ======
            _mainSequence.AppendCallback(() => PlayCelebrationEffect());

            // ====== Step 5: 클리어 UI 등장 ======
            _mainSequence.AppendInterval(_stepDelay);
            _mainSequence.AppendCallback(() => SetupResultUI());

            // 클리어 타이틀 등장 (스케일 애니메이션)
            if (_clearTitleText != null)
            {
                _clearTitleText.gameObject.SetActive(true);
                _clearTitleText.transform.localScale = Vector3.zero;
                _mainSequence.Append(_clearTitleText.transform.DOScale(1f, _uiAppearDuration).SetEase(Ease.OutBack));
            }

            // 보상 컨테이너 등장
            if (_rewardContainer != null && _rewardAmount > 0)
            {
                _rewardContainer.SetActive(true);
                _rewardContainer.transform.localScale = Vector3.zero;
                _mainSequence.Append(_rewardContainer.transform.DOScale(1f, _uiAppearDuration).SetEase(Ease.OutBack));
            }

            // NEXT 버튼 등장
            if (_nextButton != null)
            {
                _nextButton.gameObject.SetActive(true);
                _nextButton.transform.localScale = Vector3.zero;
                _nextButton.interactable = true;
                _mainSequence.Append(_nextButton.transform.DOScale(1f, _uiAppearDuration).SetEase(Ease.OutBack));
            }
        }

        #endregion

        #region Result UI

        /// <summary>
        /// 결과 UI를 설정합니다.
        /// </summary>
        private void SetupResultUI()
        {
            // 클리어 타이틀 설정
            if (_clearTitleText != null)
            {
                _clearTitleText.text = $"LEVEL {_clearedLevel}\nCLEAR!";
            }

            // 보상량 설정
            if (_rewardAmountText != null)
            {
                _rewardAmountText.text = $"+{_rewardAmount}";
            }

            // 코인 아이콘 설정
            if (_rewardCoinIcon != null)
            {
                Sprite coinSprite = ItemTable.GetCoinIcon();
                if (coinSprite != null)
                {
                    _rewardCoinIcon.sprite = coinSprite;
                }
            }
        }

        /// <summary>
        /// 결과 UI를 숨깁니다.
        /// </summary>
        private void HideResultUI()
        {
            if (_clearTitleText != null)
                _clearTitleText.gameObject.SetActive(false);

            if (_rewardContainer != null)
                _rewardContainer.SetActive(false);

            if (_nextButton != null)
                _nextButton.gameObject.SetActive(false);

            // CoinDisplay도 숨김 (클리어 시퀀스에서만 등장)
            if (_coinDisplayObject != null)
                _coinDisplayObject.SetActive(false);
        }

        /// <summary>
        /// UI 상태를 초기화합니다.
        /// </summary>
        private void ResetUIStates()
        {
            // 상단 UI 복원
            if (_currentLevelText != null)
            {
                _currentLevelText.alpha = 1f;
            }

            if (_settingsButton != null)
            {
                Image btnImage = _settingsButton.GetComponent<Image>();
                if (btnImage != null) btnImage.color = new Color(btnImage.color.r, btnImage.color.g, btnImage.color.b, 1f);

                foreach (var graphic in _settingsButton.GetComponentsInChildren<Graphic>())
                {
                    graphic.color = new Color(graphic.color.r, graphic.color.g, graphic.color.b, 1f);
                }
            }

            if (_inGameButtonsContainer != null)
            {
                CanvasGroup cg = _inGameButtonsContainer.GetComponent<CanvasGroup>();
                if (cg != null) cg.alpha = 1f;
            }
        }

        #endregion

        #region Coin Fly Animation

        // 코인 스프라이트 캐시 (런타임 생성용)
        private Sprite _cachedCoinSprite;
        private Canvas _parentCanvas;

        /// <summary>
        /// 코인 날아가기 애니메이션을 재생합니다.
        /// </summary>
        private void PlayCoinFlyAnimation(Action onComplete)
        {
            if (_rewardAmount <= 0)
            {
                onComplete?.Invoke();
                return;
            }

            // 목적지 자동 검색 (설정되지 않은 경우)
            if (_coinDestination == null)
            {
                CoinDisplay coinDisplay = FindObjectOfType<CoinDisplay>();
                if (coinDisplay != null)
                {
                    _coinDestination = coinDisplay.transform;
                }
            }

            if (_coinDestination == null)
            {
                Debug.LogWarning("[LevelClearSequence] 코인 목적지가 없어서 스킵");
                onComplete?.Invoke();
                return;
            }

            // rewardContainer가 속한 Canvas 찾기 (좌표 계산과 코인 생성에 동일한 Canvas 사용)
            if (_rewardContainer != null)
            {
                _parentCanvas = _rewardContainer.GetComponentInParent<Canvas>();
            }
            if (_parentCanvas == null)
            {
                _parentCanvas = GetComponentInParent<Canvas>();
            }
            if (_parentCanvas == null)
            {
                _parentCanvas = FindObjectOfType<Canvas>();
            }

            // 코인 스프라이트 로드
            if (_cachedCoinSprite == null)
            {
                _cachedCoinSprite = ItemTable.GetCoinIcon();
            }

            if (_cachedCoinSprite == null)
            {
                Debug.LogWarning("[LevelClearSequence] 코인 아이콘을 찾을 수 없어서 스킵");
                onComplete?.Invoke();
                return;
            }

            // RectTransform 좌표 계산을 위한 Canvas 참조 (_parentCanvas 직접 사용)
            RectTransform canvasRect = _parentCanvas?.GetComponent<RectTransform>();

            // Canvas 카메라 (Screen Space - Camera 또는 World Space인 경우 필요)
            Camera canvasCamera = null;
            if (_parentCanvas != null)
            {
                if (_parentCanvas.renderMode == RenderMode.ScreenSpaceCamera ||
                    _parentCanvas.renderMode == RenderMode.WorldSpace)
                {
                    canvasCamera = _parentCanvas.worldCamera;
                }
            }

            // 시작 위치 계산 (rewardContainer의 RectTransform에서 직접 가져오기)
            Vector2 startAnchoredPos = Vector2.zero;
            if (_rewardContainer != null)
            {
                RectTransform rewardRect = _rewardContainer.GetComponent<RectTransform>();
                if (rewardRect != null && canvasRect != null)
                {
                    // rewardContainer의 월드 위치를 스크린 좌표로, 그리고 Canvas 로컬 좌표로 변환
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, rewardRect.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPoint,
                        canvasCamera,
                        out startAnchoredPos
                    );
                }
            }

            // 목적지 위치 계산 (CoinDisplay의 RectTransform에서 직접 가져오기)
            Vector2 endAnchoredPos = Vector2.zero;
            if (_coinDestination != null && canvasRect != null)
            {
                RectTransform destRect = _coinDestination.GetComponent<RectTransform>();
                if (destRect != null)
                {
                    Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(canvasCamera, destRect.position);
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        canvasRect,
                        screenPoint,
                        canvasCamera,
                        out endAnchoredPos
                    );
                }
            }

            // 코인 생성 및 애니메이션
            int completedCount = 0;
            int totalCoins = _flyingCoinCount;

            // 좌표 캡처 (클로저에서 사용)
            Vector2 capturedStart = startAnchoredPos;
            Vector2 capturedEnd = endAnchoredPos;

            // 각 코인을 순차적으로 생성하고 애니메이션
            for (int i = 0; i < totalCoins; i++)
            {
                // 코인 생성 딜레이 적용
                float spawnDelay = i * _coinSpawnInterval;

                // 캡처된 변수 문제 방지를 위해 로컬 복사
                int coinIndex = i;

                DOVirtual.DelayedCall(spawnDelay, () =>
                {
                    // 랜덤 오프셋으로 시작 위치 분산
                    Vector2 randomOffset = new Vector2(
                        UnityEngine.Random.Range(-_startSpreadRange, _startSpreadRange),
                        UnityEngine.Random.Range(-_startSpreadRange, _startSpreadRange)
                    );

                    Vector2 coinStartPos = capturedStart + randomOffset;

                    // 코인 GameObject 생성 (프리팹 또는 동적 생성)
                    GameObject coin = CreateCoinObject(coinStartPos);

                    if (coin == null)
                    {
                        completedCount++;
                        if (completedCount >= totalCoins)
                        {
                            onComplete?.Invoke();
                        }
                        return;
                    }

                    RectTransform coinRect = coin.GetComponent<RectTransform>();

                    if (coinRect != null)
                    {
                        // RectTransform용 이동 애니메이션 (DOAnchorPos)
                        coinRect.DOAnchorPos(capturedEnd, _coinFlyDuration)
                            .SetEase(Ease.InQuad);
                    }

                    // 스케일 축소 애니메이션
                    coin.transform.DOScale(0.5f, _coinFlyDuration)
                        .SetEase(Ease.InQuad)
                        .OnComplete(() =>
                        {
                            Destroy(coin);
                            completedCount++;

                            // 모든 코인 완료 체크
                            if (completedCount >= totalCoins)
                            {
                                onComplete?.Invoke();
                            }
                        });
                });
            }
        }

        /// <summary>
        /// 코인 GameObject를 생성합니다. 프리팹이 있으면 사용하고, 없으면 동적으로 생성합니다.
        /// </summary>
        /// <param name="anchoredPosition">Canvas 로컬 좌표 (anchoredPosition)</param>
        private GameObject CreateCoinObject(Vector2 anchoredPosition)
        {
            // 프리팹이 있으면 사용 (프리팹 사용시에는 월드좌표 변환 필요)
            if (_coinPrefab != null)
            {
                GameObject prefabCoin = Instantiate(_coinPrefab, transform.parent);
                RectTransform prefabRect = prefabCoin.GetComponent<RectTransform>();
                if (prefabRect != null)
                {
                    prefabRect.anchoredPosition = anchoredPosition;
                }
                return prefabCoin;
            }

            // 동적으로 코인 UI 생성
            GameObject coinObj = new GameObject("FlyingCoin");

            // _parentCanvas에 직접 배치 (좌표 계산에 사용한 동일한 Canvas)
            if (_parentCanvas != null)
            {
                coinObj.transform.SetParent(_parentCanvas.transform, false);
            }
            else
            {
                coinObj.transform.SetParent(transform.parent, false);
            }

            // RectTransform 설정 (UI 요소로 동작하도록)
            RectTransform rectTransform = coinObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(_coinSize, _coinSize);

            // 앵커를 중앙으로 설정 (RectTransformUtility가 중앙 기준으로 계산)
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);

            // anchoredPosition으로 위치 설정 (Canvas 로컬 좌표)
            rectTransform.anchoredPosition = anchoredPosition;

            // Image 컴포넌트 추가
            Image coinImage = coinObj.AddComponent<Image>();
            coinImage.sprite = _cachedCoinSprite;
            coinImage.preserveAspect = true;
            coinImage.raycastTarget = false;

            // Canvas 오버라이드로 렌더링 순서 최상위 보장
            Canvas overrideCanvas = coinObj.AddComponent<Canvas>();
            overrideCanvas.overrideSorting = true;
            overrideCanvas.sortingOrder = 1000;
            coinObj.AddComponent<GraphicRaycaster>();

            return coinObj;
        }

        #endregion

        #region Celebration Effect

        /// <summary>
        /// 축하 연출을 재생합니다. (파티클 시스템)
        /// </summary>
        private void PlayCelebrationEffect()
        {
            if (_celebrationController != null)
            {
                _celebrationController.Play();
            }
        }

        /// <summary>
        /// 축하 연출을 중지합니다.
        /// </summary>
        private void StopCelebrationEffect()
        {
            if (_celebrationController != null)
            {
                _celebrationController.Stop();
            }
        }

        #endregion
    }
}
