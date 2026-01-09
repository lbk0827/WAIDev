using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using DG.Tweening;
using WaiJigsaw.Data;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// 챕터 클리어 시퀀스 (3~6단계)
    /// - 3단계: 경계선 사라짐 + Confetti 연출
    /// - 4단계: 완성 이미지가 Collection 버튼으로 날아감
    /// - 5단계: 이미지가 버튼에 흡수됨
    /// - 6단계: 다음 챕터 카드 뿌리기 연출
    /// </summary>
    public class ChapterClearSequence : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private LobbyGridManager _lobbyGridManager;  // 로비 그리드 매니저
        [SerializeField] private RectTransform _collectionButton;     // Collection 버튼
        [SerializeField] private Image _completedImageOverlay;        // 완성 이미지 오버레이 (날아가는 용도)
        [SerializeField] private CanvasGroup _gridCanvasGroup;        // 그리드 페이드용 CanvasGroup (6단계에서 사용)

        [Header("Confetti")]
        [SerializeField] private CelebrationController _celebrationController;

        [Header("Timing Settings")]
        [SerializeField] private float _borderFadeDuration = 0.4f;    // 경계선 사라지는 시간
        [SerializeField] private float _confettiDuration = 1.2f;      // Confetti 재생 시간
        [SerializeField] private float _flyToCollectionDuration = 0.7f; // 이미지 날아가는 시간
        [SerializeField] private float _nextChapterDelay = 0.2f;      // 다음 챕터 전환 딜레이

        [Header("Animation Settings")]
        [SerializeField] private float _imageStartScale = 1.0f;       // 날아가기 시작 스케일
        [SerializeField] private float _imageEndScale = 0.1f;         // 날아가기 끝 스케일
        [SerializeField] private Ease _flyEase = Ease.InOutQuad;      // 날아가는 이징

        // 연출 상태
        private bool _isPlaying = false;
        private LevelGroupTableRecord _clearedGroup;
        private Sprite _completedSprite;

        /// <summary>
        /// 연출이 재생 중인지 여부
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 챕터 클리어 연출 완료 이벤트
        /// </summary>
        public event Action OnSequenceComplete;

        /// <summary>
        /// 다음 챕터 카드 뿌리기 요청 이벤트
        /// </summary>
        public event Action OnRequestNextChapterCards;

        /// <summary>
        /// 챕터 클리어 연출을 시작합니다.
        /// </summary>
        /// <param name="clearedGroup">클리어한 챕터 그룹 정보</param>
        /// <param name="completedSprite">완성된 챕터 이미지</param>
        public void Play(LevelGroupTableRecord clearedGroup, Sprite completedSprite)
        {
            Play(clearedGroup, completedSprite, null);
        }

        /// <summary>
        /// 챕터 클리어 연출을 시작합니다. (LobbyGridManager 참조 포함)
        /// </summary>
        /// <param name="clearedGroup">클리어한 챕터 그룹 정보</param>
        /// <param name="completedSprite">완성된 챕터 이미지</param>
        /// <param name="lobbyGridManager">LobbyGridManager 참조 (null이면 기존 참조 사용)</param>
        public void Play(LevelGroupTableRecord clearedGroup, Sprite completedSprite, LobbyGridManager lobbyGridManager)
        {
            if (_isPlaying)
            {
                Debug.LogWarning("[ChapterClearSequence] 이미 연출이 재생 중입니다.");
                return;
            }

            // LobbyGridManager 참조 설정 (전달받은 값 우선)
            if (lobbyGridManager != null)
            {
                _lobbyGridManager = lobbyGridManager;
                Debug.Log($"[ChapterClearSequence] LobbyGridManager 참조를 전달받았습니다: {_lobbyGridManager.gameObject.name}");
            }
            else if (_lobbyGridManager == null)
            {
                Debug.LogWarning("[ChapterClearSequence] LobbyGridManager 참조가 없습니다. Step 6에서 이벤트로 처리됩니다.");
            }

            _clearedGroup = clearedGroup;
            _completedSprite = completedSprite;
            _isPlaying = true;

            StartCoroutine(PlaySequenceCoroutine());
        }

        /// <summary>
        /// 연출 시퀀스 코루틴
        /// </summary>
        private IEnumerator PlaySequenceCoroutine()
        {
            Debug.Log($"[ChapterClearSequence] 챕터 {_clearedGroup.GroupID} 클리어 연출 시작");

            // === 3단계: 경계선 사라짐 + Confetti 연출 ===
            yield return StartCoroutine(Step3_FadeGridAndPlayConfetti());

            // === 4단계 & 5단계: 이미지가 Collection 버튼으로 날아감 ===
            yield return StartCoroutine(Step4_FlyToCollection());

            // Confetti 정지
            StopConfetti();

            // === 6단계: 다음 챕터 카드 뿌리기 연출 ===
            yield return new WaitForSeconds(_nextChapterDelay);
            yield return StartCoroutine(Step6_ShowNextChapter());

            // 연출 완료
            _isPlaying = false;
            Debug.Log($"[ChapterClearSequence] 챕터 {_clearedGroup.GroupID} 클리어 연출 완료");
            OnSequenceComplete?.Invoke();
        }

        /// <summary>
        /// 3단계: 그리드 경계선 사라짐 + Confetti 연출
        /// 카드 이미지는 유지하고 테두리(BlackBorder, WhiteBorder)만 페이드 아웃
        /// </summary>
        private IEnumerator Step3_FadeGridAndPlayConfetti()
        {
            Debug.Log("[ChapterClearSequence] Step 3: 경계선 사라짐 + Confetti");

            // Confetti 연출 먼저 시작 (경계선 페이드와 동시에)
            PlayConfetti();

            // 카드 테두리만 페이드 아웃 (카드 이미지는 유지)
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.FadeOutAllCardBorders(_borderFadeDuration);
            }

            // 경계선 페이드 완료 대기
            yield return new WaitForSeconds(_borderFadeDuration);

            // Confetti 추가 재생 시간 대기
            yield return new WaitForSeconds(_confettiDuration);

            // 완성 이미지 오버레이 준비 (그리드 위치/크기에 맞춤)
            if (_completedImageOverlay != null && _completedSprite != null)
            {
                _completedImageOverlay.sprite = _completedSprite;

                // 그리드 컨테이너 위치/크기에 맞춤
                if (_lobbyGridManager != null)
                {
                    RectTransform gridRect = _lobbyGridManager.GetGridContainerRect();
                    if (gridRect != null)
                    {
                        RectTransform overlayRect = _completedImageOverlay.rectTransform;
                        // 그리드와 동일한 위치/크기로 설정
                        overlayRect.position = gridRect.position;
                        overlayRect.sizeDelta = gridRect.sizeDelta;
                    }
                }

                _completedImageOverlay.gameObject.SetActive(true);
                _completedImageOverlay.color = Color.white; // 즉시 표시
            }

            // 그리드 숨기기 (완성 이미지가 대체)
            if (_gridCanvasGroup != null)
            {
                _gridCanvasGroup.alpha = 0f;
            }
        }

        // 오버레이 원래 위치/크기 저장용
        private Vector3 _originalOverlayPosition;
        private Vector2 _originalOverlaySizeDelta;
        private Vector3 _originalOverlayScale;

        /// <summary>
        /// 4단계 & 5단계: 완성 이미지가 Collection 버튼으로 날아감
        /// </summary>
        private IEnumerator Step4_FlyToCollection()
        {
            Debug.Log("[ChapterClearSequence] Step 4-5: 이미지가 Collection으로 날아감");

            if (_completedImageOverlay == null || _collectionButton == null)
            {
                Debug.LogWarning("[ChapterClearSequence] 필요한 UI 참조가 없습니다.");
                yield break;
            }

            // Collection 버튼의 월드 위치
            Vector3 targetPosition = _collectionButton.position;

            // 원래 위치/크기 저장 (나중에 복원용)
            RectTransform overlayRect = _completedImageOverlay.rectTransform;
            _originalOverlayPosition = overlayRect.position;
            _originalOverlaySizeDelta = overlayRect.sizeDelta;
            _originalOverlayScale = overlayRect.localScale;

            // DOTween 시퀀스로 이동 + 축소 동시 실행
            Sequence flySequence = DOTween.Sequence();

            // 이동 애니메이션
            flySequence.Append(
                overlayRect.DOMove(targetPosition, _flyToCollectionDuration)
                    .SetEase(_flyEase)
            );

            // 축소 애니메이션 (동시 실행)
            flySequence.Join(
                overlayRect.DOScale(_imageEndScale, _flyToCollectionDuration)
                    .SetEase(Ease.InQuad)
            );

            // 마지막에 페이드 아웃 (버튼에 흡수되는 느낌)
            flySequence.Join(
                _completedImageOverlay.DOFade(0f, _flyToCollectionDuration * 0.3f)
                    .SetDelay(_flyToCollectionDuration * 0.7f)
            );

            // Collection 버튼 펌핑 효과 (이미지가 도착할 때)
            flySequence.InsertCallback(_flyToCollectionDuration * 0.9f, () =>
            {
                PlayCollectionButtonPump();
            });

            yield return flySequence.WaitForCompletion();

            // 오버레이 숨기기 및 초기화
            _completedImageOverlay.gameObject.SetActive(false);
            overlayRect.position = _originalOverlayPosition;
            overlayRect.sizeDelta = _originalOverlaySizeDelta;
            overlayRect.localScale = _originalOverlayScale;
        }

        /// <summary>
        /// 6단계: 다음 챕터 카드 뿌리기
        /// </summary>
        private IEnumerator Step6_ShowNextChapter()
        {
            Debug.Log("[ChapterClearSequence] Step 6: 다음 챕터 카드 뿌리기");

            // 현재 레벨 가져오기
            int currentLevel = WaiJigsaw.Data.GameDataContainer.Instance.CurrentLevel;
            Debug.Log($"[ChapterClearSequence] 다음 챕터 로드 - CurrentLevel: {currentLevel}");

            // 직접 LobbyGridManager 호출 (이벤트 대신)
            if (_lobbyGridManager != null)
            {
                _lobbyGridManager.SetupGridWithDealingAnimation(currentLevel);
            }
            else
            {
                Debug.LogError("[ChapterClearSequence] _lobbyGridManager가 null입니다!");
                // 이벤트도 발생시켜서 백업 처리
                OnRequestNextChapterCards?.Invoke();
            }

            // 그리드 다시 표시 (새로 생성된 다음 챕터 카드들이 보임)
            if (_gridCanvasGroup != null)
            {
                _gridCanvasGroup.alpha = 1f;
            }

            yield return null;
        }

        /// <summary>
        /// Confetti 연출 재생
        /// </summary>
        private void PlayConfetti()
        {
            if (_celebrationController != null)
            {
                _celebrationController.Play();
            }
        }

        /// <summary>
        /// Confetti 연출 정지
        /// </summary>
        private void StopConfetti()
        {
            if (_celebrationController != null)
            {
                _celebrationController.Stop();
            }
        }

        /// <summary>
        /// Collection 버튼 펌핑 효과
        /// </summary>
        private void PlayCollectionButtonPump()
        {
            if (_collectionButton == null) return;

            // 펌핑 애니메이션 (1.0 -> 1.2 -> 1.0)
            _collectionButton.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 0.5f);
        }

        /// <summary>
        /// 연출을 강제 중지합니다.
        /// </summary>
        public void Stop()
        {
            if (!_isPlaying) return;

            StopAllCoroutines();
            DOTween.Kill(_completedImageOverlay);
            DOTween.Kill(_gridCanvasGroup);
            DOTween.Kill(_collectionButton);

            StopConfetti();

            // UI 상태 초기화
            if (_completedImageOverlay != null)
            {
                _completedImageOverlay.gameObject.SetActive(false);
                _completedImageOverlay.rectTransform.localScale = Vector3.one;
            }

            if (_gridCanvasGroup != null)
            {
                _gridCanvasGroup.alpha = 1f;
            }

            _isPlaying = false;
        }
    }
}
