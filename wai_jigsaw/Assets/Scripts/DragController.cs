using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WaiJigsaw.Data;

public class DragController : MonoBehaviour
{
    // ====== 퍼즐 데이터 ======
    [HideInInspector] public PuzzleBoardSetup board;
    [HideInInspector] public int currentSlotIndex;    // 현재 위치한 슬롯 인덱스 (0 ~ N)
    [HideInInspector] public int originalGridX;       // 정답 그리드 좌표 X
    [HideInInspector] public int originalGridY;       // 정답 그리드 좌표 Y

    // ====== 조각 크기 정보 (그룹화 시 위치 조정용) ======
    [HideInInspector] public float pieceWidth;
    [HideInInspector] public float pieceHeight;

    // ====== 그룹 시스템 ======
    public PieceGroup group; // 내가 속한 그룹

    // ====== 시각 효과 (프레임 방식 이중 테두리) ======
    private GameObject _whiteFrame;  // 안쪽 하얀색 프레임
    private GameObject _blackFrame;  // 바깥쪽 검정색 프레임
    private SpriteRenderer _whiteFrameRenderer;
    private SpriteRenderer _blackFrameRenderer;
    private MaterialPropertyBlock _whiteFramePropertyBlock;
    private MaterialPropertyBlock _blackFramePropertyBlock;

    // 테두리 두께 설정 (PuzzleBoardSetup에서 전달받음)
    private float _whiteBorderThickness = 0.025f;  // 조각 크기 대비 비율
    private float _blackBorderThickness = 0.008f;  // 조각 크기 대비 비율

    // ====== EdgeCover 시스템 (spacing 영역 가리기) ======
    // 0:Top, 1:Bottom, 2:Left, 3:Right
    private GameObject[] _edgeCovers = new GameObject[4];
    private float _coverSize = 0f;  // spacing/2 크기

    // ====== 카드 시스템 ======
    private SpriteRenderer _cardBackRenderer;   // 카드 뒷면
    private bool _isFlipped = false;            // true = 앞면(퍼즐), false = 뒷면
    private bool _canDrag = false;              // 드래그 가능 여부 (인트로 중에는 불가)

    private Vector3 _dragOffset;
    private float _cameraZDepth;
    private SpriteRenderer _spriteRenderer;
    private int _originalSortingOrder;

    // ====== 둥근 모서리 셰이더 ======
    private static Material _sharedRoundedMaterial;  // 공유 Material (메모리 효율)
    private static Shader _roundedShader;
    private MaterialPropertyBlock _propertyBlock;    // 개별 프로퍼티 설정용 (앞면)
    private MaterialPropertyBlock _cardBackPropertyBlock;  // 카드 뒷면용 PropertyBlock
    private const string ROUNDED_SHADER_NAME = "Custom/RoundedSprite";
    private const string CORNER_RADIUS_PROPERTY = "_CornerRadius";
    private const string CORNER_RADII_PROPERTY = "_CornerRadii";  // Vector4 (TL, TR, BL, BR)
    private const string UV_RECT_PROPERTY = "_UVRect";

    // ====== 프레임 셰이더 (오버레이 방식) ======
    private static Material _sharedFrameMaterial;  // 프레임용 공유 Material
    private static Shader _frameShader;
    private const string FRAME_SHADER_NAME = "Custom/RoundedFrame";
    private const string FRAME_THICKNESS_PROPERTY = "_FrameThickness";
    private const string HIDE_DIRECTIONS_PROPERTY = "_HideDirections";

    // 프레임 방향 숨김 상태 (Top, Bottom, Left, Right)
    private Vector4 _frameHideDirections = Vector4.zero;

    // 각 모서리 반경 저장 (TL, TR, BL, BR)
    private Vector4 _cornerRadii = new Vector4(0.05f, 0.05f, 0.05f, 0.05f);
    private float _defaultCornerRadius = 0.05f;

    // ====== Padding 시스템 (셰이더 기반 spacing) ======
    private const string PADDING_PROPERTY = "_Padding";  // Vector4 (Left, Right, Top, Bottom)
    private Vector4 _padding = Vector4.zero;  // 각 방향의 패딩값 (UV 비율)
    private float _defaultPaddingWorldSize = 0f;  // 기본 패딩값 (World Space 크기)

