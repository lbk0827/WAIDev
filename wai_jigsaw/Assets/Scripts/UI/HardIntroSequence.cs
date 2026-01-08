using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;

namespace WaiJigsaw.UI
{
    /// <summary>
    /// Hard 난이도 진입 시퀀스를 관리하는 컴포넌트
    ///
    /// 연출 순서 (총 1.0~1.5초):
    /// 1. 입력 잠금 (InputLock = true)
    /// 2. 레드 톤 오버레이 페이드 인
    /// 3. "HARD" 배너 등장 (스케일 애니메이션)
    /// 4. 배너 유지 (0.6~0.8초)
    /// 5. 배너 페이드 아웃
    /// 6. 레드 톤 오버레이 페이드 아웃
    /// 7. 입력 잠금 해제 (InputLock = false)
    /// </summary>
    public class HardIntroSequence : MonoBehaviour
    {
        [Header("====== Overlay ======")]
        [Tooltip("레드 톤 오버레이 이미지 (반투명 빨간색)")]
        [SerializeField] private Image _redTintOverlay;
        [Tooltip("오버레이 최대 알파값 (0.15~0.25 권장)")]
        [SerializeField] private float _overlayMaxAlpha = 0.2f;

        [Header("====== Hard Banner ======")]
        [Tooltip("HARD 배너 컨테이너 (배경 이미지 + 텍스트 포함)")]
        [SerializeField] private GameObject _hardBannerContainer;
        [Tooltip("HARD 텍스트 (TMP_Text)")]
        [SerializeField] private TMP_Text _hardText;

        [Header("====== Animation Settings ======")]
        [Tooltip("오버레이 페이드 시간 (초)")]
        [SerializeField] private float _overlayFadeDuration = 0.2f;
        [Tooltip("배너 등장 시간 (초)")]
        [SerializeField] private float _bannerAppearDuration = 0.3f;
        [Tooltip("배너 유지 시간 (초)")]
        [SerializeField] private float _bannerHoldDuration = 0.7f;
        [Tooltip("배너 페이드 아웃 시간 (초)")]
        [SerializeField] private float _bannerFadeOutDuration = 0.25f;

        [Header("====== References ======")]
        [Tooltip("PuzzleBoardSetup (입력 잠금 제어용)")]
        [SerializeField] private PuzzleBoardSetup _puzzleBoardSetup;

        // 시퀀스 상태
        private bool _isPlaying = false;
        private Sequence _mainSequence;

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
            // 초기 상태: UI 숨김
            HideIntroUI();
        }

        private void OnDestroy()
        {
            // 시퀀스 정리
            _mainSequence?.Kill();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Hard 인트로 시퀀스를 시작합니다.
        /// </summary>
        /// <param name="onComplete">시퀀스 완료 후 콜백</param>
        public void PlayHardIntro(Action onComplete = null)
        {
            if (_isPlaying)
            {
                Debug.LogWarning("[HardIntroSequence] 이미 시퀀스가 재생 중입니다.");
                return;
            }

            _isPlaying = true;
            _onSequenceComplete = onComplete;

            Debug.Log("[HardIntroSequence] Hard 인트로 시퀀스 시작");

            // 입력 잠금
            SetInputLock(true);

            // DOTween 시퀀스 생성 및 실행
            CreateAndPlaySequence();
        }

        /// <summary>
        /// 시퀀스를 리셋합니다.
        /// </summary>
        public void ResetSequence()
        {
            _mainSequence?.Kill();
            _isPlaying = false;

            // 입력 잠금 해제
            SetInputLock(false);

            // UI 숨김
            HideIntroUI();
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

            // ====== Step 1: 레드 오버레이 페이드 인 ======
            if (_redTintOverlay != null)
            {
                _redTintOverlay.gameObject.SetActive(true);
                Color overlayColor = _redTintOverlay.color;
                overlayColor.a = 0f;
                _redTintOverlay.color = overlayColor;

                _mainSequence.Append(_redTintOverlay.DOFade(_overlayMaxAlpha, _overlayFadeDuration));
            }

            // ====== Step 2: Hard 배너 등장 (스케일 애니메이션) ======
            if (_hardBannerContainer != null)
            {
                _hardBannerContainer.SetActive(true);
                _hardBannerContainer.transform.localScale = Vector3.zero;

                _mainSequence.Append(
                    _hardBannerContainer.transform
                        .DOScale(1f, _bannerAppearDuration)
                        .SetEase(Ease.OutBack)
                );
            }

            // ====== Step 3: 배너 유지 시간 ======
            _mainSequence.AppendInterval(_bannerHoldDuration);

            // ====== Step 4: 배너 페이드 아웃 ======
            if (_hardBannerContainer != null)
            {
                // CanvasGroup으로 페이드 아웃
                CanvasGroup bannerCG = _hardBannerContainer.GetComponent<CanvasGroup>();
                if (bannerCG == null)
                {
                    bannerCG = _hardBannerContainer.AddComponent<CanvasGroup>();
                }

                _mainSequence.Append(bannerCG.DOFade(0f, _bannerFadeOutDuration));
            }

            // ====== Step 5: 레드 오버레이 페이드 아웃 ======
            if (_redTintOverlay != null)
            {
                _mainSequence.Join(_redTintOverlay.DOFade(0f, _overlayFadeDuration));
            }

            // ====== Step 6: 시퀀스 완료 처리 ======
            _mainSequence.OnComplete(() =>
            {
                Debug.Log("[HardIntroSequence] Hard 인트로 시퀀스 완료");

                // 입력 잠금 해제
                SetInputLock(false);

                // UI 숨김
                HideIntroUI();

                // 상태 초기화
                _isPlaying = false;

                // 콜백 호출
                _onSequenceComplete?.Invoke();
            });
        }

        #endregion

        #region UI Control

        /// <summary>
        /// 인트로 UI를 숨깁니다.
        /// </summary>
        private void HideIntroUI()
        {
            if (_redTintOverlay != null)
            {
                _redTintOverlay.gameObject.SetActive(false);
            }

            if (_hardBannerContainer != null)
            {
                _hardBannerContainer.SetActive(false);

                // CanvasGroup 알파 복원
                CanvasGroup bannerCG = _hardBannerContainer.GetComponent<CanvasGroup>();
                if (bannerCG != null)
                {
                    bannerCG.alpha = 1f;
                }
            }
        }

        /// <summary>
        /// 입력 잠금을 설정합니다.
        /// </summary>
        /// <param name="locked">true: 잠금, false: 해제</param>
        private void SetInputLock(bool locked)
        {
            if (_puzzleBoardSetup != null)
            {
                _puzzleBoardSetup.SetInputLock(locked);
            }
            else
            {
                Debug.LogWarning("[HardIntroSequence] PuzzleBoardSetup이 할당되지 않았습니다. 입력 잠금을 설정할 수 없습니다.");
            }
        }

        #endregion
    }
}
