using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using System.Collections;

/// <summary>
/// 씬 전환 관리자 - 페이드 인/아웃 효과로 부드러운 전환
/// DontDestroyOnLoad로 씬 간 유지
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private float _fadeDuration = 0.3f;
    [SerializeField] private Color _fadeColor = Color.black;

    [Header("Scene Names")]
    public const string LOBBY_SCENE = "LobbyScene";
    public const string GAME_SCENE = "GameScene";

    // 페이드용 UI
    private Canvas _fadeCanvas;
    private CanvasGroup _fadeCanvasGroup;
    private Image _fadeImage;

    // 전환 중 플래그
    private bool _isTransitioning = false;
    public bool IsTransitioning => _isTransitioning;

    private void Awake()
    {
        // 싱글턴 설정
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CreateFadeCanvas();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 페이드용 Canvas를 동적으로 생성합니다.
    /// </summary>
    private void CreateFadeCanvas()
    {
        // Canvas 생성
        GameObject canvasObj = new GameObject("FadeCanvas");
        canvasObj.transform.SetParent(transform);

        _fadeCanvas = canvasObj.AddComponent<Canvas>();
        _fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _fadeCanvas.sortingOrder = 9999; // 가장 앞에 표시

        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // CanvasGroup 생성 (알파 조절용)
        _fadeCanvasGroup = canvasObj.AddComponent<CanvasGroup>();
        _fadeCanvasGroup.alpha = 0f;
        _fadeCanvasGroup.blocksRaycasts = false;

        // 검정 이미지 생성
        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(canvasObj.transform, false);

        _fadeImage = imageObj.AddComponent<Image>();
        _fadeImage.color = _fadeColor;

        // 전체 화면 채우기
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 씬을 전환합니다. (페이드 효과 포함)
    /// </summary>
    /// <param name="sceneName">전환할 씬 이름</param>
    /// <param name="onSceneLoaded">씬 로드 완료 후 콜백</param>
    public void LoadScene(string sceneName, Action onSceneLoaded = null)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning("[SceneTransitionManager] 이미 전환 중입니다.");
            return;
        }

        StartCoroutine(TransitionCoroutine(sceneName, onSceneLoaded));
    }

    /// <summary>
    /// 로비 씬으로 전환합니다.
    /// </summary>
    public void LoadLobbyScene(Action onComplete = null)
    {
        LoadScene(LOBBY_SCENE, onComplete);
    }

    /// <summary>
    /// 게임 씬으로 전환합니다.
    /// </summary>
    public void LoadGameScene(Action onComplete = null)
    {
        LoadScene(GAME_SCENE, onComplete);
    }

    /// <summary>
    /// 씬 전환 코루틴
    /// </summary>
    private IEnumerator TransitionCoroutine(string sceneName, Action onComplete)
    {
        _isTransitioning = true;
        _fadeCanvasGroup.blocksRaycasts = true;

        // Fade Out (투명 → 불투명)
        yield return FadeCoroutine(0f, 1f);

        // 씬 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // 콜백 호출
        onComplete?.Invoke();

        // 약간의 대기 (새 씬 초기화 시간)
        yield return new WaitForSeconds(0.1f);

        // Fade In (불투명 → 투명)
        yield return FadeCoroutine(1f, 0f);

        _fadeCanvasGroup.blocksRaycasts = false;
        _isTransitioning = false;
    }

    /// <summary>
    /// 페이드 코루틴
    /// </summary>
    private IEnumerator FadeCoroutine(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;

        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / _fadeDuration;

            // Ease in-out
            float easedT = t * t * (3f - 2f * t);

            _fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, easedT);
            yield return null;
        }

        _fadeCanvasGroup.alpha = endAlpha;
    }

    /// <summary>
    /// 즉시 페이드 인 (씬 시작 시 사용)
    /// </summary>
    public void FadeInImmediate()
    {
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 0f;
            _fadeCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// 즉시 페이드 아웃 (화면 가림)
    /// </summary>
    public void FadeOutImmediate()
    {
        if (_fadeCanvasGroup != null)
        {
            _fadeCanvasGroup.alpha = 1f;
            _fadeCanvasGroup.blocksRaycasts = true;
        }
    }
}