    void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        // 시작 시 자기 자신만의 그룹 생성
        group = new PieceGroup();
        group.AddPiece(this);
    }

    void Start()
    {
        _cameraZDepth = Camera.main.transform.position.z;

        // 테두리가 아직 생성되지 않았으면 생성
        if (_whiteFrame == null)
        {
            CreateFrameBorders();
        }
    }

    /// <summary>
    /// 테두리 두께를 설정합니다 (PuzzleBoardSetup에서 호출).
    /// </summary>
    public void SetBorderThickness(float whiteRatio, float blackRatio)
    {
        _whiteBorderThickness = whiteRatio;
        _blackBorderThickness = blackRatio;
    }

    /// <summary>
    /// 테두리 두께를 World Space 크기로 반환합니다 (GroupBorder용).
    /// 개별 카드의 프레임과 동일한 시각적 두께를 반환합니다.
    /// - whiteWidth: 흰색 테두리의 시각적 두께 (= 전체 두께 - 검정 두께)
    /// - blackWidth: 검정 테두리의 시각적 두께
    /// </summary>
    public void GetBorderThicknessWorldSpace(out float whiteWidth, out float blackWidth)
    {
        float baseSize = Mathf.Min(pieceWidth, pieceHeight);
        if (baseSize <= 0) baseSize = 1f;

        // 개별 카드 프레임의 시각적 두께와 일치하도록 계산
        // WhiteFrame: _whiteBorderThickness + _blackBorderThickness (전체)
        // BlackFrame: _blackBorderThickness (바깥쪽)
        // 실제 보이는 흰색 = 전체 - 검정 = _whiteBorderThickness
        float totalFrameThickness = _whiteBorderThickness + _blackBorderThickness;

        // GroupBorder LineRenderer용 (World Space)
        // - whiteWidth: 안쪽 흰색 선 두께 (시각적으로 보이는 흰색 영역)
        // - blackWidth: 바깥쪽 검정 선 두께
        whiteWidth = baseSize * totalFrameThickness;  // 전체 프레임 두께
        blackWidth = baseSize * _blackBorderThickness;

        Debug.Log($"[DragController] GetBorderThicknessWorldSpace - baseSize={baseSize:F3}, totalRatio={totalFrameThickness:F4}, blackRatio={_blackBorderThickness:F4}, whiteWidth={whiteWidth:F4}, blackWidth={blackWidth:F4}");
    }

    /// <summary>
    /// 모서리 반경을 World Space 크기로 반환합니다 (GroupBorder용).
    /// 병합 시 개별 모서리가 0으로 설정될 수 있으므로 기본값(_defaultCornerRadius)을 사용합니다.
    /// </summary>
    public float GetCornerRadiusWorldSpace()
    {
        float baseSize = Mathf.Min(pieceWidth, pieceHeight);
        if (baseSize <= 0) baseSize = 1f;

        // 기본 반경 사용 (병합 시에도 GroupBorder는 둥근 모서리 유지)
        return baseSize * _defaultCornerRadius;
    }

    private void OnMouseDown()
    {
        // 인트로 중에는 드래그 불가
        if (!_canDrag) return;

        // 팝업이 열려있으면 드래그 불가
        if (GameManager.Instance != null && GameManager.Instance.IsPopupOpen) return;

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        _cameraZDepth = screenPoint.z;

        // 드래그 시작점 저장 (누적 오차 방지용)
        Vector3 mouseStartPos = GetMouseWorldPos();

        // 그룹 전체에게 드래그 시작 알림
        group.OnDragStart(mouseStartPos);
        group.SetSortingOrder(100);

        board.OnPieceDragStart(this);
    }

    private void OnMouseDrag()
    {
        if (!_canDrag) return;

        // 팝업이 열려있으면 드래그 불가
        if (GameManager.Instance != null && GameManager.Instance.IsPopupOpen) return;

        // 마우스의 총 이동량 계산
        Vector3 currentMousePos = GetMouseWorldPos();
        group.OnDragUpdate(currentMousePos);
    }

    private void OnMouseUp()
    {
        if (!_canDrag) return;

        // 팝업이 열려있으면 드래그 불가
        if (GameManager.Instance != null && GameManager.Instance.IsPopupOpen) return;

        group.SetSortingOrder(1);

        if (board != null)
        {
            board.OnPieceDropped(this);
        }
    }
    
    // 외부에서 강제로 위치를 옮길 때 사용 (시작 위치 정보까지 갱신해야 함)
    public void UpdatePosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    public void SetPositionImmediate(Vector3 newPos)
    {
        transform.position = newPos;
    }
    /// <summary>
    /// 오버레이 방식의 이중 테두리를 생성합니다.
    /// 퍼즐 이미지 위에 프레임을 오버레이 (중앙 투명 셰이더 사용)
    /// </summary>
    void CreateFrameBorders()
    {
        if (_spriteRenderer == null) return;

        float width = _spriteRenderer.bounds.size.x;
        float height = _spriteRenderer.bounds.size.y;
        float baseSize = Mathf.Min(width, height);

        // 테두리 두께 계산 (World Space)
        float whiteThickness = baseSize * _whiteBorderThickness;
        float blackThickness = baseSize * _blackBorderThickness;

        // 1. 하얀 프레임 생성 (전체 테두리 영역, 아래 레이어)
        _whiteFrame = new GameObject("WhiteFrame");
        _whiteFrame.transform.SetParent(transform, false);
        _whiteFrame.transform.localPosition = Vector3.zero;

        _whiteFrameRenderer = _whiteFrame.AddComponent<SpriteRenderer>();
        _whiteFrameRenderer.sprite = CreateUnitSprite();
        _whiteFrameRenderer.color = Color.white;
        _whiteFrameRenderer.sortingOrder = 2;  // 퍼즐 이미지(1) 위

        // 하얀 프레임 크기 = 퍼즐 이미지와 동일
        _whiteFrame.transform.localScale = new Vector3(width, height, 1);

        // 2. 검정 프레임 생성 (바깥 테두리만, 위 레이어)
        _blackFrame = new GameObject("BlackFrame");
        _blackFrame.transform.SetParent(transform, false);
        _blackFrame.transform.localPosition = Vector3.zero;

        _blackFrameRenderer = _blackFrame.AddComponent<SpriteRenderer>();
        _blackFrameRenderer.sprite = CreateUnitSprite();
        _blackFrameRenderer.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        _blackFrameRenderer.sortingOrder = 3;  // 하얀 프레임 위

        // 검정 프레임 크기 = 퍼즐 이미지와 동일
        _blackFrame.transform.localScale = new Vector3(width, height, 1);

        // 프레임 셰이더 적용 (중앙 투명)
        ApplyShaderToFrames();
    }

    /// <summary>
    /// 프레임에 RoundedFrame 셰이더를 적용합니다 (중앙 투명, 오버레이 방식).
    /// </summary>
    private void ApplyShaderToFrames()
    {
        if (_whiteFrameRenderer == null || _blackFrameRenderer == null)
            return;

        // 프레임 셰이더 로드 (한 번만)
        if (_frameShader == null)
        {
            _frameShader = Shader.Find(FRAME_SHADER_NAME);
            if (_frameShader == null)
            {
                Debug.LogWarning($"[DragController] 프레임 셰이더를 찾을 수 없습니다: {FRAME_SHADER_NAME}");
                return;
            }
        }

        if (_sharedFrameMaterial == null)
        {
            _sharedFrameMaterial = new Material(_frameShader);
        }

        float baseSize = Mathf.Min(pieceWidth, pieceHeight);
        if (baseSize <= 0) baseSize = 1f;

        // 하얀 프레임 설정 (전체 테두리 영역 - 아래 레이어)
        _whiteFrameRenderer.sharedMaterial = _sharedFrameMaterial;
        _whiteFramePropertyBlock = new MaterialPropertyBlock();
        _whiteFrameRenderer.GetPropertyBlock(_whiteFramePropertyBlock);

        // 하얀 프레임 두께 = 전체 (하얀 + 검정)
        float whiteFrameThicknessUV = _whiteBorderThickness + _blackBorderThickness;
        _whiteFramePropertyBlock.SetFloat(FRAME_THICKNESS_PROPERTY, whiteFrameThicknessUV);
        _whiteFramePropertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
        _whiteFrameRenderer.SetPropertyBlock(_whiteFramePropertyBlock);

        // 검정 프레임 설정 (바깥쪽만 - 위 레이어)
        _blackFrameRenderer.sharedMaterial = _sharedFrameMaterial;
        _blackFramePropertyBlock = new MaterialPropertyBlock();
        _blackFrameRenderer.GetPropertyBlock(_blackFramePropertyBlock);

        // 검정 프레임 두께 = 검정만 (하얀 위에 덮어서 바깥 부분만 보이게)
        float blackFrameThicknessUV = _blackBorderThickness;
        _blackFramePropertyBlock.SetFloat(FRAME_THICKNESS_PROPERTY, blackFrameThicknessUV);
        _blackFramePropertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
        _blackFrameRenderer.SetPropertyBlock(_blackFramePropertyBlock);
    }

    Sprite CreatePixelSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
    }

    // 인접한 짝과 결합되었을 때 테두리 숨기기
    public void UpdateVisuals()
    {
        // Start() 전에 호출될 수 있으므로 테두리가 없으면 먼저 생성
        if (_whiteFrame == null)
        {
            CreateFrameBorders();
        }

        // 프레임 활성화
        ShowFrames(true);

        // 그룹 내의 다른 조각들과 관계를 확인하여 겹치는 부분 끄기
        // 프레임 방식에서는 Padding 셰이더로 간격을 처리함
    }

    public void HideBorder(int direction)
    {
        // 0:Top, 1:Bottom, 2:Left, 3:Right
        // 프레임 셰이더의 _HideDirections 업데이트
        switch (direction)
        {
            case 0: _frameHideDirections.x = 1f; break; // Top
            case 1: _frameHideDirections.y = 1f; break; // Bottom
            case 2: _frameHideDirections.z = 1f; break; // Left
            case 3: _frameHideDirections.w = 1f; break; // Right
        }
        ApplyFrameHideDirections();

        // 테두리 숨김 시 연결된 모서리도 직각 처리 (자연스러운 연결을 위해)
        // Top → TopLeft, TopRight 직각
        // Bottom → BottomLeft, BottomRight 직각
        // Left → TopLeft, BottomLeft 직각
        // Right → TopRight, BottomRight 직각
        switch (direction)
        {
            case 0: // Top
                _cornerRadii.x = 0f; // TopLeft
                _cornerRadii.y = 0f; // TopRight
                break;
            case 1: // Bottom
                _cornerRadii.z = 0f; // BottomLeft
                _cornerRadii.w = 0f; // BottomRight
                break;
            case 2: // Left
                _cornerRadii.x = 0f; // TopLeft
                _cornerRadii.z = 0f; // BottomLeft
                break;
            case 3: // Right
                _cornerRadii.y = 0f; // TopRight
                _cornerRadii.w = 0f; // BottomRight
                break;
        }
        ApplyCornerRadii();
    }

    /// <summary>
    /// 특정 방향의 프레임 테두리를 복원합니다.
    /// </summary>
    public void ShowBorder(int direction)
    {
        switch (direction)
        {
            case 0: _frameHideDirections.x = 0f; break; // Top
            case 1: _frameHideDirections.y = 0f; break; // Bottom
            case 2: _frameHideDirections.z = 0f; break; // Left
            case 3: _frameHideDirections.w = 0f; break; // Right
        }
        ApplyFrameHideDirections();

        // 테두리 복원 시 연결된 모서리도 복원 (인접 테두리가 모두 보일 때만)
        // 모서리는 인접한 두 테두리가 모두 보여야 둥글게 복원됨
        RecalculateCornerRadii();
    }

    /// <summary>
    /// 현재 테두리 숨김 상태에 따라 모서리 반경을 재계산합니다.
    /// 모서리는 인접한 두 테두리가 모두 보일 때만 둥글게 됩니다.
    /// </summary>
    private void RecalculateCornerRadii()
    {
        bool showTop = _frameHideDirections.x < 0.5f;
        bool showBottom = _frameHideDirections.y < 0.5f;
        bool showLeft = _frameHideDirections.z < 0.5f;
        bool showRight = _frameHideDirections.w < 0.5f;

        // TopLeft: Top과 Left 모두 보여야 둥글게
        _cornerRadii.x = (showTop && showLeft) ? _defaultCornerRadius : 0f;
        // TopRight: Top과 Right 모두 보여야 둥글게
        _cornerRadii.y = (showTop && showRight) ? _defaultCornerRadius : 0f;
        // BottomLeft: Bottom과 Left 모두 보여야 둥글게
        _cornerRadii.z = (showBottom && showLeft) ? _defaultCornerRadius : 0f;
        // BottomRight: Bottom과 Right 모두 보여야 둥글게
        _cornerRadii.w = (showBottom && showRight) ? _defaultCornerRadius : 0f;

        ApplyCornerRadii();
    }

    /// <summary>
    /// 모든 프레임 테두리를 복원합니다.
    /// </summary>
    public void ShowAllBorders()
    {
        _frameHideDirections = Vector4.zero;
        ApplyFrameHideDirections();

        // 모든 테두리가 보이면 모든 모서리도 둥글게 복원
        RecalculateCornerRadii();
    }

    /// <summary>
    /// 프레임 숨김 상태를 셰이더에 적용합니다.
    /// </summary>
    private void ApplyFrameHideDirections()
    {
        if (_whiteFramePropertyBlock != null && _whiteFrameRenderer != null)
        {
            _whiteFramePropertyBlock.SetVector(HIDE_DIRECTIONS_PROPERTY, _frameHideDirections);
            _whiteFrameRenderer.SetPropertyBlock(_whiteFramePropertyBlock);
        }
        if (_blackFramePropertyBlock != null && _blackFrameRenderer != null)
        {
            _blackFramePropertyBlock.SetVector(HIDE_DIRECTIONS_PROPERTY, _frameHideDirections);
            _blackFrameRenderer.SetPropertyBlock(_blackFramePropertyBlock);
        }
    }

    /// <summary>
    /// 프레임 전체를 보이거나 숨깁니다.
    /// </summary>
    public void ShowFrames(bool show)
    {
        if (_whiteFrame != null)
            _whiteFrame.SetActive(show);
        if (_blackFrame != null)
            _blackFrame.SetActive(show);
    }

    // 그룹에서 강제로 탈퇴 (스왑 당할 때 등)
    public void BreakFromGroup()
    {
        PieceGroup oldGroup = group;

        if (group != null)
        {
            group.RemovePiece(this);
        }

        // 새로운 단독 그룹 생성
        group = new PieceGroup();
        group.AddPiece(this);

        // 이전 그룹의 테두리 업데이트 (조각이 빠졌으므로)
        if (oldGroup != null)
        {
            oldGroup.UpdateGroupBorder();
        }

        // 새 그룹(단독)의 테두리 업데이트 - 개별 프레임 표시
        group.UpdateGroupBorder();

        // 테두리 초기화 (다시 다 보여줌)
        UpdateVisuals();
        ShowAllBorders();  // 프레임 테두리 모두 복원

        // Padding 복원 (다시 다 가리기) - EdgeCover 대체
        RestoreAllPadding();

        // 둥근 모서리 복원 (4개 모두 둥글게)
        RestoreAllCorners();
    }

    // ====== EdgeCover 제어 메서드 ======

    /// <summary>
    /// 특정 방향의 EdgeCover를 제거합니다 (병합 시 호출).
    /// </summary>
    public void RemoveEdgeCover(int direction)
    {
        if (direction >= 0 && direction < 4 && _edgeCovers[direction] != null)
        {
            _edgeCovers[direction].SetActive(false);
        }
    }

    /// <summary>
    /// 특정 방향의 EdgeCover를 복원합니다 (그룹 분리 시 호출).
    /// </summary>
    public void RestoreEdgeCover(int direction)
    {
        if (direction >= 0 && direction < 4 && _edgeCovers[direction] != null)
        {
            _edgeCovers[direction].SetActive(true);
        }
    }

    /// <summary>
    /// 모든 EdgeCover를 복원합니다 (그룹 완전 분리 시 호출).
    /// </summary>
    public void RestoreAllEdgeCovers()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_edgeCovers[i] != null)
            {
                _edgeCovers[i].SetActive(true);
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = _cameraZDepth;
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }

    // ====== 둥근 모서리 셰이더 메서드 ======

    /// <summary>
    /// 둥근 모서리 셰이더를 적용합니다.
    /// </summary>
    /// <param name="cornerRadius">모서리 반경 (0~0.5)</param>
    /// <param name="shader">Inspector에서 참조한 셰이더 (null이면 Shader.Find 시도)</param>
    public void ApplyRoundedCornerShader(float cornerRadius = 0.05f, Shader shader = null)
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        Debug.Log($"[DragController] ApplyRoundedCornerShader 시작: pieceWidth={pieceWidth}, pieceHeight={pieceHeight}");

        // 셰이더 설정 (전달받은 셰이더 우선, 없으면 Shader.Find 시도)
        if (_roundedShader == null)
        {
            if (shader != null)
            {
                _roundedShader = shader;
                Debug.Log($"[DragController] Inspector에서 셰이더 참조 사용: {shader.name}");
            }
            else
            {
                _roundedShader = Shader.Find(ROUNDED_SHADER_NAME);
                if (_roundedShader == null)
                {
                    Debug.LogError($"[DragController] 셰이더를 찾을 수 없습니다: {ROUNDED_SHADER_NAME}");
                    return;
                }
                Debug.Log($"[DragController] Shader.Find로 셰이더 로드 성공: {ROUNDED_SHADER_NAME}");
            }
        }

        // 공유 Material 생성 (한 번만)
        if (_sharedRoundedMaterial == null)
        {
            _sharedRoundedMaterial = new Material(_roundedShader);
        }

        // Material 적용
        _spriteRenderer.sharedMaterial = _sharedRoundedMaterial;

        // 스프라이트의 UV rect 계산
        Sprite sprite = _spriteRenderer.sprite;
        Texture2D texture = sprite.texture;

        Rect spriteRect = sprite.rect;
        float uvMinX = spriteRect.x / texture.width;
        float uvMinY = spriteRect.y / texture.height;
        float uvMaxX = (spriteRect.x + spriteRect.width) / texture.width;
        float uvMaxY = (spriteRect.y + spriteRect.height) / texture.height;

        Vector4 uvRect = new Vector4(uvMinX, uvMinY, uvMaxX, uvMaxY);

        // 기본 모서리 반경 저장 및 초기화
        _defaultCornerRadius = cornerRadius;
        _cornerRadii = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);

        // MaterialPropertyBlock으로 개별 프로퍼티 설정 (Material 복사 없이)
        _propertyBlock = new MaterialPropertyBlock();
        _spriteRenderer.GetPropertyBlock(_propertyBlock);

        _propertyBlock.SetVector(UV_RECT_PROPERTY, uvRect);
        _propertyBlock.SetFloat(CORNER_RADIUS_PROPERTY, cornerRadius);
        _propertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
        _propertyBlock.SetVector(PADDING_PROPERTY, _padding);  // Padding 초기값도 설정
        _propertyBlock.SetTexture("_MainTex", texture);

        _spriteRenderer.SetPropertyBlock(_propertyBlock);

        // 카드 뒷면에도 셰이더 적용 (있는 경우)
        ApplyShaderToCardBack();

        // 프레임에도 셰이더 적용 (있는 경우)
        ApplyShaderToFrames();

        Debug.Log($"[DragController] 둥근 모서리 셰이더 적용됨. CornerRadius={cornerRadius}, UVRect={uvRect}, Padding={_padding}");
    }

    /// <summary>
    /// 카드 뒷면에 둥근 모서리 셰이더를 적용합니다.
    /// </summary>
    private void ApplyShaderToCardBack()
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

        _cardBackPropertyBlock.SetVector(UV_RECT_PROPERTY, uvRect);
        _cardBackPropertyBlock.SetFloat(CORNER_RADIUS_PROPERTY, _defaultCornerRadius);
        _cardBackPropertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
        _cardBackPropertyBlock.SetVector(PADDING_PROPERTY, _padding);
        _cardBackPropertyBlock.SetTexture("_MainTex", texture);

        _cardBackRenderer.SetPropertyBlock(_cardBackPropertyBlock);

        Debug.Log($"[DragController] 카드 뒷면 셰이더 적용됨. UVRect={uvRect}");
    }

    /// <summary>
    /// 둥근 모서리 반경을 설정합니다.
    /// </summary>
    public void SetCornerRadius(float radius)
    {
        if (_propertyBlock != null && _spriteRenderer != null)
        {
            _propertyBlock.SetFloat(CORNER_RADIUS_PROPERTY, radius);
            _spriteRenderer.SetPropertyBlock(_propertyBlock);
        }
    }

    /// <summary>
    /// 현재 둥근 모서리 반경을 반환합니다.
    /// </summary>
    public float GetCornerRadius()
    {
        if (_propertyBlock != null)
        {
            return _propertyBlock.GetFloat(CORNER_RADIUS_PROPERTY);
        }
        return 0f;
    }

    /// <summary>
    /// 특정 방향의 모서리 반경을 설정합니다.
    /// direction: 0=TopLeft, 1=TopRight, 2=BottomLeft, 3=BottomRight
    /// </summary>
    public void SetCornerRadiusAt(int direction, float radius)
    {
        switch (direction)
        {
            case 0: _cornerRadii.x = radius; break; // TL
            case 1: _cornerRadii.y = radius; break; // TR
            case 2: _cornerRadii.z = radius; break; // BL
            case 3: _cornerRadii.w = radius; break; // BR
        }
        ApplyCornerRadii();
    }

    /// <summary>
    /// 모든 모서리 반경을 개별적으로 설정합니다.
    /// </summary>
    public void SetCornerRadii(float topLeft, float topRight, float bottomLeft, float bottomRight)
    {
        _cornerRadii = new Vector4(topLeft, topRight, bottomLeft, bottomRight);
        ApplyCornerRadii();
    }

    /// <summary>
    /// 현재 모서리 반경을 PropertyBlock에 적용합니다.
    /// </summary>
    private void ApplyCornerRadii()
    {
        // 앞면 적용
        if (_propertyBlock != null && _spriteRenderer != null)
        {
            _propertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
            _spriteRenderer.SetPropertyBlock(_propertyBlock);
        }

        // 뒷면도 적용
        if (_cardBackPropertyBlock != null && _cardBackRenderer != null)
        {
            _cardBackPropertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
            _cardBackRenderer.SetPropertyBlock(_cardBackPropertyBlock);
        }

        // 프레임에도 적용
        if (_whiteFramePropertyBlock != null && _whiteFrameRenderer != null)
        {
            _whiteFramePropertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
            _whiteFrameRenderer.SetPropertyBlock(_whiteFramePropertyBlock);
        }
        if (_blackFramePropertyBlock != null && _blackFrameRenderer != null)
        {
            _blackFramePropertyBlock.SetVector(CORNER_RADII_PROPERTY, _cornerRadii);
            _blackFrameRenderer.SetPropertyBlock(_blackFramePropertyBlock);
        }
    }

    /// <summary>
    /// 특정 방향의 모서리를 숨깁니다 (반경 0으로 설정).
    /// direction: 0=TopLeft, 1=TopRight, 2=BottomLeft, 3=BottomRight
    /// </summary>
    public void HideCorner(int direction)
    {
        SetCornerRadiusAt(direction, 0f);
    }

    /// <summary>
    /// 특정 방향의 모서리를 복원합니다 (기본 반경으로).
    /// direction: 0=TopLeft, 1=TopRight, 2=BottomLeft, 3=BottomRight
    /// </summary>
    public void RestoreCorner(int direction)
    {
        SetCornerRadiusAt(direction, _defaultCornerRadius);
    }

    /// <summary>
    /// 모든 모서리를 기본 반경으로 복원합니다.
    /// </summary>
    public void RestoreAllCorners()
    {
        _cornerRadii = new Vector4(_defaultCornerRadius, _defaultCornerRadius, _defaultCornerRadius, _defaultCornerRadius);
        ApplyCornerRadii();
    }

    // ====== Padding 시스템 메서드 ======

    /// <summary>
    /// 기본 패딩값을 설정합니다. (pieceSpacing/2 기준으로 UV 비율 계산)
    /// </summary>
    public void SetDefaultPadding(float paddingWorldSize)
    {
        // World space 크기 저장 (복원 시 재계산용)
        _defaultPaddingWorldSize = paddingWorldSize;

        // 이미지 패딩을 0으로 설정하여 이미지가 하얀 테두리 아래까지 채워지도록 함
        // 하얀 테두리는 이미지 위에 오버레이되므로, 이미지는 테두리 영역까지 표시됨
        // 카드 간 간격은 카드 배치 위치(슬롯)로 유지됨
        _padding = Vector4.zero;
        ApplyPadding();

        Debug.Log($"[DragController] Padding 설정: 0 (이미지가 테두리까지 채워짐)");
    }

    /// <summary>
    /// 현재 패딩값을 PropertyBlock에 적용합니다.
    /// </summary>
    private void ApplyPadding()
    {
        // 앞면 적용
        if (_propertyBlock != null && _spriteRenderer != null)
        {
            _propertyBlock.SetVector(PADDING_PROPERTY, _padding);
            _spriteRenderer.SetPropertyBlock(_propertyBlock);
        }
        else
        {
            Debug.LogWarning($"[DragController] ApplyPadding 실패: _propertyBlock={_propertyBlock != null}, _spriteRenderer={_spriteRenderer != null}");
        }

        // 뒷면도 적용
        if (_cardBackPropertyBlock != null && _cardBackRenderer != null)
        {
            _cardBackPropertyBlock.SetVector(PADDING_PROPERTY, _padding);
            _cardBackRenderer.SetPropertyBlock(_cardBackPropertyBlock);
        }
    }

    /// <summary>
    /// 특정 방향의 패딩을 설정합니다.
    /// direction: 0=Left, 1=Right, 2=Top, 3=Bottom
    /// </summary>
    public void SetPaddingAt(int direction, float value)
    {
        switch (direction)
        {
            case 0: _padding.x = value; break; // Left
            case 1: _padding.y = value; break; // Right
            case 2: _padding.z = value; break; // Top
            case 3: _padding.w = value; break; // Bottom
        }
        ApplyPadding();
    }

    /// <summary>
    /// 특정 방향의 패딩을 제거합니다 (음수값으로 설정하여 살짝 겹침).
    /// direction: 0=Top, 1=Bottom, 2=Left, 3=Right (EdgeCover와 동일한 순서)
    /// </summary>
    public void RemovePadding(int direction)
    {
        // EdgeCover 순서: 0=Top, 1=Bottom, 2=Left, 3=Right
        // Padding 순서: x=Left, y=Right, z=Top, w=Bottom
        // 변환 필요
        //
        // 음수 패딩을 사용하여 이미지가 살짝 겹치게 함 (경계선 제거)
        // -0.005 = 0.5% 확장으로 인접 카드와 겹침
        const float overlapPadding = -0.005f;

        switch (direction)
        {
            case 0: _padding.z = overlapPadding; break; // Top
            case 1: _padding.w = overlapPadding; break; // Bottom
            case 2: _padding.x = overlapPadding; break; // Left
            case 3: _padding.y = overlapPadding; break; // Right
        }
        ApplyPadding();
    }

    /// <summary>
    /// 특정 방향의 패딩을 기본값(0)으로 복원합니다.
    /// direction: 0=Top, 1=Bottom, 2=Left, 3=Right (EdgeCover와 동일한 순서)
    /// </summary>
    public void RestorePadding(int direction)
    {
        // 패딩을 0으로 복원 (이미지가 테두리까지 채워짐)
        switch (direction)
        {
            case 0: _padding.z = 0f; break; // Top
            case 1: _padding.w = 0f; break; // Bottom
            case 2: _padding.x = 0f; break; // Left
            case 3: _padding.y = 0f; break; // Right
        }
        ApplyPadding();
    }

    /// <summary>
    /// 모든 방향의 패딩을 기본값(0)으로 복원합니다.
    /// </summary>
    public void RestoreAllPadding()
    {
        // 패딩을 0으로 복원 (이미지가 테두리까지 채워짐)
        _padding = Vector4.zero;
        ApplyPadding();
    }

    // ====== 카드 시스템 메서드 ======

    /// <summary>
    /// 카드 컴포넌트를 초기화합니다 (뒷면 생성).
    /// </summary>
    public void InitializeCardVisuals(Sprite cardBackSprite, float frameThickness = 0.08f)
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        float width = _spriteRenderer.bounds.size.x;
        float height = _spriteRenderer.bounds.size.y;

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
        _cardBackRenderer.sortingOrder = 10; // 퍼즐 이미지와 테두리 위에 (테두리가 5,6 사용)

        // 스프라이트 원본 크기 기준으로 스케일 계산
        float backSpriteWidth = backSprite.bounds.size.x;
        float backSpriteHeight = backSprite.bounds.size.y;
        float backScaleX = width / backSpriteWidth;
        float backScaleY = height / backSpriteHeight;
        backObj.transform.localScale = new Vector3(backScaleX, backScaleY, 1);

        // 초기 상태: 뒷면이 보이는 상태
        _isFlipped = false;
        _spriteRenderer.enabled = false; // 퍼즐 이미지 숨김

        // 기존 테두리 숨기기 (뒷면 상태에서는 보이지 않음)
        ShowFrames(false);

        // [Deprecated] EdgeCover는 더 이상 사용하지 않음 - Padding 셰이더로 대체
        // CreateEdgeCovers();
        // Padding은 ApplyRoundedCornerShader 호출 후 SetDefaultPadding으로 설정됨
    }

    /// <summary>
    /// 커버 크기를 설정합니다 (spacing/2).
    /// </summary>
    public void SetCoverSize(float size)
    {
        _coverSize = size;
    }

    /// <summary>
    /// EdgeCover를 생성합니다 (각 방향의 spacing 영역을 가리는 불투명 커버).
    /// </summary>
    void CreateEdgeCovers()
    {
        Debug.Log($"[EdgeCover] CreateEdgeCovers 호출됨. _coverSize={_coverSize}, pieceWidth={pieceWidth}, pieceHeight={pieceHeight}");

        if (_coverSize <= 0)
        {
            Debug.LogWarning("[EdgeCover] _coverSize가 0 이하입니다. EdgeCover 생성 안됨.");
            return;
        }

        float width = pieceWidth;
        float height = pieceHeight;

        // 커버 색상 (Main Camera 배경색과 자동 동기화)
        Color coverColor = Camera.main.backgroundColor;
        coverColor.a = 1f; // 알파값 강제 1 (배경색 알파가 0일 수 있음)
        Debug.Log($"[EdgeCover] Camera backgroundColor: R={coverColor.r}, G={coverColor.g}, B={coverColor.b}, A={coverColor.a}");

        // 각 방향별 위치와 크기
        // 0:Top, 1:Bottom, 2:Left, 3:Right
        // EdgeCover는 이미지 가장자리에 위치하여 spacing/2 만큼 덮음
        // buffer: 렌더링 틈새 방지를 위한 여유 (0.02f로 증가)
        float buffer = 0.02f;
        float coverSizeWithBuffer = _coverSize + buffer;

        // 위치 계산: 커버가 퍼즐 이미지 가장자리를 완전히 덮도록 조정
        Vector3[] positions = new Vector3[]
        {
            new Vector3(0, (height / 2) - (coverSizeWithBuffer / 2) + buffer, 0),   // Top: 상단 가장자리
            new Vector3(0, -(height / 2) + (coverSizeWithBuffer / 2) - buffer, 0),  // Bottom: 하단 가장자리
            new Vector3(-(width / 2) + (coverSizeWithBuffer / 2) - buffer, 0, 0),   // Left: 좌측 가장자리
            new Vector3((width / 2) - (coverSizeWithBuffer / 2) + buffer, 0, 0)     // Right: 우측 가장자리
        };

        Vector3[] scales = new Vector3[]
        {
            new Vector3(width + buffer * 2, coverSizeWithBuffer, 1),      // Top (가로로 긴 막대)
            new Vector3(width + buffer * 2, coverSizeWithBuffer, 1),      // Bottom
            new Vector3(coverSizeWithBuffer, height + buffer * 2, 1),     // Left (세로로 긴 막대)
            new Vector3(coverSizeWithBuffer, height + buffer * 2, 1)      // Right
        };

        string[] names = new string[] { "EdgeCover_Top", "EdgeCover_Bottom", "EdgeCover_Left", "EdgeCover_Right" };

        for (int i = 0; i < 4; i++)
        {
            GameObject cover = new GameObject(names[i]);
            cover.transform.SetParent(transform, false);
            cover.transform.localPosition = positions[i];

            SpriteRenderer sr = cover.AddComponent<SpriteRenderer>();
            // PPU=1인 스프라이트 생성 (스케일이 Unity 단위와 1:1 매칭되도록)
            sr.sprite = CreateUnitSprite();
            sr.color = coverColor;
            sr.sortingOrder = 2; // 퍼즐 이미지(1) 위, 카드뒷면(3) 아래

            // 스케일 적용 (PPU=1이므로 스케일 = 실제 Unity 크기)
            cover.transform.localScale = scales[i];

            _edgeCovers[i] = cover;

            Debug.Log($"[EdgeCover] {names[i]} 생성됨. 위치={positions[i]}, 크기={scales[i]}");
        }
    }

    /// <summary>
    /// PPU=1인 1x1 픽셀 스프라이트를 생성합니다 (EdgeCover용).
    /// </summary>
    Sprite CreateUnitSprite()
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f); // PPU = 1
    }

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
        _cardBackRenderer.gameObject.SetActive(false);
        _spriteRenderer.enabled = true;

        // 테두리 다시 활성화 (프레임 방식)
        ShowFrames(true);

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

        _spriteRenderer.enabled = true;
        _isFlipped = true;

        // 테두리 활성화 (프레임 방식)
        ShowFrames(true);
    }

    /// <summary>
    /// 즉시 뒷면으로 전환합니다 (애니메이션 없이).
    /// </summary>
    public void ShowBackImmediate()
    {
        if (_cardBackRenderer != null)
            _cardBackRenderer.gameObject.SetActive(true);

        _spriteRenderer.enabled = false;
        _isFlipped = false;

        // 테두리 숨김 (프레임 방식)
        ShowFrames(false);
    }

    /// <summary>
    /// 드래그 가능 여부를 설정합니다.
    /// </summary>
    public void SetDraggable(bool canDrag)
    {
        _canDrag = canDrag;
    }

    #region Pumping Animation

    private bool _isPumping = false;

    /// <summary>
    /// 펌핑 애니메이션을 재생합니다 (합쳐질 때 피드백).
    /// </summary>
    /// <param name="scale">최대 스케일 (1.0 기준, 예: 1.15 = 15% 확대)</param>
    /// <param name="duration">애니메이션 시간 (초)</param>
    public void PlayPumpingAnimation(float scale = 1.15f, float duration = 0.2f)
    {
        if (_isPumping) return;
        StartCoroutine(PumpingAnimation(scale, duration));
    }

    private IEnumerator PumpingAnimation(float maxScale, float duration)
    {
        _isPumping = true;

        Vector3 originalScale = transform.localScale;
        Vector3 targetScale = originalScale * maxScale;
        float halfDuration = duration / 2f;

        // 1단계: 확대 (Ease Out)
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 2f); // Ease Out Quad
            transform.localScale = Vector3.Lerp(originalScale, targetScale, easedT);
            yield return null;
        }

        // 2단계: 축소 (Ease In)
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float easedT = t * t; // Ease In Quad
            transform.localScale = Vector3.Lerp(targetScale, originalScale, easedT);
            yield return null;
        }

        transform.localScale = originalScale;
        _isPumping = false;
    }

    #endregion

    /// <summary>
    /// 카드가 뒤집혔는지 확인합니다.
    /// </summary>
    public bool IsFlipped => _isFlipped;

    /// <summary>
    /// 카드 뒷면 렌더러를 반환합니다.
    /// </summary>
    public SpriteRenderer CardBackRenderer => _cardBackRenderer;

    /// <summary>
    /// 그룹 내 위치에 따라 모서리 가시성을 업데이트합니다.
    /// 인접한 조각이 있는 방향의 모서리는 직각(0)으로, 없는 방향은 둥글게.
    /// </summary>
    public void UpdateCornersBasedOnGroup()
    {
        if (group == null || group.pieces.Count <= 1)
        {
            // 그룹이 없거나 혼자면 모든 모서리 복원
            RestoreAllCorners();
            return;
        }

        // 인접 방향 확인 (같은 그룹 내에서)
        bool hasTop = false;
        bool hasBottom = false;
        bool hasLeft = false;
        bool hasRight = false;

        foreach (var other in group.pieces)
        {
            if (other == this) continue;

            int dx = other.originalGridX - this.originalGridX;
            int dy = other.originalGridY - this.originalGridY;

            // 상하좌우 인접 확인 (대각선 제외)
            if (dx == 0 && dy == -1) hasTop = true;     // 위쪽 (Y가 작을수록 위)
            if (dx == 0 && dy == 1) hasBottom = true;   // 아래쪽
            if (dx == -1 && dy == 0) hasLeft = true;    // 왼쪽
            if (dx == 1 && dy == 0) hasRight = true;    // 오른쪽
        }

        // 각 모서리 결정: 해당 모서리에 닿는 두 변 중 하나라도 인접 조각이 있으면 직각
        // TL (Top-Left): 위 또는 왼쪽에 인접 조각 있으면 직각
        // TR (Top-Right): 위 또는 오른쪽에 인접 조각 있으면 직각
        // BL (Bottom-Left): 아래 또는 왼쪽에 인접 조각 있으면 직각
        // BR (Bottom-Right): 아래 또는 오른쪽에 인접 조각 있으면 직각

        float radiusTL = (hasTop || hasLeft) ? 0f : _defaultCornerRadius;
        float radiusTR = (hasTop || hasRight) ? 0f : _defaultCornerRadius;
        float radiusBL = (hasBottom || hasLeft) ? 0f : _defaultCornerRadius;
        float radiusBR = (hasBottom || hasRight) ? 0f : _defaultCornerRadius;

        SetCornerRadii(radiusTL, radiusTR, radiusBL, radiusBR);
    }
}

