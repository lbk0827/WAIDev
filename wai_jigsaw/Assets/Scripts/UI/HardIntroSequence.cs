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
        [Tooltip("레드 톤 오버레이 이미지 (반투명 빨간색) - 전체 화면 Dim")]
        [SerializeField] private Image _redTintOverlay;
        [Tooltip("오버레이 최대 알파값 (0.15~0.25 권장)")]
        [SerializeField] private float _overlayMaxAlpha = 0.2f;

        [Header("====== Hard Banner ======")]
        [Tooltip("HARD 배너 컨테이너 (배경 이미지 + 텍스트 포함)")]
        [SerializeField] private GameObject _hardBannerContainer;
        [Tooltip("HARD 텍스트 (TMP_Text)")]
        [SerializeField] private TMP_Text _hardText;
        [Tooltip("배너 뒤 Dim 이미지 (HardBannerContainer의 자식인 경우 별도 제어)")]
        [SerializeField] private Image _bannerDimImage;

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

            // Dim이 배너 자식인지 확인 (자식이면 별도 CanvasGroup으로 제어)
            bool isDimChildOfBanner = _bannerDimImage != null && _hardBannerContainer != null &&
                                      _bannerDimImage.transform.IsChildOf(_hardBannerContainer.transform);

            CanvasGroup dimCanvasGroup = null;
            if (isDimChildOfBanner && _bannerDimImage != null)
            {
                // Dim에 별도 CanvasGroup 추가하여 배너 CanvasGroup과 독립적으로 제어
                dimCanvasGroup = _bannerDimImage.GetComponent<CanvasGroup>();
                if (dimCanvasGroup == null)
                {
                    dimCanvasGroup = _bannerDimImage.gameObject.AddComponent<CanvasGroup>();
                }
            }

            // ====== Step 1: Dim + 오버레이 페이드 인 ======
            // 배너 Dim (HardTintDim) 페이드 인
            if (_bannerDimImage != null)
            {
                _bannerDimImage.gameObject.SetActive(true);

                if (isDimChildOfBanner && dimCanvasGroup != null)
                {
                    // 자식인 경우 CanvasGroup으로 알파 제어
                    dimCanvasGroup.alpha = 0f;
                    _mainSequence.Append(dimCanvasGroup.DOFade(_overlayMaxAlpha, _overlayFadeDuration));
                }
                else
                {
                    // 독립적인 경우 Image 알파 직접 제어
                    Color dimColor = _bannerDimImage.color;
                    dimColor.a = 0f;
                    _bannerDimImage.color = dimColor;
                    _mainSequence.Append(_bannerDimImage.DOFade(_overlayMaxAlpha, _overlayFadeDuration));
                }
            }

            // 레드 오버레이도 함께 페이드 인 (있으면)
            if (_redTintOverlay != null)
            {
                _redTintOverlay.gameObject.SetActive(true);
                Color overlayColor = _redTintOverlay.color;
                overlayColor.a = 0f;
                _redTintOverlay.color = overlayColor;

                if (_bannerDimImage != null)
                {
                    // Dim과 동시에 페이드 인
                    _mainSequence.Join(_redTintOverlay.DOFade(_overlayMaxAlpha, _overlayFadeDuration));
                }
                else
                {
                    // Dim이 없으면 오버레이만 페이드 인
                    _mainSequence.Append(_redTintOverlay.DOFade(_overlayMaxAlpha, _overlayFadeDuration));
                }
            }

            // ====== Step 2: Hard 배너 등장 (스케일 애니메이션) ======
            if (_hardBannerContainer != null)
            {
                _hardBannerContainer.SetActive(true);
                _hardBannerContainer.transform.localScale = Vector3.zero;

                // 배너 CanvasGroup 준비 (페이드 아웃용)
                CanvasGroup bannerCG = _hardBannerContainer.GetComponent<CanvasGroup>();
                if (bannerCG == null)
                {
                    bannerCG = _hardBannerContainer.AddComponent<CanvasGroup>();
                }
                bannerCG.alpha = 1f;

                _mainSequence.Append(
                    _hardBannerContainer.transform
                        .DOScale(1f, _bannerAppearDuration)
                        .SetEase(Ease.OutBack)
                );
            }

            // ====== Step 3: 배너 유지 시간 ======
            _mainSequence.AppendInterval(_bannerHoldDuration);

            // ====== Step 4: 배너 + Dim + 오버레이 동시 페이드 아웃 ======
            bool hasFadeOutStarted = false;

            // 배너 페이드 아웃
            if (_hardBannerContainer != null)
            {
                CanvasGroup bannerCG = _hardBannerContainer.GetComponent<CanvasGroup>();
                if (bannerCG != null)
                {
                    _mainSequence.Append(bannerCG.DOFade(0f, _bannerFadeOutDuration));
                    hasFadeOutStarted = true;
                }
            }

            // Dim 동시 페이드 아웃
            if (_bannerDimImage != null)
            {
                if (isDimChildOfBanner && dimCanvasGroup != null)
                {
                    // 자식인 경우 CanvasGroup으로 알파 제어
                    if (hasFadeOutStarted)
                    {
                        _mainSequence.Join(dimCanvasGroup.DOFade(0f, _bannerFadeOutDuration));
                    }
                    else
                    {
                        _mainSequence.Append(dimCanvasGroup.DOFade(0f, _bannerFadeOutDuration));
                        hasFadeOutStarted = true;
                    }
                }
                else
                {
                    // 독립적인 경우 Image 알파 직접 제어
                    if (hasFadeOutStarted)
                    {
                        _mainSequence.Join(_bannerDimImage.DOFade(0f, _bannerFadeOutDuration));
                    }
                    else
                    {
                        _mainSequence.Append(_bannerDimImage.DOFade(0f, _bannerFadeOutDuration));
                        hasFadeOutStarted = true;
                    }
                }
            }

            // 레드 오버레이도 동시 페이드 아웃
            if (_redTintOverlay != null)
            {
                if (hasFadeOutStarted)
                {
                    _mainSequence.Join(_redTintOverlay.DOFade(0f, _bannerFadeOutDuration));
                }
                else
                {
                    _mainSequence.Append(_redTintOverlay.DOFade(0f, _bannerFadeOutDuration));
                }
            }

            // ====== Step 5: 시퀀스 완료 처리 ======
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

            if (_bannerDimImage != null)
            {
                _bannerDimImage.gameObject.SetActive(false);
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
