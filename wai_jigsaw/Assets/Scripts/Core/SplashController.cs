using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace WaiJigsaw.Core
{
    /// <summary>
    /// 스플래시 화면 컨트롤러
    /// - 회사 로고 표시 (DUG_LOGO)
    /// - 로딩 표시
    /// - 로비 씬으로 전환
    /// </summary>
    public class SplashController : MonoBehaviour
    {
        [Header("Company Logo")]
        [Tooltip("회사 로고 CanvasGroup (alpha 페이드용)")]
        [SerializeField] private CanvasGroup _companyLogoGroup;

        [Header("Loading")]
        [Tooltip("로딩 표시 CanvasGroup")]
        [SerializeField] private CanvasGroup _loadingGroup;
        [Tooltip("로딩 텍스트 (선택적)")]
        [SerializeField] private TMP_Text _loadingText;

        [Header("Timing")]
        [Tooltip("로고 페이드 인 시간")]
        [SerializeField] private float _logoFadeInDuration = 0.5f;
        [Tooltip("로고 표시 유지 시간")]
        [SerializeField] private float _logoDisplayDuration = 1.5f;
        [Tooltip("로고 페이드 아웃 시간")]
        [SerializeField] private float _logoFadeOutDuration = 0.5f;
        [Tooltip("로딩 표시 최소 시간")]
        [SerializeField] private float _loadingMinDuration = 0.5f;

        [Header("Managers")]
        [Tooltip("GameManager 프리팹 (없으면 씬에서 찾거나 새로 생성)")]
        [SerializeField] private GameManager _gameManagerPrefab;

        private void Start()
        {
            // 초기 상태 설정
            InitializeUI();

            // 스플래시 시퀀스 시작
            StartCoroutine(SplashSequence());
        }

        /// <summary>
        /// UI 초기 상태 설정 (모든 그룹 숨김)
        /// </summary>
        private void InitializeUI()
        {
            if (_companyLogoGroup != null)
            {
                _companyLogoGroup.alpha = 0f;
            }

            if (_loadingGroup != null)
            {
                _loadingGroup.alpha = 0f;
            }
        }

        /// <summary>
        /// 메인 스플래시 시퀀스
        /// </summary>
        private IEnumerator SplashSequence()
        {
            // 1. GameManager 생성 (초기화 수행)
            EnsureGameManager();

            // 잠시 대기 (GameManager 초기화 완료 대기)
            yield return new WaitForSeconds(0.1f);

            // 2. 회사 로고 표시
            yield return ShowCompanyLogo();

            // 3. 로딩 표시
            yield return ShowLoading();

            // 4. 로비 씬으로 전환
            TransitionToLobby();
        }

        /// <summary>
        /// GameManager가 존재하는지 확인하고, 없으면 생성합니다.
        /// </summary>
        private void EnsureGameManager()
        {
            if (GameManager.Instance != null)
            {
                Debug.Log("[SplashController] GameManager가 이미 존재합니다.");
                return;
            }

            if (_gameManagerPrefab != null)
            {
                Debug.Log("[SplashController] GameManager 프리팹으로 생성합니다.");
                Instantiate(_gameManagerPrefab);
            }
            else
            {
                // 프리팹이 없으면 새 GameObject 생성
                Debug.Log("[SplashController] GameManager를 새로 생성합니다.");
                GameObject go = new GameObject("GameManager");
                go.AddComponent<GameManager>();
            }
        }

        /// <summary>
        /// 회사 로고 표시 시퀀스
        /// </summary>
        private IEnumerator ShowCompanyLogo()
        {
            if (_companyLogoGroup == null)
            {
                Debug.LogWarning("[SplashController] CompanyLogoGroup이 할당되지 않았습니다. 로고 단계를 건너뜁니다.");
                yield break;
            }

            // 페이드 인
            yield return FadeCanvasGroup(_companyLogoGroup, 0f, 1f, _logoFadeInDuration);

            // 표시 유지
            yield return new WaitForSeconds(_logoDisplayDuration);

            // 페이드 아웃
            yield return FadeCanvasGroup(_companyLogoGroup, 1f, 0f, _logoFadeOutDuration);
        }

        /// <summary>
        /// 로딩 표시 시퀀스
        /// </summary>
        private IEnumerator ShowLoading()
        {
            if (_loadingGroup == null)
            {
                Debug.LogWarning("[SplashController] LoadingGroup이 할당되지 않았습니다. 로딩 단계를 건너뜁니다.");
                yield break;
            }

            // 페이드 인
            yield return FadeCanvasGroup(_loadingGroup, 0f, 1f, 0.3f);

            // 로딩 텍스트 애니메이션 (점 추가)
            if (_loadingText != null)
            {
                float elapsed = 0f;
                int dotCount = 0;

                while (elapsed < _loadingMinDuration)
                {
                    dotCount = (dotCount % 3) + 1;
                    _loadingText.text = "Loading" + new string('.', dotCount);

                    yield return new WaitForSeconds(0.3f);
                    elapsed += 0.3f;
                }
            }
            else
            {
                yield return new WaitForSeconds(_loadingMinDuration);
            }
        }

        /// <summary>
        /// 로비 씬으로 전환
        /// </summary>
        private void TransitionToLobby()
        {
            Debug.Log("[SplashController] 로비 씬으로 전환합니다.");

            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.LoadLobbyScene();
            }
            else
            {
                // SceneTransitionManager가 없으면 직접 로드
                Debug.LogWarning("[SplashController] SceneTransitionManager가 없습니다. 직접 씬을 로드합니다.");
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneTransitionManager.LOBBY_SCENE);
            }
        }

        /// <summary>
        /// CanvasGroup 알파 페이드 코루틴
        /// </summary>
        private IEnumerator FadeCanvasGroup(CanvasGroup group, float startAlpha, float endAlpha, float duration)
        {
            if (group == null) yield break;

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Ease in-out
                float easedT = t * t * (3f - 2f * t);

                group.alpha = Mathf.Lerp(startAlpha, endAlpha, easedT);
                yield return null;
            }

            group.alpha = endAlpha;
        }
    }
}