// ====== 그룹 클래스 ======
public class PieceGroup
{
    public List<DragController> pieces = new List<DragController>();
    private Dictionary<DragController, Vector3> _startPositions = new Dictionary<DragController, Vector3>();
    private Vector3 _mouseStartWorldPos;

    // ====== 그룹 테두리 렌더러 (CompositeCollider2D + LineRenderer 방식) ======
    private GameObject _borderContainer;
    private GroupBorderRenderer _borderRenderer;

    public void AddPiece(DragController piece)
    {
        if (!pieces.Contains(piece))
        {
            pieces.Add(piece);
            piece.group = this;
        }
    }

    /// <summary>
    /// 두 그룹을 병합합니다 (카드 이동 없음 - EdgeCover 제거로 이미지 연결).
    /// 카드들은 슬롯 위치에 그대로 유지되고, EdgeCover만 제거되어 이미지가 연결됩니다.
    /// </summary>
    public void MergeGroupWithSnap(PieceGroup otherGroup, DragController anchorPiece, DragController connectingPiece)
    {
        if (otherGroup == this) return;

        // 이전 그룹의 테두리 제거
        otherGroup.DestroyGroupBorder();

        // 카드 위치 이동 없이 그룹만 병합
        // EdgeCover 제거는 CheckNeighbor에서 처리됨
        foreach (var piece in otherGroup.pieces)
        {
            piece.group = this;
            pieces.Add(piece);
        }
        otherGroup.pieces.Clear();
    }

