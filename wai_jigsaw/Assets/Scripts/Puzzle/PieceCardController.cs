using UnityEngine;
using System.Collections;

/// <summary>
/// 퍼즐 조각의 카드 시스템을 관리합니다.
/// - 카드 뒷면 생성 및 관리
/// - 카드 뒤집기 애니메이션
/// - 앞면/뒷면 전환
/// </summary>
public class PieceCardController : MonoBehaviour
{
    // ====== 카드 상태 ======
    private SpriteRenderer _cardBackRenderer;   // 카드 뒷면
    private SpriteRenderer _frontRenderer;      // 퍼즐 이미지 (앞면)
    private bool _isFlipped = false;            // true = 앞면(퍼즐), false = 뒷면
    private bool _canDrag = false;              // 드래그 가능 여부

    // ====== 참조 ======
    private DragController _dragController;     // 프레임 제어용

    // ====== 셰이더 프로퍼티 ======
    private MaterialPropertyBlock _cardBackPropertyBlock;
    private static Material _sharedRoundedMaterial;

    private const string CORNER_RADII_PROPERTY = "_CornerRadii";
    private const string UV_RECT_PROPERTY = "_UVRect";
    private const string PADDING_PROPERTY = "_Padding";
    private const string CORNER_RADIUS_PROPERTY = "_CornerRadius";

    #region Initialization

    /// <summary>
    /// 카드 컨트롤러를 초기화합니다.
    /// </summary>
    public void Initialize(SpriteRenderer frontRenderer, DragController dragController, Material sharedMaterial = null)
    {
        _frontRenderer = frontRenderer;
        _dragController = dragController;
        if (sharedMaterial != null)
        {
            _sharedRoundedMaterial = sharedMaterial;
        }
    }

    /// <summary>
    /// 카드 뒷면을 생성합니다.
    /// </summary>
    public void CreateCardBack(Sprite cardBackSprite, float defaultCornerRadius)
    {
        if (_frontRenderer == null) return;

        float width = _frontRenderer.bounds.size.x;
        float height = _frontRenderer.bounds.size.y;

        // 카드 뒷면 생성 (퍼즐 이미지 위에 덮음)
        GameObject backObj = new GameObject("CardBack");
        backObj.transform.SetParent(transform, false);
        backObj.transform.localPosition = Vector3.zero;

        _cardBackRenderer = backObj.AddComponent<SpriteRenderer>();
        Sprite backSprite;
        if (cardBackSprite != null)
        {
            backSprite = cardBackSprite;
            _cardBackRenderer.sprite = backSprite;
        }
        else
        {
            backSprite = CreatePixelSprite();
            _cardBackRenderer.sprite = backSprite;
            _cardBackRenderer.color = new Color(0.2f, 0.3f, 0.5f); // 기본 파란색
        }
        _cardBackRenderer.sortingOrder = 10; // 퍼즐 이미지와 테두리 위에

        // 스프라이트 원본 크기 기준으로 스케일 계산
        float backSpriteWidth = backSprite.bounds.size.x;
        float backSpriteHeight = backSprite.bounds.size.y;
        float backScaleX = width / backSpriteWidth;
        float backScaleY = height / backSpriteHeight;
        backObj.transform.localScale = new Vector3(backScaleX, backScaleY, 1);

        // 초기 상태: 뒷면이 보이는 상태
        _isFlipped = false;
        _frontRenderer.enabled = false; // 퍼즐 이미지 숨김

        // 기존 테두리 숨기기 (뒷면 상태에서는 보이지 않음)
        if (_dragController != null)
        {
            _dragController.ShowFrames(false);
        }

        // 카드 뒷면에 셰이더 적용
        ApplyShaderToCardBack(defaultCornerRadius);
    }

    /// <summary>
    /// 카드 뒷면에 둥근 모서리 셰이더를 적용합니다.
    /// </summary>
    private void ApplyShaderToCardBack(float cornerRadius)
    {
        if (_cardBackRenderer == null || _sharedRoundedMaterial == null)
            return;

        // 카드 뒷면에 동일한 Material 적용
        _cardBackRenderer.sharedMaterial = _sharedRoundedMaterial;

        // 카드 뒷면 스프라이트의 UV 계산
        Sprite backSprite = _cardBackRenderer.sprite;
        if (backSprite == null) return;

        Texture2D texture = backSprite.texture;
        Rect spriteRect = backSprite.rect;
        float uvMinX = spriteRect.x / texture.width;
        float uvMinY = spriteRect.y / texture.height;
        float uvMaxX = (spriteRect.x + spriteRect.width) / texture.width;
        float uvMaxY = (spriteRect.y + spriteRect.height) / texture.height;
        Vector4 uvRect = new Vector4(uvMinX, uvMinY, uvMaxX, uvMaxY);

        // 카드 뒷면용 PropertyBlock 설정
        _cardBackPropertyBlock = new MaterialPropertyBlock();
        _cardBackRenderer.GetPropertyBlock(_cardBackPropertyBlock);

        Vector4 cornerRadii = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);

