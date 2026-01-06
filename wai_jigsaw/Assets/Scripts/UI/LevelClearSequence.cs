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
        [Tooltip("보드 이동 거리 (Y축, 월드 유닛)")]
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
        [Tooltip("코인 프리팹 (날아가는 코인용)")]
        [SerializeField] private GameObject _coinPrefab;
        [Tooltip("코인이 날아갈 목적지 (보유 코인 UI)")]
        [SerializeField] private Transform _coinDestination;
        [Tooltip("날아가는 코인 개수")]
        [SerializeField] private int _flyingCoinCount = 8;
        [Tooltip("코인 날아가는 시간 (초)")]
        [SerializeField] private float _coinFlyDuration = 0.5f;
        [Tooltip("코인 생성 간격 (초)")]
        [SerializeField] private float _coinSpawnInterval = 0.05f;

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

            Debug.Log($"[LevelClearSequence] 레벨 {clearedLevel} 클리어 시퀀스 시작 (보상: {_rewardAmount} 코인)");

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
            _mainSequence.AppendCallback(() => Debug.Log("[LevelClearSequence] Step 1: 상단 UI 페이드 아웃"));

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
            _mainSequence.AppendCallback(() => Debug.Log("[LevelClearSequence] Step 2: 코인 UI 페이드 인"));

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
            _mainSequence.AppendCallback(() => Debug.Log("[LevelClearSequence] Step 3: 보드 위로 이동"));

            if (_boardContainer != null)
            {
                Vector3 targetPos = _boardOriginalPosition + new Vector3(0, _boardMoveDistance, 0);

                // _puzzleBoardSetup 참조 확인
                if (_puzzleBoardSetup == null)
                {
                    Debug.LogWarning("[LevelClearSequence] _puzzleBoardSetup이 할당되지 않았습니다! Inspector에서 설정해주세요.");
                }

                // 보드 이동 + 그룹 테두리 동기화 (LineRenderer는 useWorldSpace=true라서 직접 점 이동 필요)
                // 퍼즐 조각의 실제 월드 위치 변화를 추적하여 GroupBorder 이동
                PuzzleBoardSetup boardSetupRef = _puzzleBoardSetup;
                Transform boardRef = _boardContainer;
                Vector3 totalDelta = Vector3.zero;

                // 첫 번째 퍼즐 조각의 월드 위치를 기준으로 이동량 계산
                Transform firstPieceTransform = boardSetupRef != null ? boardSetupRef.GetFirstPieceTransform() : null;
                Vector3 lastPieceWorldPos = firstPieceTransform != null ? firstPieceTransform.position : Vector3.zero;

                Debug.Log($"[LevelClearSequence] 보드 이동 시작: 원래위치={_boardOriginalPosition}, 목표위치={targetPos}, 첫조각위치={lastPieceWorldPos}");

                _mainSequence.Append(
                    _boardContainer.DOMove(targetPos, _boardMoveDuration)
                        .SetEase(Ease.OutCubic)
                        .OnUpdate(() =>
                        {
                            if (boardRef == null || firstPieceTransform == null) return;

                            // 퍼즐 조각의 실제 월드 위치 변화량 계산
                            Vector3 currentPieceWorldPos = firstPieceTransform.position;
                            Vector3 worldDelta = currentPieceWorldPos - lastPieceWorldPos;
                            lastPieceWorldPos = currentPieceWorldPos;
                            totalDelta += worldDelta;

                            // 실제 월드 이동량을 GroupBorder에 적용
                            if (boardSetupRef != null && worldDelta.sqrMagnitude > 0.00001f)
                            {
                                boardSetupRef.MoveCompletedGroupBorder(worldDelta);
                            }
                        })
                        .OnComplete(() =>
                        {
                            Debug.Log($"[LevelClearSequence] 보드 이동 완료: 총 월드 이동량=({totalDelta.x:F3}, {totalDelta.y:F3}, {totalDelta.z:F3})");
                        })
                );
            }

            // ====== Step 4: 축하 연출 (TODO: 파티클) ======
            _mainSequence.AppendCallback(() => Debug.Log("[LevelClearSequence] Step 4: 축하 연출 (TODO)"));
            // PlayCelebrationEffect(); // 추후 구현

            // ====== Step 5: 클리어 UI 등장 ======
            _mainSequence.AppendInterval(_stepDelay);
            _mainSequence.AppendCallback(() => SetupResultUI());
            _mainSequence.AppendCallback(() => Debug.Log("[LevelClearSequence] Step 5: 클리어 UI 등장"));

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

            // 시퀀스 완료 콜백
            _mainSequence.AppendCallback(() => Debug.Log("[LevelClearSequence] 시퀀스 재생 완료 - NEXT 버튼 대기"));
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

        /// <summary>
        /// 코인 날아가기 애니메이션을 재생합니다.
        /// </summary>
        private void PlayCoinFlyAnimation(Action onComplete)
        {
            if (_coinPrefab == null || _coinDestination == null || _rewardAmount <= 0)
            {
                Debug.Log("[LevelClearSequence] 코인 프리팹 또는 목적지가 없어서 스킵");
                onComplete?.Invoke();
                return;
            }

            // 시작 위치 (획득 코인 UI 위치)
            Vector3 startPos = _rewardContainer != null
                ? _rewardContainer.transform.position
                : transform.position;

            // 목적지 위치
            Vector3 endPos = _coinDestination.position;

            Debug.Log($"[LevelClearSequence] 코인 날아가기 시작: {_flyingCoinCount}개");

            // 코인 생성 및 애니메이션
            int completedCount = 0;
            Sequence coinSequence = DOTween.Sequence();

            for (int i = 0; i < _flyingCoinCount; i++)
            {
                float delay = i * _coinSpawnInterval;

                coinSequence.InsertCallback(delay, () =>
                {
                    // 코인 생성
                    GameObject coin = Instantiate(_coinPrefab, startPos, Quaternion.identity, transform);

                    // 랜덤 오프셋으로 시작 위치 분산
                    Vector3 randomOffset = new Vector3(
                        UnityEngine.Random.Range(-50f, 50f),
                        UnityEngine.Random.Range(-50f, 50f),
                        0
                    );
                    coin.transform.position = startPos + randomOffset;

                    // 포물선 이동 애니메이션
                    Sequence singleCoinSeq = DOTween.Sequence();

                    // 위치 이동 (포물선 효과)
                    singleCoinSeq.Append(coin.transform.DOMove(endPos, _coinFlyDuration).SetEase(Ease.InQuad));

                    // 스케일 축소
                    singleCoinSeq.Join(coin.transform.DOScale(0.5f, _coinFlyDuration).SetEase(Ease.InQuad));

                    // 완료 시 제거
                    singleCoinSeq.OnComplete(() =>
                    {
                        Destroy(coin);
                        completedCount++;

                        // 모든 코인 완료 체크
                        if (completedCount >= _flyingCoinCount)
                        {
                            onComplete?.Invoke();
                        }
                    });
                });
            }
        }

        #endregion

        #region Celebration Effect (TODO)

        /// <summary>
        /// 축하 연출을 재생합니다. (파티클 시스템)
        /// </summary>
        private void PlayCelebrationEffect()
        {
            // TODO: 파티클 시스템 구현
            // - 폭죽 효과
            // - 색종이 효과
            Debug.Log("[LevelClearSequence] 축하 연출 재생 (TODO: 파티클 구현)");
        }

        #endregion

        #region Helper Classes

        /// <summary>
        /// 람다 클로저에서 Vector3 값을 안전하게 추적하기 위한 래퍼 클래스
        /// </summary>
        private class PositionTracker
        {
            public Vector3 lastPosition;
        }

        #endregion
    }
}