    public void MergeGroup(PieceGroup otherGroup)
    {
        if (otherGroup == this) return;

        // 이전 그룹의 테두리 제거
        otherGroup.DestroyGroupBorder();

        foreach (var piece in otherGroup.pieces)
        {
            piece.group = this;
            pieces.Add(piece);
        }
        otherGroup.pieces.Clear();
    }

    public void RemovePiece(DragController piece)
    {
        if (pieces.Contains(piece))
        {
            pieces.Remove(piece);
        }
    }

    public void OnDragStart(Vector3 mousePos)
    {
        _mouseStartWorldPos = mousePos;
        _startPositions.Clear();
        foreach (var piece in pieces)
        {
            _startPositions[piece] = piece.transform.position;
        }
    }

    public void OnDragUpdate(Vector3 currentMousePos)
    {
        Vector3 delta = currentMousePos - _mouseStartWorldPos;
        foreach (var piece in pieces)
        {
            if (_startPositions.ContainsKey(piece))
            {
                piece.SetPositionImmediate(_startPositions[piece] + delta);
            }
        }

        // 그룹 테두리 위치도 업데이트
        if (_borderRenderer != null)
        {
            _borderRenderer.UpdatePosition();
        }
    }

    public void SetSortingOrder(int order)
    {
        foreach (var piece in pieces)
        {
            // 메인 퍼즐 이미지
            piece.GetComponent<SpriteRenderer>().sortingOrder = order;

            // 카드 뒷면 (맨 위)
            if (piece.CardBackRenderer != null)
            {
                piece.CardBackRenderer.sortingOrder = order + 10;
            }

            // 그룹이 2개 이상이면 개별 프레임은 숨기고 그룹 테두리 사용
            if (pieces.Count >= 2)
            {
                piece.ShowFrames(false);
            }
            else
            {
                // 단독 조각이면 개별 프레임 표시
                piece.ShowFrames(true);

                // 프레임 테두리 (퍼즐 이미지 위, 오버레이 방식)
                var allRenderers = piece.GetComponentsInChildren<SpriteRenderer>();
                foreach (var sr in allRenderers)
                {
                    // 자기 자신, 카드 뒷면 제외
                    if (sr.gameObject == piece.gameObject) continue;
                    if (piece.CardBackRenderer != null && sr == piece.CardBackRenderer) continue;

                    // 프레임 종류에 따라 다른 order 적용 (퍼즐 이미지 위에 오버레이)
                    if (sr.gameObject.name == "WhiteFrame")
                        sr.sortingOrder = order + 1;  // 퍼즐 이미지 위
                    else if (sr.gameObject.name == "BlackFrame")
                        sr.sortingOrder = order + 2;  // 하얀 프레임 위
                    else
                        sr.sortingOrder = order + 1;
                }
            }
        }

        // 그룹 테두리 sorting order 업데이트
        if (_borderRenderer != null)
        {
            _borderRenderer.SetSortingOrder(order + 1);
        }
    }