        _cardBackPropertyBlock.SetVector(UV_RECT_PROPERTY, uvRect);
        _cardBackPropertyBlock.SetFloat(CORNER_RADIUS_PROPERTY, cornerRadius);
        _cardBackPropertyBlock.SetVector(CORNER_RADII_PROPERTY, cornerRadii);
        _cardBackPropertyBlock.SetVector(PADDING_PROPERTY, Vector4.zero);
        _cardBackPropertyBlock.SetTexture("_MainTex", texture);

        _cardBackRenderer.SetPropertyBlock(_cardBackPropertyBlock);
    }

    private Sprite CreatePixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    #endregion

    #region Card Flip

    /// <summary>
    /// 카드를 뒤집습니다 (애니메이션).
    /// </summary>
    public void FlipCard(float duration = 0.3f, System.Action onComplete = null)
    {
        if (_isFlipped)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(FlipAnimation(duration, onComplete));
    }

    private IEnumerator FlipAnimation(float duration, System.Action onComplete)
    {
        float halfDuration = duration / 2f;

        // 1단계: 카드가 옆으로 납작해짐 (뒷면이 사라지는 느낌)
        Vector3 originalScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(1f, 0f, t);
            transform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        // 중간: 뒷면 숨기고 앞면 보이기
        if (_cardBackRenderer != null)
        {
            _cardBackRenderer.gameObject.SetActive(false);
        }
        _frontRenderer.enabled = true;

        // 테두리 다시 활성화 (프레임 방식)
        if (_dragController != null)
        {
            _dragController.ShowFrames(true);
        }

        // 2단계: 카드가 다시 펼쳐짐 (앞면이 나타나는 느낌)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float scaleX = Mathf.Lerp(0f, 1f, t);
            transform.localScale = new Vector3(scaleX * originalScale.x, originalScale.y, originalScale.z);
            yield return null;
        }

        transform.localScale = originalScale;
        _isFlipped = true;

        onComplete?.Invoke();
    }

    /// <summary>
    /// 즉시 앞면으로 전환합니다 (애니메이션 없이).
    /// </summary>
    public void ShowFrontImmediate()
    {
        if (_cardBackRenderer != null)
            _cardBackRenderer.gameObject.SetActive(false);

        if (_frontRenderer != null)
            _frontRenderer.enabled = true;

        _isFlipped = true;

        // 테두리 활성화 (프레임 방식)
        if (_dragController != null)
        {
            _dragController.ShowFrames(true);
        }
    }

    /// <summary>
    /// 즉시 뒷면으로 전환합니다 (애니메이션 없이).
    /// </summary>
    public void ShowBackImmediate()
    {
        if (_cardBackRenderer != null)
            _cardBackRenderer.gameObject.SetActive(true);

        if (_frontRenderer != null)
            _frontRenderer.enabled = false;

        _isFlipped = false;

        // 테두리 숨김 (프레임 방식)
        if (_dragController != null)
        {
            _dragController.ShowFrames(false);
        }
    }

    #endregion

    #region Properties

    /// <summary>
    /// 카드가 뒤집혔는지 확인합니다.
    /// </summary>
    public bool IsFlipped => _isFlipped;

    /// <summary>
    /// 카드 뒷면 렌더러를 반환합니다.
    /// </summary>
    public SpriteRenderer CardBackRenderer => _cardBackRenderer;

    /// <summary>
    /// 드래그 가능 여부를 설정합니다.
    /// </summary>
    public bool CanDrag
    {
        get => _canDrag;
        set => _canDrag = value;
    }

    /// <summary>
    /// 카드 뒷면 PropertyBlock을 반환합니다 (셰이더 프로퍼티 설정용).
    /// </summary>
    public MaterialPropertyBlock CardBackPropertyBlock => _cardBackPropertyBlock;

    #endregion

    #region Shader Updates

    /// <summary>
    /// 모서리 반경을 업데이트합니다.
    /// </summary>
    public void UpdateCornerRadii(Vector4 cornerRadii)
    {
        if (_cardBackPropertyBlock != null && _cardBackRenderer != null)
        {
            _cardBackPropertyBlock.SetVector(CORNER_RADII_PROPERTY, cornerRadii);
            _cardBackRenderer.SetPropertyBlock(_cardBackPropertyBlock);
        }
    }

    /// <summary>
    /// 패딩을 업데이트합니다.
    /// </summary>
    public void UpdatePadding(Vector4 padding)
    {
        if (_cardBackPropertyBlock != null && _cardBackRenderer != null)
        {
            _cardBackPropertyBlock.SetVector(PADDING_PROPERTY, padding);
            _cardBackRenderer.SetPropertyBlock(_cardBackPropertyBlock);
        }
    }

    #endregion
}
