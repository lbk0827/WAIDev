using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 로비의 개별 카드 슬롯을 관리합니다.
/// - 앞면(클리어된 조각 이미지) / 뒷면(레벨 번호) 상태
/// - 클릭 시 해당 레벨 시작
/// - 카드 플립 애니메이션
/// </summary>
public class LobbyCardSlot : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image _frontImage;      // 앞면 (조각 이미지)
    [SerializeField] private Image _backImage;       // 뒷면 (카드 뒷면 배경)
    [SerializeField] private TMP_Text _levelText;    // 레벨 번호 텍스트
    [SerializeField] private Button _button;         // 클릭 버튼

    [Header("Visual Settings")]
    [SerializeField] private Color _clearedColor = Color.white;
    [SerializeField] private Color _lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private Color _currentColor = new Color(1f, 0.9f, 0.5f, 1f); // 현재 레벨 (노란색)

    // 내부 데이터
    private int _levelNumber;
    private bool _isCleared;
    private bool _isCurrent;
    private bool _isFlipping;

    public int LevelNumber => _levelNumber;

    /// <summary>
    /// 런타임에서 참조를 설정합니다. (프리팹 없이 동적 생성 시 사용)
    /// </summary>
    public void SetReferences(Image frontImage, Image backImage, TMP_Text levelText, Button button)
    {
        _frontImage = frontImage;
        _backImage = backImage;
        _levelText = levelText;
        _button = button;
    }

    private void Awake()
    {
        // 컴포넌트 자동 찾기 (Inspector에서 설정 안 했을 경우)
        if (_button == null) _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.AddListener(OnCardClicked);
        }
    }

    /// <summary>
    /// 카드 슬롯을 초기화합니다.
    /// </summary>
    /// <param name="levelNumber">이 슬롯이 나타내는 레벨 번호</param>
    /// <param name="pieceSprite">클리어 시 보여줄 조각 스프라이트</param>
    /// <param name="isCleared">이미 클리어했는지 여부</param>
    /// <param name="isCurrent">현재 플레이할 레벨인지 여부</param>
    public void Initialize(int levelNumber, Sprite pieceSprite, bool isCleared, bool isCurrent)
    {
        _levelNumber = levelNumber;
        _isCleared = isCleared;
        _isCurrent = isCurrent;

        // 앞면 이미지 설정
        if (_frontImage != null && pieceSprite != null)
        {
            _frontImage.sprite = pieceSprite;
        }

        // 레벨 번호 텍스트 설정
        if (_levelText != null)
        {
            _levelText.text = levelNumber.ToString();
        }

        // 상태에 따라 표시 설정
        UpdateVisualState(false); // 애니메이션 없이 즉시 적용
    }

    /// <summary>
    /// 시각적 상태를 업데이트합니다.
    /// </summary>
    /// <param name="animate">플립 애니메이션 사용 여부</param>
    public void UpdateVisualState(bool animate = true)
    {
        if (_isCleared)
        {
            // 클리어 상태: 앞면 표시 (조각 이미지)
            if (animate && !_isFlipping)
            {
                StartCoroutine(FlipToFront());
            }
            else
            {
                ShowFront();
            }
        }
        else
        {
            // 미클리어 상태: 뒷면 표시 (레벨 번호)
            ShowBack();
        }
    }

    /// <summary>
    /// 앞면을 즉시 표시합니다.
    /// </summary>
    private void ShowFront()
    {
        if (_frontImage != null) _frontImage.gameObject.SetActive(true);
        if (_backImage != null) _backImage.gameObject.SetActive(false);
        if (_levelText != null) _levelText.gameObject.SetActive(false);

        // 클리어된 슬롯 색상
        if (_frontImage != null) _frontImage.color = _clearedColor;
    }

    /// <summary>
    /// 뒷면을 즉시 표시합니다.
    /// </summary>
    private void ShowBack()
    {
        if (_frontImage != null) _frontImage.gameObject.SetActive(false);
        if (_backImage != null) _backImage.gameObject.SetActive(true);
        if (_levelText != null) _levelText.gameObject.SetActive(true);

        // 현재 레벨 vs 잠긴 레벨 색상 구분
        Color targetColor = _isCurrent ? _currentColor : _lockedColor;
        if (_backImage != null) _backImage.color = targetColor;
    }

    /// <summary>
    /// 카드 플립 애니메이션 (뒷면 -> 앞면)
    /// </summary>
    private IEnumerator FlipToFront()
    {
        _isFlipping = true;
        float duration = 0.3f;
        float elapsed = 0f;

        // 1단계: 축소 (Y축 스케일 0으로)
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1f, 0f, elapsed / (duration / 2));
            transform.localScale = new Vector3(1f, scale, 1f);
            yield return null;
        }

        // 면 전환
        ShowFront();

        // 2단계: 확대 (Y축 스케일 1로)
        elapsed = 0f;
        while (elapsed < duration / 2)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1f, elapsed / (duration / 2));
            transform.localScale = new Vector3(1f, scale, 1f);
            yield return null;
        }

        transform.localScale = Vector3.one;
        _isFlipping = false;
    }

    /// <summary>
    /// 카드 클릭 시 호출됩니다.
    /// 현재는 레벨 진입 비활성화 (플레이 버튼으로 진입)
    /// </summary>
    private void OnCardClicked()
    {
        // 레벨 진입은 플레이 버튼으로 진행
        // 추후 카드 선택/미리보기 등 다른 기능 추가 가능
        Debug.Log($"카드 클릭: 레벨 {_levelNumber} (클리어: {_isCleared}, 현재: {_isCurrent})");
    }

    /// <summary>
    /// 잠긴 레벨 클릭 시 흔들림 효과
    /// </summary>
    private IEnumerator ShakeEffect()
    {
        Vector3 originalPos = transform.localPosition;
        float duration = 0.2f;
        float elapsed = 0f;
        float magnitude = 5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * magnitude * (1 - elapsed / duration);
            transform.localPosition = originalPos + new Vector3(x, 0, 0);
            yield return null;
        }

        transform.localPosition = originalPos;
    }

    /// <summary>
    /// 클리어 상태로 변경합니다 (애니메이션 포함).
    /// </summary>
    public void SetCleared()
    {
        _isCleared = true;
        _isCurrent = false;
        UpdateVisualState(true); // 플립 애니메이션 실행
    }

    /// <summary>
    /// 현재 레벨로 설정합니다.
    /// </summary>
    public void SetAsCurrent()
    {
        _isCurrent = true;
        if (!_isCleared)
        {
            ShowBack(); // 색상 업데이트
        }
    }
}