    // ====== 그룹 테두리 메서드 ======

    /// <summary>
    /// 그룹 테두리를 업데이트합니다. 2개 이상 조각이 있으면 그룹 테두리 생성/업데이트.
    /// </summary>
    public void UpdateGroupBorder()
    {
        if (pieces.Count < 2)
        {
            // 단독 조각이면 그룹 테두리 제거하고 개별 프레임 표시
            DestroyGroupBorder();
            if (pieces.Count == 1)
            {
                pieces[0].ShowFrames(true);
                pieces[0].ShowAllBorders();
            }
            return;
        }

        // 2개 이상: 개별 프레임 숨기고 그룹 테두리 생성/업데이트
        foreach (var piece in pieces)
        {
            piece.ShowFrames(false);
        }

        CreateOrUpdateGroupBorder();
    }

    /// <summary>
    /// 그룹 테두리를 생성하거나 업데이트합니다.
    /// </summary>
    private void CreateOrUpdateGroupBorder()
    {
        if (pieces.Count < 2) return;

        // 컨테이너가 없으면 생성
        if (_borderContainer == null)
        {
            _borderContainer = new GameObject("GroupBorder");
            _borderRenderer = _borderContainer.AddComponent<GroupBorderRenderer>();

            // 테두리 두께 및 모서리 반경 설정 (첫 번째 조각 기준 - 개별 카드와 동일한 값 사용)
            var firstPiece = pieces[0];
            float whiteWidth, blackWidth;
            firstPiece.GetBorderThicknessWorldSpace(out whiteWidth, out blackWidth);
            float cornerRadius = firstPiece.GetCornerRadiusWorldSpace();

            _borderRenderer.SetBorderWidth(whiteWidth, blackWidth);
            _borderRenderer.SetCornerRadius(cornerRadius);
        }

        // 조각 목록 전달 및 테두리 업데이트
        _borderRenderer.SetPieces(pieces);
    }

    /// <summary>
    /// 그룹 테두리 위치만 업데이트합니다 (애니메이션 중 호출용).
    /// UpdateGroupBorder()와 달리 조각 목록을 다시 설정하지 않고 위치만 갱신합니다.
    /// </summary>
    public void UpdateGroupBorderPosition()
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.UpdatePosition();
        }
    }

    /// <summary>
    /// 펌핑 애니메이션용 - 그룹 테두리를 중심 기준으로 스케일 적용
    /// </summary>
    public void UpdateGroupBorderWithScale(Vector3 groupCenter, float scale)
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.UpdatePositionWithScale(groupCenter, scale);
        }
    }

    /// <summary>
    /// 펌핑 애니메이션 완료 후 테두리 스케일 데이터 초기화
    /// </summary>
    public void ResetGroupBorderScaleData()
    {
        if (_borderRenderer != null)
        {
            _borderRenderer.ResetScaleData();
        }
    }

    /// <summary>
    /// 그룹 테두리를 제거합니다.
    /// </summary>
    public void DestroyGroupBorder()
    {
        if (_borderContainer != null)
        {
            Object.Destroy(_borderContainer);
            _borderContainer = null;
            _borderRenderer = null;
        }
    }
}